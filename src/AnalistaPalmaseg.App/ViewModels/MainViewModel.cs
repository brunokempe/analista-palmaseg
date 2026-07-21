using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ImportacaoService _importacaoService;
    private readonly RelatorioService _relatorioService;
    private readonly AppDbContext _context;

    [ObservableProperty]
    private ObservableObject? _currentView;

    [ObservableProperty]
    private string _tituloAtivo = "Dashboard";

    [ObservableProperty]
    private bool _isLoading;

    public DashboardViewModel DashboardVm { get; }
    public RenovacoesViewModel RenovacoesVm { get; }
    public NovosNegociosViewModel NovosNegociosVm { get; }
    public PendentesViewModel PendentesVm { get; }
    public RetencaoViewModel RetencaoVm { get; }

    public MainViewModel(
        ImportacaoService importacaoService,
        RelatorioService relatorioService,
        AppDbContext context,
        DashboardViewModel dashboardVm,
        RenovacoesViewModel renovacoesVm,
        NovosNegociosViewModel novosNegociosVm,
        PendentesViewModel pendentesVm,
        RetencaoViewModel retencaoVm)
    {
        _importacaoService = importacaoService;
        _relatorioService = relatorioService;
        _context = context;

        DashboardVm = dashboardVm;
        RenovacoesVm = renovacoesVm;
        NovosNegociosVm = novosNegociosVm;
        PendentesVm = pendentesVm;
        RetencaoVm = retencaoVm;

        _currentView = dashboardVm;
    }

    [RelayCommand]
    private void NavDashboard() { CurrentView = DashboardVm; TituloAtivo = "Dashboard"; }

    [RelayCommand]
    private void NavRenovacoes() { CurrentView = RenovacoesVm; TituloAtivo = "Renovações"; }

    [RelayCommand]
    private void NavNovosNegocios() { CurrentView = NovosNegociosVm; TituloAtivo = "Novos negócios"; }

    [RelayCommand]
    private void NavPendentes() { CurrentView = PendentesVm; TituloAtivo = "Pendentes em aberto"; }

    [RelayCommand]
    private void NavRetencao() { CurrentView = RetencaoVm; TituloAtivo = "Evolução da retenção"; }

    [RelayCommand]
    private async Task ImportarArquivoAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Selecionar planilha do produtor",
            Filter = "Planilhas ODS|*.ods|Todas as planilhas|*.ods;*.xlsx",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true) return;

        IsLoading = true;
        try
        {
            string? senha = null;

            var ext = Path.GetExtension(dialog.FileName).ToLower();
            if (ext == ".ods")
            {
                var senhaDialog = new Views.SenhaDialog();
                if (senhaDialog.ShowDialog() == true)
                    senha = senhaDialog.Senha;
            }

            var importacao = await _importacaoService.ImportarAsync(dialog.FileName, senha);

            await DashboardVm.CarregarAsync();
            await RenovacoesVm.CarregarAsync();
            await NovosNegociosVm.CarregarAsync();
            await PendentesVm.CarregarAsync();
            await RetencaoVm.CarregarAsync();
        }
        finally
        {
            IsLoading = false;
        }
    }
}
