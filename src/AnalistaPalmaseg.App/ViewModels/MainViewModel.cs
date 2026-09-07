using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AnalistaPalmaseg.Core.Services;
using AnalistaPalmaseg.Core.Models;

namespace AnalistaPalmaseg.App.ViewModels;

public record MenuFavoritoItem(string Chave, string Titulo, string Icone);

public partial class MainViewModel : ObservableObject
{
    private readonly ImportacaoService _importacaoService;
    private readonly SessaoService _sessao;
    private readonly RelatorioRenovacaoService _relatorioRenovacaoService;
    private readonly FavoritoMenuService _favoritoMenuService;

    private readonly Dictionary<string, (string Titulo, string Icone, Func<Task> Executar)> _menuItems;
    private readonly HashSet<string> _favoritoKeys = [];

    [ObservableProperty] private ObservableObject? _currentView;
    [ObservableProperty] private string _tituloAtivo = "Início";
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<MenuFavoritoItem> Favoritos { get; } = [];
    public bool TemFavoritos => Favoritos.Count > 0;

    public bool this[string chave] => _favoritoKeys.Contains(chave);

    public bool IsAdmin => _sessao.IsAdmin;
    public string NomeUsuario => _sessao.NomeUsuario;

    // ── Sidebar / seções ──────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SidebarWidth),
                              nameof(CarteiraItemsVisibility),
                              nameof(RelatoriosItemsVisibility),
                              nameof(ApolicesItemsVisibility),
                              nameof(CadastrosItemsVisibility),
                              nameof(LeedsItemsVisibility),
                              nameof(GerenciadorItemsVisibility),
                              nameof(ComparativoItemsVisibility))]
    private bool _isSidebarExpanded = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CarteiraItemsVisibility))]
    private bool _isCarteiraExpanded = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RelatoriosItemsVisibility))]
    private bool _isRelatoriosExpanded = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ApolicesItemsVisibility))]
    private bool _isApolicesExpanded = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CadastrosItemsVisibility))]
    private bool _isCadastrosExpanded = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LeedsItemsVisibility))]
    private bool _isLeedsExpanded = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GerenciadorItemsVisibility))]
    private bool _isGerenciadorExpanded = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ComparativoItemsVisibility))]
    private bool _isComparativoExpanded = false;

    public double SidebarWidth => IsSidebarExpanded ? 220 : 56;
    public Visibility CarteiraItemsVisibility    => !IsSidebarExpanded || IsCarteiraExpanded    ? Visibility.Visible : Visibility.Collapsed;
    public Visibility RelatoriosItemsVisibility  => !IsSidebarExpanded || IsRelatoriosExpanded  ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ApolicesItemsVisibility    => !IsSidebarExpanded || IsApolicesExpanded    ? Visibility.Visible : Visibility.Collapsed;
    public Visibility CadastrosItemsVisibility   => !IsSidebarExpanded || IsCadastrosExpanded   ? Visibility.Visible : Visibility.Collapsed;
    public Visibility LeedsItemsVisibility       => !IsSidebarExpanded || IsLeedsExpanded       ? Visibility.Visible : Visibility.Collapsed;
    public Visibility GerenciadorItemsVisibility => !IsSidebarExpanded || IsGerenciadorExpanded ? Visibility.Visible : Visibility.Collapsed;
    public Visibility ComparativoItemsVisibility => !IsSidebarExpanded || IsComparativoExpanded ? Visibility.Visible : Visibility.Collapsed;

    // Análise de Carteira
    public InicioViewModel InicioVm { get; }
    public DashboardViewModel DashboardVm { get; }
    public RenovacoesViewModel RenovacoesVm { get; }
    public NovosNegociosViewModel NovosNegociosVm { get; }
    public PendentesViewModel PendentesVm { get; }
    public RetencaoViewModel RetencaoVm { get; }
    public ComparacaoViewModel ComparacaoVm { get; }
    public ResultadosViewModel ResultadosVm { get; }

    // Acompanhamento de Apólices
    public ApolicesDashboardViewModel ApolicesDashboardVm { get; }

    // Dashboard de Funcionários
    public FuncionariosDashboardViewModel FuncionariosDashboardVm { get; }

    // Admin
    public GerenciarUsuariosViewModel GerenciarUsuariosVm { get; }

    // Acompanhamento de Renovações (todos os usuários)
    public AcompanhamentoRenovacoesViewModel AcompanhamentoRenovacoesVm { get; }

    // Seguros Novos
    public SeguroNovosViewModel SeguroNovosVm { get; }

    // Relatórios
    public RelatorioEmissaoViewModel RelatorioEmissaoVm { get; }

    // Gerenciador (admin)
    public GerenciadorRenovacoesViewModel    GerenciadorRenovacoesVm    { get; }
    public GerenciadorCotacoesViewModel      GerenciadorCotacoesVm      { get; }
    public EmissaoDashboardViewModel         EmissaoDashboardVm         { get; }
    public DistribuicaoProdutorViewModel     DistribuicaoProdutorVm     { get; }

    // Controle de Boletos
    public ControleBoletosViewModel ControleBoletosVm { get; }

    // Clientes
    public ClientesViewModel ClientesVm { get; }

    // Pastas por Produtores
    public PastasProdutorViewModel PastasProdutorVm { get; }

    // Leeds
    public LeadsViewModel LeadsVm { get; }

    // Metas (admin)
    public DefinicoesMetasViewModel DefinicoesMetasVm { get; }
    public DashboardMetasViewModel DashboardMetasVm { get; }

    public MainViewModel(
        ImportacaoService importacaoService,
        SessaoService sessao,
        InicioViewModel inicioVm,
        DashboardViewModel dashboardVm,
        RenovacoesViewModel renovacoesVm,
        NovosNegociosViewModel novosNegociosVm,
        PendentesViewModel pendentesVm,
        RetencaoViewModel retencaoVm,
        ComparacaoViewModel comparacaoVm,
        ResultadosViewModel resultadosVm,
        ApolicesDashboardViewModel apolicesDashboardVm,
        FuncionariosDashboardViewModel funcionariosDashboardVm,
        GerenciarUsuariosViewModel gerenciarUsuariosVm,
        AcompanhamentoRenovacoesViewModel acompanhamentoRenovacoesVm,
        GerenciadorRenovacoesViewModel gerenciadorRenovacoesVm,
        GerenciadorCotacoesViewModel gerenciadorCotacoesVm,
        EmissaoDashboardViewModel emissaoDashboardVm,
        SeguroNovosViewModel seguroNovosVm,
        RelatorioEmissaoViewModel relatorioEmissaoVm,
        DefinicoesMetasViewModel definicoesMetasVm,
        DashboardMetasViewModel dashboardMetasVm,
        ControleBoletosViewModel controleBoletosVm,
        ClientesViewModel clientesVm,
        LeadsViewModel leadsVm,
        DistribuicaoProdutorViewModel distribuicaoProdutorVm,
        PastasProdutorViewModel pastasProdutorVm,
        RelatorioRenovacaoService relatorioRenovacaoService,
        FavoritoMenuService favoritoMenuService)
    {
        _importacaoService = importacaoService;
        _sessao = sessao;
        _favoritoMenuService = favoritoMenuService;

        InicioVm = inicioVm;
        DashboardVm = dashboardVm;
        RenovacoesVm = renovacoesVm;
        NovosNegociosVm = novosNegociosVm;
        PendentesVm = pendentesVm;
        RetencaoVm = retencaoVm;
        ComparacaoVm = comparacaoVm;
        ResultadosVm = resultadosVm;
        ApolicesDashboardVm = apolicesDashboardVm;
        FuncionariosDashboardVm = funcionariosDashboardVm;
        GerenciarUsuariosVm = gerenciarUsuariosVm;
        AcompanhamentoRenovacoesVm = acompanhamentoRenovacoesVm;
        GerenciadorRenovacoesVm = gerenciadorRenovacoesVm;
        GerenciadorCotacoesVm = gerenciadorCotacoesVm;
        EmissaoDashboardVm = emissaoDashboardVm;
        SeguroNovosVm = seguroNovosVm;
        RelatorioEmissaoVm = relatorioEmissaoVm;
        DefinicoesMetasVm = definicoesMetasVm;
        DashboardMetasVm = dashboardMetasVm;
        ControleBoletosVm = controleBoletosVm;
        ClientesVm             = clientesVm;
        LeadsVm                = leadsVm;
        DistribuicaoProdutorVm = distribuicaoProdutorVm;
        PastasProdutorVm = pastasProdutorVm;
        _relatorioRenovacaoService = relatorioRenovacaoService;

        WeakReferenceMessenger.Default.Register<AbrirClienteMessage>(this, (_, m) =>
        {
            _ = AbrirCadastroClienteAsync(m.DocumentoPrincipal);
        });

        _currentView = inicioVm;

        _menuItems = new Dictionary<string, (string, string, Func<Task>)>
        {
            ["Inicio"] = ("Início", "HomeOutline", () => { NavInicio(); return Task.CompletedTask; }),
            ["AcompanhamentoRenovacoes"] = ("Renovações", "ClipboardCheckOutline", NavAcompanhamentoRenovacoesAsync),
            ["SeguroNovos"] = ("Seguros Novos", "ShieldPlusOutline", NavSeguroNovosAsync),
            ["EmissaoDashboard"] = ("Dashboard de Emissões", "FileCheckOutline", NavEmissaoDashboardAsync),
            ["ControleBoletos"] = ("Controle de Boletos", "ReceiptTextCheckOutline", NavControleBoletosAsync),
            ["DashboardMetas"] = ("Dashboard de Metas", "TrophyOutline", NavDashboardMetasAsync),
            ["Clientes"] = ("Clientes", "AccountGroupOutline", NavClientesAsync),
            ["PastasProdutor"] = ("Pastas por Produtores", "FolderAccountOutline", NavPastasProdutorAsync),
            ["Leads"] = ("Cotações", "AccountConvertOutline", NavLeadsAsync),
            ["RelatorioEmissao"] = ("Emissões por produtor", "ChartLine", NavRelatorioEmissaoAsync),
            ["Dashboard"] = ("Dashboard", "ViewDashboardOutline", () => { NavDashboard(); return Task.CompletedTask; }),
            ["Renovacoes"] = ("Renovações (Comparativo)", "ClipboardTextOutline", () => { NavRenovacoes(); return Task.CompletedTask; }),
            ["NovosNegocios"] = ("Novos negócios", "PlusBoxOutline", () => { NavNovosNegocios(); return Task.CompletedTask; }),
            ["Pendentes"] = ("Pendentes em aberto", "ClockOutline", () => { NavPendentes(); return Task.CompletedTask; }),
            ["Retencao"] = ("Evolução retenção", "TrendingUp", () => { NavRetencao(); return Task.CompletedTask; }),
            ["Comparacao"] = ("Comparação produtores", "ChartBarStacked", () => { NavComparacao(); return Task.CompletedTask; }),
            ["Resultados"] = ("Resultado — Metas", "CheckboxMarkedCircleOutline", () => { NavResultados(); return Task.CompletedTask; }),
            ["ApolicesDashboard"] = ("Acomp. Apólices", "FileDocumentOutline", () => { NavApolicesDashboard(); return Task.CompletedTask; }),
            ["FuncionariosDashboard"] = ("Dashboard Funcionários", "AccountMultipleOutline", () => { NavFuncionariosDashboard(); return Task.CompletedTask; }),
            ["DistribuicaoProdutor"] = ("Dist. por Produtor", "AccountGroupOutline", NavDistribuicaoProdutorAsync),
            ["GerenciadorRenovacoes"] = ("Renovações (Gerenciador)", "FileRefreshOutline", NavGerenciadorRenovacoesAsync),
            ["DefinicoesMetas"] = ("Definições de Metas", "ChartTimeline", NavDefinicoesMetasAsync),
            ["GerenciarUsuarios"] = ("Usuários", "AccountCogOutline", NavGerenciarUsuariosAsync),
        };
    }

    // ── Favoritos ──────────────────────────────────────────────
    public async Task CarregarFavoritosAsync()
    {
        var usuarioId = _sessao.UsuarioAtual?.Id;
        if (usuarioId is null) return;

        var chaves = await _favoritoMenuService.GetFavoritosAsync(usuarioId.Value);
        _favoritoKeys.Clear();
        foreach (var chave in chaves) _favoritoKeys.Add(chave);

        AtualizarListaFavoritos();
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
    }

    private void AtualizarListaFavoritos()
    {
        Favoritos.Clear();
        foreach (var chave in _favoritoKeys)
        {
            if (_menuItems.TryGetValue(chave, out var info))
                Favoritos.Add(new MenuFavoritoItem(chave, info.Titulo, info.Icone));
        }
        OnPropertyChanged(nameof(TemFavoritos));
    }

    [RelayCommand]
    private async Task ToggleFavoritoAsync(string chave)
    {
        var usuarioId = _sessao.UsuarioAtual?.Id;
        if (usuarioId is null || !_menuItems.ContainsKey(chave)) return;

        var agoraFavorito = await _favoritoMenuService.AlternarFavoritoAsync(usuarioId.Value, chave);

        if (agoraFavorito) _favoritoKeys.Add(chave);
        else _favoritoKeys.Remove(chave);

        AtualizarListaFavoritos();
        OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
    }

    [RelayCommand]
    private async Task NavFavoritoAsync(string chave)
    {
        if (_menuItems.TryGetValue(chave, out var info))
            await info.Executar();
    }

    // ── Toggle sidebar / seções ───────────────────────────────
    [RelayCommand] private void ToggleSidebar()      => IsSidebarExpanded      = !IsSidebarExpanded;
    [RelayCommand] private void ToggleCarteira()     => IsCarteiraExpanded     = !IsCarteiraExpanded;
    [RelayCommand] private void ToggleRelatorios()   => IsRelatoriosExpanded   = !IsRelatoriosExpanded;
    [RelayCommand] private void ToggleApolices()     => IsApolicesExpanded     = !IsApolicesExpanded;
    [RelayCommand] private void ToggleCadastros()    => IsCadastrosExpanded    = !IsCadastrosExpanded;
    [RelayCommand] private void ToggleLeeds()        => IsLeedsExpanded        = !IsLeedsExpanded;
    [RelayCommand] private void ToggleGerenciador()  => IsGerenciadorExpanded  = !IsGerenciadorExpanded;
    [RelayCommand] private void ToggleComparativo()  => IsComparativoExpanded  = !IsComparativoExpanded;

    // ── Navegação ──────────────────────────────────────────────
    [RelayCommand] private void NavInicio()           { CurrentView = InicioVm;           TituloAtivo = "Início"; }
    [RelayCommand] private void NavDashboard()        { CurrentView = DashboardVm;         TituloAtivo = "Dashboard"; }
    [RelayCommand] private void NavRenovacoes()       { CurrentView = RenovacoesVm;        TituloAtivo = "Renovações"; }
    [RelayCommand] private void NavNovosNegocios()    { CurrentView = NovosNegociosVm;     TituloAtivo = "Novos negócios"; }
    [RelayCommand] private void NavPendentes()        { CurrentView = PendentesVm;         TituloAtivo = "Pendentes em aberto"; }
    [RelayCommand] private void NavRetencao()         { CurrentView = RetencaoVm;          TituloAtivo = "Evolução da retenção"; }
    [RelayCommand] private void NavComparacao()       { CurrentView = ComparacaoVm;        TituloAtivo = "Comparação de produtores"; }
    [RelayCommand] private void NavResultados()       { CurrentView = ResultadosVm;        TituloAtivo = "Resultado — Metas"; }
    [RelayCommand] private void NavApolicesDashboard()    { CurrentView = ApolicesDashboardVm;    TituloAtivo = "Acompanhamento de Apólices"; }
    [RelayCommand] private void NavFuncionariosDashboard(){ CurrentView = FuncionariosDashboardVm; TituloAtivo = "Dashboard de Funcionários"; }

    [RelayCommand]
    private async Task NavRelatorioEmissaoAsync()
    {
        await RelatorioEmissaoVm.CarregarAsync();
        CurrentView = RelatorioEmissaoVm;
        TituloAtivo = "Emissões por Produtor";
    }

    [RelayCommand]
    private async Task NavSeguroNovosAsync()
    {
        await SeguroNovosVm.CarregarAsync();
        CurrentView = SeguroNovosVm;
        TituloAtivo = "Seguros Novos";
    }

    [RelayCommand]
    private async Task NavAcompanhamentoRenovacoesAsync()
    {
        await AcompanhamentoRenovacoesVm.CarregarAsync();
        CurrentView = AcompanhamentoRenovacoesVm;
        TituloAtivo = "Acompanhamento de Renovações";
    }
    [RelayCommand] private void Sair() => Application.Current.Shutdown();

    [RelayCommand]
    private async Task NavGerenciarUsuariosAsync()
    {
        await GerenciarUsuariosVm.CarregarAsync();
        CurrentView = GerenciarUsuariosVm;
        TituloAtivo = "Gerenciar Usuários";
    }

    [RelayCommand]
    private async Task NavDistribuicaoProdutorAsync()
    {
        await DistribuicaoProdutorVm.CarregarAsync();
        CurrentView = DistribuicaoProdutorVm;
        TituloAtivo = "Distribuição por Produtor";
    }

    [RelayCommand]
    private async Task NavGerenciadorRenovacoesAsync()
    {
        await GerenciadorRenovacoesVm.CarregarAsync();
        CurrentView = GerenciadorRenovacoesVm;
        TituloAtivo = "Renovações";
    }

    [RelayCommand]
    private void NavGerenciadorCotacoes()
    {
        CurrentView = GerenciadorCotacoesVm;
        TituloAtivo = "Cotações";
    }

    [RelayCommand]
    private async Task NavDefinicoesMetasAsync()
    {
        await DefinicoesMetasVm.CarregarAsync();
        CurrentView = DefinicoesMetasVm;
        TituloAtivo = "Definições de Metas";
    }

    [RelayCommand]
    private async Task NavDashboardMetasAsync()
    {
        await DashboardMetasVm.CarregarAsync();
        CurrentView = DashboardMetasVm;
        TituloAtivo = "Dashboard de Metas";
    }

    [RelayCommand]
    private async Task NavClientesAsync()
    {
        await ClientesVm.CarregarAsync();
        CurrentView = ClientesVm;
        TituloAtivo = "Cadastro de Clientes";
    }

    private async Task AbrirCadastroClienteAsync(string documentoPrincipal)
    {
        await NavClientesAsync();
        ClientesVm.SelecionarPorDocumento(documentoPrincipal);
    }

    [RelayCommand]
    private async Task NavPastasProdutorAsync()
    {
        await PastasProdutorVm.CarregarAsync();
        CurrentView = PastasProdutorVm;
        TituloAtivo = "Pastas por Produtores";
    }

    [RelayCommand]
    private async Task NavLeadsAsync()
    {
        await LeadsVm.CarregarAsync();
        CurrentView = LeadsVm;
        TituloAtivo = "Leeds — Cotações";
    }

    [RelayCommand]
    private async Task NavControleBoletosAsync()
    {
        await ControleBoletosVm.CarregarAsync();
        CurrentView = ControleBoletosVm;
        TituloAtivo = "Controle de Boletos";
    }

    [RelayCommand]
    private async Task NavEmissaoDashboardAsync()
    {
        await EmissaoDashboardVm.CarregarAsync();
        CurrentView = EmissaoDashboardVm;
        TituloAtivo = "Dashboard de Emissão";
    }

    // ── Importação — Relatório de Renovações ─────────────────
    [RelayCommand]
    private async Task ImportarRelatorioRenovacaoAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Selecionar relatório de renovações",
            Filter = "Planilhas Excel|*.xlsx;*.xls",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        IsLoading = true;
        try
        {
            var inseridos = await _relatorioRenovacaoService.ImportarAsync(dialog.FileName);
            System.Windows.MessageBox.Show(
                $"Importação concluída! {inseridos} novo(s) registro(s) inserido(s).",
                "Relatório importado",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            await NavGerenciadorRenovacoesAsync();
        }
        catch (IOException ex) when (ex.HResult == unchecked((int)0x80070020)
            || ex.Message.Contains("used by another process", StringComparison.OrdinalIgnoreCase))
        {
            System.Windows.MessageBox.Show(
                "O arquivo está aberto em outro programa. Feche-o e tente novamente.",
                "Arquivo em uso", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, "Erro ao importar",
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Importação — Análise de Carteira ──────────────────────
    [RelayCommand]
    private async Task ImportarArquivoAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Selecionar planilhas do produtor",
            Filter = "Planilhas ODS|*.ods|Todas as planilhas|*.ods;*.xlsx",
            Multiselect = true
        };

        if (dialog.ShowDialog() != true) return;

        var arquivos = dialog.FileNames;
        IsLoading = true;
        try
        {
            string? senha = null;
            var temOds = arquivos.Any(f => Path.GetExtension(f).Equals(".ods", StringComparison.OrdinalIgnoreCase));
            if (temOds)
            {
                var senhaDialog = new Views.SenhaDialog
                {
                    Owner = System.Windows.Application.Current.MainWindow
                };
                if (senhaDialog.ShowDialog() == true)
                    senha = senhaDialog.Senha;
            }

            var erros = new List<string>();
            foreach (var arquivo in arquivos)
            {
                try
                {
                    var arquivoSenha = arquivo.EndsWith(".ods", StringComparison.OrdinalIgnoreCase) ? senha : null;
                    await _importacaoService.ImportarAsync(arquivo, arquivoSenha);
                }
                catch (IOException ex) when (ex.HResult == unchecked((int)0x80070020)
                    || ex.Message.Contains("used by another process", StringComparison.OrdinalIgnoreCase)
                    || ex.Message.Contains("sendo usado", StringComparison.OrdinalIgnoreCase))
                {
                    erros.Add($"• {Path.GetFileName(arquivo)}: arquivo aberto em outro programa. Feche-o e tente novamente.");
                }
                catch (Exception ex)
                {
                    erros.Add($"• {Path.GetFileName(arquivo)}: {ex.Message}");
                }
            }

            if (erros.Count > 0)
                System.Windows.MessageBox.Show(string.Join("\n", erros), "Erro ao importar",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);

            await RefreshCarteiraViewsAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }

    // ── Refresh ───────────────────────────────────────────────
    private async Task RefreshCarteiraViewsAsync()
    {
        await InicioVm.CarregarAsync();
        await DashboardVm.CarregarAsync();
        await RenovacoesVm.CarregarAsync();
        await NovosNegociosVm.CarregarAsync();
        await PendentesVm.CarregarAsync();
        await RetencaoVm.CarregarAsync();
        await ComparacaoVm.CarregarAsync();
        await ResultadosVm.CarregarAsync();
        await FuncionariosDashboardVm.CarregarAsync();
    }

}
