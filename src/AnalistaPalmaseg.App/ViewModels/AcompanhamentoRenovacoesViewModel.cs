using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class AcompanhamentoRenovacoesViewModel : ObservableObject
{
    private readonly RelatorioRenovacaoService _service;
    private readonly SessaoService _sessao;
    private readonly FolhaAmarelaService _folhaService;
    private readonly AnexoService _anexoService;
    private List<RelatorioRenovacao> _todos = [];
    private ListCollectionView? _view;
    private CancellationTokenSource? _debounceCts;
    private int _mesFiltroAno;
    private int _mesFiltroMes;
    private readonly Dictionary<string, (int Year, int Month)> _mesLookup = [];
    private Dictionary<int, string> _situacaoAnterior = [];

    [ObservableProperty] private ICollectionView? _registrosView;
    [ObservableProperty] private RelatorioRenovacao? _registroSelecionado;
    [ObservableProperty] private string _filtroTexto = string.Empty;
    [ObservableProperty] private string _filtroSituacao = "Todos";
    [ObservableProperty] private string _filtroProdutor = "(Com produtor)";
    [ObservableProperty] private string _mesSelecionado = "Todos";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _resumo = string.Empty;

    public bool IsAdmin => _sessao.IsAdmin;
    public bool TemRegistroSelecionado => RegistroSelecionado != null;

    public string[] SituacoesEditar { get; } =
        ["À Renovar", "Agendado", "Calculado", "Procurado", "Ren. Palma", "Ren. Outro", "Não renovado", "Recusado", "Emitido"];

    public string[] SituacoesFiltrar { get; } =
        ["Todos", "À Renovar", "Agendado", "Calculado", "Procurado", "Ren. Palma", "Ren. Outro", "Não renovado", "Recusado", "Emitido"];

    public ObservableCollection<string> ProdutoresDisponiveis { get; } = [];
    public ObservableCollection<string> MesesDisponiveis { get; } = [];

    public AcompanhamentoRenovacoesViewModel(
        RelatorioRenovacaoService service,
        SessaoService sessao,
        FolhaAmarelaService folhaService,
        AnexoService anexoService)
    {
        _service = service;
        _sessao = sessao;
        _folhaService = folhaService;
        _anexoService = anexoService;
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            foreach (var item in _todos)
                item.PropertyChanged -= OnItemPropertyChanged;

            _todos = await _service.GetTodosAsync();

            foreach (var item in _todos)
                item.PropertyChanged += OnItemPropertyChanged;

            _situacaoAnterior = _todos.ToDictionary(r => r.Id, r => r.SituacaoAcompanhamento);

            var prods = await _service.GetNovoProdutorDistinctAsync();
            ProdutoresDisponiveis.Clear();
            ProdutoresDisponiveis.Add(string.Empty);
            ProdutoresDisponiveis.Add("(Com produtor)");
            foreach (var p in prods) ProdutoresDisponiveis.Add(p);

            var cul = new CultureInfo("pt-BR");
            _mesLookup.Clear();
            var meses = _todos
                .Where(r => r.VigenciaFinal.HasValue)
                .Select(r => new { r.VigenciaFinal!.Value.Year, r.VigenciaFinal!.Value.Month })
                .Distinct()
                .OrderBy(m => m.Year).ThenBy(m => m.Month)
                .ToList();

            MesesDisponiveis.Clear();
            MesesDisponiveis.Add("Todos");
            foreach (var m in meses)
            {
                var raw = new DateTime(m.Year, m.Month, 1).ToString("MMMM/yyyy", cul);
                var label = raw.Length > 0 ? char.ToUpper(raw[0]) + raw.Substring(1) : raw;
                MesesDisponiveis.Add(label);
                _mesLookup[label] = (m.Year, m.Month);
            }

            var col = new ObservableCollection<RelatorioRenovacao>(_todos);
            _view = (ListCollectionView)CollectionViewSource.GetDefaultView(col);
            _view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(RelatorioRenovacao.NomeCliente)));
            _view.CustomSort = Comparer<object>.Create((a, b) =>
            {
                var ra = (RelatorioRenovacao)a;
                var rb = (RelatorioRenovacao)b;
                var nome = StringComparer.OrdinalIgnoreCase.Compare(ra.NomeCliente, rb.NomeCliente);
                return nome != 0 ? nome : Nullable.Compare(ra.VigenciaFinal, rb.VigenciaFinal);
            });
            _view.Filter = FiltroItem;
            RegistrosView = _view;
            AtualizarResumo();
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(RelatorioRenovacao.SituacaoAcompanhamento)) return;
        if (sender is not RelatorioRenovacao reg) return;

        if (reg.SituacaoAcompanhamento == "Ren. Palma")
        {
            List<Anexo> anexos;
            try { anexos = await _anexoService.GetAnexosAsync(reg.Id); }
            catch { anexos = []; }

            var dialog = new AnalistaPalmaseg.App.Views.FechamentoRenPalmaDialog(reg, anexos)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() != true)
            {
                Reverter(reg);
                return;
            }

            try
            {
                await _service.SalvarFechamentoAsync(reg);
                _situacaoAnterior[reg.Id] = reg.SituacaoAcompanhamento;
                AplicarFiltro();
                EnviarRefreshDashboard(reg);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar fechamento:\n{ex.Message}", "Erro",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                Reverter(reg);
            }
            return;
        }

        // Statuses that require a justification
        var exigeMotivo = reg.SituacaoAcompanhamento is "Agendado" or "Ren. Outro" or "Não renovado" or "Recusado";

        if (exigeMotivo)
        {
            var motivoDialog = new AnalistaPalmaseg.App.Views.MotivoSituacaoDialog(reg.SituacaoAcompanhamento)
            {
                Owner = Application.Current.MainWindow
            };

            if (motivoDialog.ShowDialog() != true)
            {
                Reverter(reg);
                return;
            }

            reg.MotivoSituacao = motivoDialog.Motivo;
        }
        else
        {
            reg.MotivoSituacao = null;
        }

        try
        {
            await _service.SalvarSituacaoAsync(reg);
            _situacaoAnterior[reg.Id] = reg.SituacaoAcompanhamento;
            AplicarFiltro();
            EnviarRefreshDashboard(reg);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar situação:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void EnviarRefreshDashboard(RelatorioRenovacao reg)
    {
        if (reg.VigenciaFinal.HasValue)
            WeakReferenceMessenger.Default.Send(
                new DashboardRefreshMessage(reg.VigenciaFinal.Value.Month, reg.VigenciaFinal.Value.Year));
    }

    private void Reverter(RelatorioRenovacao reg)
    {
        reg.PropertyChanged -= OnItemPropertyChanged;
        reg.SituacaoAcompanhamento = _situacaoAnterior.GetValueOrDefault(reg.Id, "À Renovar");
        reg.PropertyChanged += OnItemPropertyChanged;
    }

    // ── Filtros ────────────────────────────────────────────────────────────────

    partial void OnFiltroTextoChanged(string value)
    {
        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;
        Task.Delay(300, token).ContinueWith(_ =>
        {
            if (token.IsCancellationRequested) return;
            Application.Current?.Dispatcher.Invoke(AplicarFiltro);
        }, TaskScheduler.Default);
    }

    partial void OnFiltroSituacaoChanged(string value) => AplicarFiltro();
    partial void OnFiltroProdutorChanged(string value) => AplicarFiltro();

    partial void OnMesSelecionadoChanged(string value)
    {
        if (value == "Todos" || !_mesLookup.TryGetValue(value, out var mes))
        {
            _mesFiltroAno = 0;
            _mesFiltroMes = 0;
        }
        else
        {
            _mesFiltroAno = mes.Year;
            _mesFiltroMes = mes.Month;
        }
        AplicarFiltro();
    }

    private bool FiltroItem(object obj)
    {
        if (obj is not RelatorioRenovacao r) return false;

        if (_mesFiltroAno > 0)
        {
            if (!r.VigenciaFinal.HasValue ||
                r.VigenciaFinal.Value.Year != _mesFiltroAno ||
                r.VigenciaFinal.Value.Month != _mesFiltroMes)
                return false;
        }

        if (FiltroSituacao != "Todos" && r.SituacaoAcompanhamento != FiltroSituacao) return false;

        if (FiltroProdutor == "(Com produtor)")
        {
            if (string.IsNullOrWhiteSpace(r.NovoProdutor)) return false;
        }
        else if (!string.IsNullOrWhiteSpace(FiltroProdutor) && r.NovoProdutor != FiltroProdutor) return false;

        if (!string.IsNullOrWhiteSpace(FiltroTexto))
        {
            var txt = FiltroTexto.Trim().ToLowerInvariant();
            return (r.NomeCliente?.ToLowerInvariant().Contains(txt) == true) ||
                   (r.Proposta?.ToLowerInvariant().Contains(txt) == true) ||
                   (r.Apolice?.ToLowerInvariant().Contains(txt) == true) ||
                   (r.DocumentoPrincipal?.ToLowerInvariant().Contains(txt) == true) ||
                   (r.Seguradora?.ToLowerInvariant().Contains(txt) == true) ||
                   (r.NovoProdutor?.ToLowerInvariant().Contains(txt) == true);
        }
        return true;
    }

    private void AplicarFiltro()
    {
        if (_view == null) return;
        _view.Refresh();
        AtualizarResumo();
    }

    private void AtualizarResumo()
    {
        if (_view == null) { Resumo = string.Empty; return; }
        var itens = _view.Cast<RelatorioRenovacao>().ToList();
        var realizadas = itens.Count(r => r.RenovacaoRealizada);
        var aRenovar = itens.Count(r => r.SituacaoAcompanhamento == "À Renovar");
        var total = itens.Sum(r => r.PremioTotal);
        Resumo = $"{itens.Count} registro(s) · {realizadas} emitida(s) · {aRenovar} à renovar · Prêmio total: {total:C2}";
    }

    partial void OnRegistroSelecionadoChanged(RelatorioRenovacao? value)
    {
        OnPropertyChanged(nameof(TemRegistroSelecionado));
        GerarFolhaAmarelaCommand.NotifyCanExecuteChanged();
        AnexarArquivosCommand.NotifyCanExecuteChanged();
        AbrirPastaAnexosCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void LimparFiltros()
    {
        FiltroTexto = string.Empty;
        FiltroSituacao = "Todos";
        FiltroProdutor = "(Com produtor)";
        MesSelecionado = "Todos";
    }

    // ── Status administrativo ──────────────────────────────────────────────────

    [RelayCommand]
    private async Task ToggleAssinatura(RelatorioRenovacao? reg)
    {
        if (reg == null) return;
        reg.AssinaturaFeita = !reg.AssinaturaFeita;
        try { await _service.SalvarStatusAdministrativoAsync(reg); }
        catch (Exception ex)
        {
            reg.AssinaturaFeita = !reg.AssinaturaFeita; // reverte
            MessageBox.Show($"Erro ao salvar:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ToggleSeguroEmitido(RelatorioRenovacao? reg)
    {
        if (reg == null) return;
        reg.SeguroEmitido = !reg.SeguroEmitido;
        try { await _service.SalvarStatusAdministrativoAsync(reg); }
        catch (Exception ex)
        {
            reg.SeguroEmitido = !reg.SeguroEmitido; // reverte
            MessageBox.Show($"Erro ao salvar:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Folha Amarela / Anexos ─────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(TemRegistroSelecionado))]
    private void GerarFolhaAmarela()
    {
        if (RegistroSelecionado == null) return;
        try { _folhaService.GerarEAbrir(RegistroSelecionado); }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao gerar folha amarela:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(TemRegistroSelecionado))]
    private async Task AnexarArquivosAsync()
    {
        if (RegistroSelecionado == null) return;

        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Selecionar arquivos para anexar",
            Filter = "Todos os arquivos|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog() != true) return;

        int ok = 0, erros = 0;
        foreach (var file in dialog.FileNames)
        {
            try { await _anexoService.AdicionarAsync(RegistroSelecionado.Id, file); ok++; }
            catch { erros++; }
        }

        var msg = erros == 0
            ? $"{ok} arquivo(s) anexado(s) com sucesso."
            : $"{ok} arquivo(s) anexado(s). {erros} falhou(ram).";
        MessageBox.Show(msg, "Anexos", MessageBoxButton.OK,
            erros == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    [RelayCommand(CanExecute = nameof(TemRegistroSelecionado))]
    private void AbrirPastaAnexos()
    {
        if (RegistroSelecionado == null) return;
        var pasta = AnexoService.ObterPasta(RegistroSelecionado.Id);
        Directory.CreateDirectory(pasta);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pasta)
            { UseShellExecute = true });
    }
}
