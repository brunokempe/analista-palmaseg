using System.IO;
using System.Windows;
using AnalistaPalmaseg.App.ViewModels;
using AnalistaPalmaseg.App.Views;
using AnalistaPalmaseg.Core.Data;
using AnalistaPalmaseg.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AnalistaPalmaseg.App;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Força cultura pt-BR para que StringFormat=C2 exiba R$
        var culture = new System.Globalization.CultureInfo("pt-BR");
        System.Threading.Thread.CurrentThread.CurrentCulture = culture;
        System.Threading.Thread.CurrentThread.CurrentUICulture = culture;
        System.Windows.FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(System.Windows.FrameworkElement),
            new System.Windows.FrameworkPropertyMetadata(
                System.Windows.Markup.XmlLanguage.GetLanguage(culture.IetfLanguageTag)));

        // Evita que o app feche automaticamente enquanto a janela de login está aberta
        // (ShutdownMode padrão OnLastWindowClose dispara quando o dialog fecha)
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                var dbPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "dados.db");

                services.AddDbContext<AppDbContext>(opts =>
                    opts.UseSqlite($"Data Source={dbPath}"));

                services.AddSingleton<SessaoService>();

                services.AddTransient<DatabaseInitializer>();
                services.AddTransient<ImportacaoService>();
                services.AddTransient<RelatorioService>();
                services.AddTransient<ApoliceService>();
                services.AddTransient<SeguroNovoService>();
                services.AddTransient<UsuarioService>();
                services.AddTransient<RelatorioRenovacaoService>();
                services.AddTransient<FolhaAmarelaService>();
                services.AddTransient<AnexoService>();
                services.AddTransient<MetaService>();
                services.AddTransient<ClienteService>();
                services.AddTransient<LeadService>();

                services.AddTransient<LoginViewModel>();
                services.AddTransient<LoginWindow>();

                services.AddTransient<InicioViewModel>();
                services.AddTransient<DashboardViewModel>();
                services.AddTransient<RenovacoesViewModel>();
                services.AddTransient<NovosNegociosViewModel>();
                services.AddTransient<PendentesViewModel>();
                services.AddTransient<RetencaoViewModel>();
                services.AddTransient<ComparacaoViewModel>();
                services.AddTransient<ResultadosViewModel>();
                services.AddTransient<ApolicesDashboardViewModel>();
                services.AddTransient<FuncionariosDashboardViewModel>();
                services.AddTransient<GerenciarUsuariosViewModel>();
                services.AddTransient<AcompanhamentoRenovacoesViewModel>();
                services.AddTransient<GerenciadorRenovacoesViewModel>();
                services.AddTransient<GerenciadorCotacoesViewModel>();
                services.AddTransient<EmissaoDashboardViewModel>();
                services.AddTransient<SeguroNovosViewModel>();
                services.AddTransient<RelatorioEmissaoViewModel>();
                services.AddTransient<DefinicoesMetasViewModel>();
                services.AddTransient<DashboardMetasViewModel>();
                services.AddTransient<ControleBoletosViewModel>();
                services.AddTransient<ClientesViewModel>();
                services.AddTransient<LeadsViewModel>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        var initializer = _host.Services.GetRequiredService<DatabaseInitializer>();
        initializer.Initialize();

        var loginWindow = _host.Services.GetRequiredService<LoginWindow>();
        if (loginWindow.ShowDialog() != true)
        {
            Shutdown();
            return;
        }

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;

        RegisterDataTemplates();

        var mainVm = (MainViewModel)mainWindow.DataContext;
        await mainVm.InicioVm.CarregarAsync();
        await mainVm.DashboardVm.CarregarAsync();
        await mainVm.RenovacoesVm.CarregarAsync();
        await mainVm.NovosNegociosVm.CarregarAsync();
        await mainVm.PendentesVm.CarregarAsync();
        await mainVm.RetencaoVm.CarregarAsync();
        await mainVm.ComparacaoVm.CarregarAsync();
        await mainVm.ResultadosVm.CarregarAsync();
        await mainVm.FuncionariosDashboardVm.CarregarAsync();

        // Agora que a janela principal está aberta, usa o modo padrão
        ShutdownMode = ShutdownMode.OnMainWindowClose;
        mainWindow.Show();
    }

    private static void RegisterDataTemplates()
    {
        var resources = Current.Resources;

        void Add<TViewModel, TView>() where TView : new()
        {
            var t = new DataTemplate(typeof(TViewModel));
            t.VisualTree = new FrameworkElementFactory(typeof(TView));
            resources.Add(new DataTemplateKey(typeof(TViewModel)), t);
        }

        Add<InicioViewModel, InicioView>();
        Add<DashboardViewModel, DashboardView>();
        Add<RenovacoesViewModel, RenovacoesView>();
        Add<NovosNegociosViewModel, NovosNegociosView>();
        Add<PendentesViewModel, PendentesView>();
        Add<RetencaoViewModel, RetencaoView>();
        Add<ComparacaoViewModel, ComparacaoView>();
        Add<ResultadosViewModel, ResultadosView>();
        Add<ApolicesDashboardViewModel, ApolicesDashboardView>();
        Add<FuncionariosDashboardViewModel, FuncionariosDashboardView>();
        Add<GerenciarUsuariosViewModel, GerenciarUsuariosView>();
        Add<AcompanhamentoRenovacoesViewModel, AcompanhamentoRenovacoesView>();
        Add<GerenciadorRenovacoesViewModel, GerenciadorRenovacoesView>();
        Add<GerenciadorCotacoesViewModel, GerenciadorCotacoesView>();
        Add<EmissaoDashboardViewModel, EmissaoDashboardView>();
        Add<SeguroNovosViewModel, SeguroNovosView>();
        Add<RelatorioEmissaoViewModel, RelatorioEmissaoView>();
        Add<DefinicoesMetasViewModel, DefinicoesMetasView>();
        Add<DashboardMetasViewModel, DashboardMetasView>();
        Add<ControleBoletosViewModel, ControleBoletosView>();
        Add<ClientesViewModel, ClientesView>();
        Add<LeadsViewModel, LeadsView>();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }
        base.OnExit(e);
    }
}
