using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;
using Microsoft.Win32;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class GerenciadorRenovacoesViewModel : ObservableObject
{
    private readonly RelatorioRenovacaoService _service;
    private readonly FolhaAmarelaService _folhaService;
    private readonly AnexoService _anexoService;
    private readonly UsuarioService _usuarioService;
    private List<RelatorioRenovacao> _todos = [];
    private ListCollectionView? _view;
    private CancellationTokenSource? _debounceCts;
    private int _mesFiltroAno;
    private int _mesFiltroMes;
    private readonly Dictionary<string, (int Year, int Month)> _mesLookup = [];

    [ObservableProperty] private ICollectionView? _registrosView;
    [ObservableProperty] private RelatorioRenovacao? _registroSelecionado;
    [ObservableProperty] private string _filtroTexto = string.Empty;
    [ObservableProperty] private string _filtroStatus = string.Empty;
    [ObservableProperty] private string _filtroSeguradora = string.Empty;
    [ObservableProperty] private string _filtroVendedor = string.Empty;
    [ObservableProperty] private string _filtroProdutor = string.Empty;
    [ObservableProperty] private string _mesSelecionado = "Todos";
    [ObservableProperty] private string _novoProdutorEmMassa = string.Empty;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _resumo = string.Empty;

    public bool TemMarcados => _todos.Any(r => r.IsChecked);

    public ObservableCollection<string> StatusDisponiveis { get; } = [];
    public ObservableCollection<string> SeguradorasDisponiveis { get; } = [];
    public ObservableCollection<string> VendedoresDisponiveis { get; } = [];
    public ObservableCollection<string> ProdutoresDisponiveis { get; } = [];
    public ObservableCollection<string> MesesDisponiveis { get; } = [];
    public ObservableCollection<string> UsuariosDisponiveis { get; } = [];

    public GerenciadorRenovacoesViewModel(
        RelatorioRenovacaoService service,
        FolhaAmarelaService folhaService,
        AnexoService anexoService,
        UsuarioService usuarioService)
    {
        _service = service;
        _folhaService = folhaService;
        _anexoService = anexoService;
        _usuarioService = usuarioService;
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            _todos = await _service.GetTodosAsync();

            var status = await _service.GetStatusDistinctAsync();
            StatusDisponiveis.Clear();
            StatusDisponiveis.Add(string.Empty);
            foreach (var s in status) StatusDisponiveis.Add(s);

            var segs = await _service.GetSeguradorasDistinctAsync();
            SeguradorasDisponiveis.Clear();
            SeguradorasDisponiveis.Add(string.Empty);
            foreach (var s in segs) SeguradorasDisponiveis.Add(s);

            var vends = await _service.GetVendedoresDistinctAsync();
            VendedoresDisponiveis.Clear();
            VendedoresDisponiveis.Add(string.Empty);
            foreach (var s in vends) VendedoresDisponiveis.Add(s);

            var prods = await _service.GetNovoProdutorDistinctAsync();
            ProdutoresDisponiveis.Clear();
            ProdutoresDisponiveis.Add(string.Empty);
            foreach (var p in prods) ProdutoresDisponiveis.Add(p);

            var usuarios = await _usuarioService.ListarAsync();
            UsuariosDisponiveis.Clear();
            UsuariosDisponiveis.Add(string.Empty);
            foreach (var u in usuarios.Where(u => u.Ativo))
                UsuariosDisponiveis.Add(u.Login);

            // Abas por mês de vencimento
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
            _view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(RelatorioRenovacao.DocumentoPrincipal)));
            _view.CustomSort = Comparer<object>.Create((a, b) =>
            {
                var ra = (RelatorioRenovacao)a;
                var rb = (RelatorioRenovacao)b;
                var cpf = StringComparer.OrdinalIgnoreCase.Compare(ra.DocumentoPrincipal, rb.DocumentoPrincipal);
                return cpf != 0 ? cpf : Nullable.Compare(ra.VigenciaFinal, rb.VigenciaFinal);
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

    partial void OnFiltroStatusChanged(string value) => AplicarFiltro();
    partial void OnFiltroSeguradoraChanged(string value) => AplicarFiltro();
    partial void OnFiltroVendedorChanged(string value) => AplicarFiltro();
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

        if (!string.IsNullOrWhiteSpace(FiltroStatus) && r.Status != FiltroStatus) return false;
        if (!string.IsNullOrWhiteSpace(FiltroSeguradora) && r.Seguradora != FiltroSeguradora) return false;
        if (!string.IsNullOrWhiteSpace(FiltroVendedor) && r.VendedorPrincipal != FiltroVendedor) return false;
        if (!string.IsNullOrWhiteSpace(FiltroProdutor) && r.NovoProdutor != FiltroProdutor) return false;

        if (!string.IsNullOrWhiteSpace(FiltroTexto))
        {
            var txt = FiltroTexto.Trim().ToLowerInvariant();
            return (r.NomeCliente?.ToLowerInvariant().Contains(txt) == true) ||
                   (r.Proposta?.ToLowerInvariant().Contains(txt) == true) ||
                   (r.Apolice?.ToLowerInvariant().Contains(txt) == true) ||
                   (r.Placa?.ToLowerInvariant().Contains(txt) == true) ||
                   (r.DocumentoPrincipal?.ToLowerInvariant().Contains(txt) == true) ||
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
        var total = itens.Sum(r => r.PremioTotal);
        var marcados = itens.Count(r => r.IsChecked);
        Resumo = marcados > 0
            ? $"{itens.Count} registro(s) · {marcados} marcado(s) · Prêmio total: {total:C2}"
            : $"{itens.Count} registro(s) · Prêmio total: {total:C2}";
        OnPropertyChanged(nameof(TemMarcados));
    }

    [RelayCommand]
    private void LimparFiltros()
    {
        FiltroTexto = string.Empty;
        FiltroStatus = string.Empty;
        FiltroSeguradora = string.Empty;
        FiltroVendedor = string.Empty;
        FiltroProdutor = string.Empty;
        MesSelecionado = "Todos";
    }

    // ── Atribuição em massa ────────────────────────────────────────────────────

    [RelayCommand]
    private async Task AtribuirEmMassa()
    {
        var marcados = _todos.Where(r => r.IsChecked).ToList();
        if (marcados.Count == 0) return;

        IsLoading = true;
        try
        {
            foreach (var r in marcados)
                r.NovoProdutor = NovoProdutorEmMassa;

            await _service.SalvarNovoProdutorEmMassaAsync(marcados);

            // Atualiza o filtro de produtores para refletir novas atribuições
            if (!string.IsNullOrEmpty(NovoProdutorEmMassa) && !ProdutoresDisponiveis.Contains(NovoProdutorEmMassa))
                ProdutoresDisponiveis.Add(NovoProdutorEmMassa);

            _view?.Refresh();
            AtualizarResumo();

            MessageBox.Show($"Produtor \"{NovoProdutorEmMassa}\" atribuído a {marcados.Count} registro(s).",
                "Atribuição concluída", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao atribuir produtor:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Edição ─────────────────────────────────────────────────────────────────

    [RelayCommand]
    private async Task SalvarEdicao(RelatorioRenovacao? reg)
    {
        if (reg == null) return;
        try { await _service.SalvarEdicaoAsync(reg); }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar edição:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Folha Amarela ──────────────────────────────────────────────────────────

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

    [RelayCommand]
    private async Task GerarFolhasMarcadas()
    {
        var marcados = ItensVisiveis().Where(r => r.IsChecked).ToList();
        if (marcados.Count == 0)
        {
            MessageBox.Show("Nenhum registro marcado.", "Aviso",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new OpenFolderDialog { Title = "Selecione a pasta de destino para as folhas amarelas" };
        if (dialog.ShowDialog() != true) return;
        var pasta = dialog.FolderName;

        IsLoading = true;
        try
        {
            var ids = marcados.Select(r => r.Id).ToList();
            var anexosPorId = await _anexoService.GetAnexosParaRegistrosAsync(ids);

            var itens = marcados
                .Select(r => (r, anexosPorId.GetValueOrDefault(r.Id, [])))
                .ToList()
                .AsReadOnly();

            var progresso = new Progress<(int Atual, int Total)>(p =>
                Resumo = $"Gerando pasta {p.Atual} de {p.Total}...");

            await _folhaService.GerarLoteAsync(itens, pasta, progresso);

            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pasta)
                { UseShellExecute = true });

            AtualizarResumo();
            MessageBox.Show($"{marcados.Count} pasta(s) gerada(s) em:\n{pasta}",
                "Concluído", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao gerar:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Anexos ─────────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(TemRegistroSelecionado))]
    private async Task AnexarArquivos()
    {
        if (RegistroSelecionado == null) return;

        var dialog = new OpenFileDialog
        {
            Title = "Selecionar arquivo(s) para anexar",
            Multiselect = true,
            Filter = "Todos os arquivos|*.*|Imagens|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|PDF|*.pdf|Word|*.docx;*.doc|Excel|*.xlsx;*.xls"
        };
        if (dialog.ShowDialog() != true) return;

        IsLoading = true;
        int ok = 0, erros = 0;
        foreach (var file in dialog.FileNames)
        {
            try { await _anexoService.AdicionarAsync(RegistroSelecionado.Id, file); ok++; }
            catch { erros++; }
        }
        IsLoading = false;

        var msg = erros == 0
            ? $"{ok} arquivo(s) anexado(s) com sucesso."
            : $"{ok} arquivo(s) anexado(s), {erros} com erro.";
        MessageBox.Show(msg, "Anexar arquivos", MessageBoxButton.OK,
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

    // ── Seleção em massa ───────────────────────────────────────────────────────

    [RelayCommand]
    private void MarcarTodos()
    {
        foreach (var r in ItensVisiveis()) r.IsChecked = true;
        AtualizarResumo();
    }

    [RelayCommand]
    private void DesmarcarTodos()
    {
        foreach (var r in _todos) r.IsChecked = false;
        AtualizarResumo();
    }

    public void NotificarMarcacao() => AtualizarResumo();

    // ── Helpers ────────────────────────────────────────────────────────────────

    private List<RelatorioRenovacao> ItensVisiveis() =>
        _view?.Cast<RelatorioRenovacao>().ToList() ?? [];

    private bool TemRegistroSelecionado() => RegistroSelecionado != null;

    partial void OnRegistroSelecionadoChanged(RelatorioRenovacao? value)
    {
        GerarFolhaAmarelaCommand.NotifyCanExecuteChanged();
        AnexarArquivosCommand.NotifyCanExecuteChanged();
        AbrirPastaAnexosCommand.NotifyCanExecuteChanged();
    }
}
