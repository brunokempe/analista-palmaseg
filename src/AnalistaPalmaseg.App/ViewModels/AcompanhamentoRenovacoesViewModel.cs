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
    private readonly ClienteService _clienteService;
    private List<RelatorioRenovacao> _todos = [];
    private ListCollectionView? _view;
    private CancellationTokenSource? _debounceCts;
    private int _mesFiltroAno;
    private int _mesFiltroMes;
    private readonly Dictionary<string, (int Year, int Month)> _mesLookup = [];
    private Dictionary<int, string> _situacaoAnterior = [];
    private bool _filtroProdutorInicializado;

    [ObservableProperty] private ICollectionView? _registrosView;
    [ObservableProperty] private RelatorioRenovacao? _registroSelecionado;
    [ObservableProperty] private string _filtroTexto = string.Empty;
    [ObservableProperty] private bool _somenteComProdutor = true;
    [ObservableProperty] private string _mesSelecionado = "Todos";
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _resumo = string.Empty;
    [ObservableProperty] private bool _temAlertaCritico;
    [ObservableProperty] private string _alertaCriticoTexto = string.Empty;

    public bool IsAdmin => _sessao.IsAdmin;
    public bool TemRegistroSelecionado => RegistroSelecionado != null;

    public string[] SituacoesEditar { get; } =
        ["À Renovar", "Agendado", "Calculado", "Procurado", "Ren. Palma", "Ren. Outro", "Não renovado", "Recusado", "Emitido"];

    public string[] SituacoesFiltrar { get; } =
        ["À Renovar", "Agendado", "Calculado", "Procurado", "Ren. Palma", "Ren. Outro", "Não renovado", "Recusado", "Emitido"];

    public ObservableCollection<string> ProdutoresDisponiveis { get; } = [];
    public ObservableCollection<string> RamosDisponiveis { get; } = [];
    public ObservableCollection<string> MesesDisponiveis { get; } = [];

    // Filtros de múltipla seleção — vazio significa "sem filtro" (mostra todos)
    public ObservableCollection<string> SituacoesSelecionadas { get; } = [];
    public ObservableCollection<string> ProdutoresSelecionados { get; } = [];
    public ObservableCollection<string> RamosSelecionados { get; } = [];

    public AcompanhamentoRenovacoesViewModel(
        RelatorioRenovacaoService service,
        SessaoService sessao,
        FolhaAmarelaService folhaService,
        AnexoService anexoService,
        ClienteService clienteService)
    {
        _service = service;
        _sessao = sessao;
        _folhaService = folhaService;
        _anexoService = anexoService;
        _clienteService = clienteService;

        foreach (var colecao in new[] { SituacoesSelecionadas, ProdutoresSelecionados, RamosSelecionados })
            colecao.CollectionChanged += (_, _) => AplicarFiltro();
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

            var clientes = await _clienteService.GetTodosAsync();
            var clientePorCpf = clientes
                .Where(c => !string.IsNullOrWhiteSpace(c.Cpf))
                .ToDictionary(c => c.Cpf, StringComparer.OrdinalIgnoreCase);

            foreach (var r in _todos)
            {
                if (r.DocumentoPrincipal != null &&
                    clientePorCpf.TryGetValue(r.DocumentoPrincipal, out var cliente))
                {
                    if (!string.IsNullOrWhiteSpace(cliente.Nome))
                        r.NomeCliente = cliente.Nome;
                    r.ClienteHistorico = cliente.Historico;
                }
            }

            _situacaoAnterior = _todos.ToDictionary(r => r.Id, r => r.SituacaoAcompanhamento);

            var prods = await _service.GetNovoProdutorDistinctAsync();
            ProdutoresDisponiveis.Clear();
            foreach (var p in prods) ProdutoresDisponiveis.Add(p);

            if (!_filtroProdutorInicializado)
            {
                _filtroProdutorInicializado = true;
                if (ProdutoresDisponiveis.Contains(_sessao.NomeUsuario))
                    ProdutoresSelecionados.Add(_sessao.NomeUsuario);
            }

            var ramos = await _service.GetRamosDistinctAsync();
            RamosDisponiveis.Clear();
            foreach (var r in ramos) RamosDisponiveis.Add(r);

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

            // Padrão ao abrir: grupos ordenados pelo vencimento mais próximo (crescente), e
            // dentro de cada grupo também por vencimento crescente.
            var menorVencimentoPorCliente = _todos
                .GroupBy(r => r.NomeCliente ?? string.Empty)
                .ToDictionary(g => g.Key, g => g.Min(r => r.VigenciaFinal));

            _view.CustomSort = Comparer<object>.Create((a, b) =>
            {
                var ra = (RelatorioRenovacao)a;
                var rb = (RelatorioRenovacao)b;
                var nomeA = ra.NomeCliente ?? string.Empty;
                var nomeB = rb.NomeCliente ?? string.Empty;
                if (nomeA != nomeB)
                {
                    var cmpGrupo = Nullable.Compare(menorVencimentoPorCliente[nomeA], menorVencimentoPorCliente[nomeB]);
                    return cmpGrupo != 0 ? cmpGrupo : string.CompareOrdinal(nomeA, nomeB);
                }
                return Nullable.Compare(ra.VigenciaFinal, rb.VigenciaFinal);
            });
            _view.Filter = FiltroItem;
            RegistrosView = _view;

            // Abre sempre na aba do mês mais recente (não em "Todos")
            if (MesesDisponiveis.Count > 1)
                MesSelecionado = MesesDisponiveis[^1];
            AtualizarResumo();
            AtualizarAlertaCriticos();
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
                Reverter(reg, limparFechamento: true);
                return;
            }

            if (reg.PercentualComissaoMinimo.HasValue &&
                (!reg.FechamentoComissao.HasValue || reg.FechamentoComissao.Value < reg.PercentualComissaoMinimo.Value))
            {
                MessageBox.Show(
                    $"A comissão informada ({reg.FechamentoComissao?.ToString("N2") ?? "não informada"}%) é menor que o " +
                    $"% mínimo de comissão definido para este registro ({reg.PercentualComissaoMinimo.Value:N2}%).\n\n" +
                    "A renovação não pode ser confirmada.",
                    "Comissão abaixo do mínimo", MessageBoxButton.OK, MessageBoxImage.Warning);
                Reverter(reg, limparFechamento: true);
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
                Reverter(reg, limparFechamento: true);
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

    private void Reverter(RelatorioRenovacao reg, bool limparFechamento = false)
    {
        var valorAnterior = _situacaoAnterior.GetValueOrDefault(reg.Id, "À Renovar");

        // Adiado para depois que o ComboBox terminar de processar a seleção atual —
        // revertendo de forma síncrona (ainda dentro do UpdateSource disparado pelo próprio
        // binding TwoWay) o WPF ignora o novo valor por proteção contra reentrância.
        Application.Current?.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
            new Action(() =>
            {
                reg.PropertyChanged -= OnItemPropertyChanged;
                reg.SituacaoAcompanhamento = valorAnterior;
                if (limparFechamento)
                {
                    reg.FechamentoSeguradora = null;
                    reg.FechamentoPremioLiquido = null;
                    reg.FechamentoFormaPagamento = null;
                    reg.FechamentoComissao = null;
                    reg.FechamentoParcelamento = null;
                    reg.FechamentoAssinatura = null;
                }
                reg.PropertyChanged += OnItemPropertyChanged;
            }));
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

    partial void OnSomenteComProdutorChanged(bool value) => AplicarFiltro();

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

        if (r.NovoProdutor == "Cancelado") return false;

        if (_mesFiltroAno > 0)
        {
            if (!r.VigenciaFinal.HasValue ||
                r.VigenciaFinal.Value.Year != _mesFiltroAno ||
                r.VigenciaFinal.Value.Month != _mesFiltroMes)
                return false;
        }

        if (SituacoesSelecionadas.Count > 0 && !SituacoesSelecionadas.Contains(r.SituacaoAcompanhamento)) return false;
        if (RamosSelecionados.Count > 0 && !RamosSelecionados.Contains(r.Ramo ?? string.Empty)) return false;

        if (SomenteComProdutor && string.IsNullOrWhiteSpace(r.NovoProdutor)) return false;
        if (ProdutoresSelecionados.Count > 0 && !ProdutoresSelecionados.Contains(r.NovoProdutor ?? string.Empty)) return false;

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
        AtualizarAlertaCriticos();
    }

    // Alerta de situações críticas (vencidas ou vencendo agora) — deliberadamente ignora o
    // filtro de mês/situação/ramo/texto, pois o objetivo é chamar atenção mesmo quando o
    // registro não está visível no grid no momento (ex.: aba do mês mais recente, que é a
    // aberta por padrão, não mostra vencimentos de meses anteriores já ultrapassados).
    private void AtualizarAlertaCriticos()
    {
        IEnumerable<RelatorioRenovacao> baseQuery = _todos.Where(r => r.NovoProdutor != "Cancelado");
        if (SomenteComProdutor)
            baseQuery = baseQuery.Where(r => !string.IsNullOrWhiteSpace(r.NovoProdutor));
        if (ProdutoresSelecionados.Count > 0)
            baseQuery = baseQuery.Where(r => ProdutoresSelecionados.Contains(r.NovoProdutor ?? string.Empty));

        var criticos = baseQuery.Where(r => r.SituacaoPendenteCritica).ToList();
        var vencidas = criticos.Count(r => r.RenovacaoVencida);
        var venceEmBreve = criticos.Count(r => r.RenovacaoVenceEmBreve);

        TemAlertaCritico = vencidas > 0 || venceEmBreve > 0;
        if (!TemAlertaCritico)
        {
            AlertaCriticoTexto = string.Empty;
            return;
        }

        var partes = new List<string>();
        if (vencidas > 0) partes.Add($"{vencidas} vencida(s) sem renovação");
        if (venceEmBreve > 0) partes.Add($"{venceEmBreve} vencendo agora");
        AlertaCriticoTexto = "⚠ " + string.Join(" · ", partes) + " — pode não estar visível no filtro/mês atual";
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
        AlterarVigenciaCommand.NotifyCanExecuteChanged();
        AbrirCadastroClienteCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void LimparFiltros()
    {
        FiltroTexto = string.Empty;
        SituacoesSelecionadas.Clear();
        ProdutoresSelecionados.Clear();
        RamosSelecionados.Clear();
        SomenteComProdutor = true;
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
    private async Task AlterarVigenciaAsync()
    {
        if (RegistroSelecionado == null) return;
        var reg = RegistroSelecionado;

        var dialog = new AnalistaPalmaseg.App.Views.AlterarVigenciaDialog(reg.NomeCliente ?? "Cliente", reg.VigenciaInicial, reg.VigenciaFinal)
        {
            Owner = Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true) return;

        reg.VigenciaInicial = dialog.VigenciaInicial;
        reg.VigenciaFinal = dialog.VigenciaFinal;

        try
        {
            await _service.SalvarVigenciaAsync(reg);
            EnviarRefreshDashboard(reg);
            await CarregarAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar vigência:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand(CanExecute = nameof(TemRegistroSelecionado))]
    private void AbrirCadastroCliente()
    {
        if (string.IsNullOrWhiteSpace(RegistroSelecionado?.DocumentoPrincipal)) return;
        WeakReferenceMessenger.Default.Send(new AbrirClienteMessage(RegistroSelecionado.DocumentoPrincipal));
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
