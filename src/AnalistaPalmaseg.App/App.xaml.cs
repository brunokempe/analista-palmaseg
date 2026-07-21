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

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                var dbPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "AnalistaPalmaseg", "dados.db");
                Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

                services.AddDbContext<AppDbContext>(opts =>
                    opts.UseSqlite($"Data Source={dbPath}"));

                services.AddTransient<DatabaseInitializer>();
                services.AddTransient<ImportacaoService>();
                services.AddTransient<RelatorioService>();

                services.AddTransient<DashboardViewModel>();
                services.AddTransient<RenovacoesViewModel>();
                services.AddTransient<NovosNegociosViewModel>();
                services.AddTransient<PendentesViewModel>();
                services.AddTransient<RetencaoViewModel>();
                services.AddTransient<MainViewModel>();
                services.AddTransient<MainWindow>();
            })
            .Build();

        await _host.StartAsync();

        var initializer = _host.Services.GetRequiredService<DatabaseInitializer>();
        initializer.Initialize();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();

        // Register DataTemplates for view routing
        RegisterDataTemplates();

        // Load initial data
        var dashVm = _host.Services.GetRequiredService<MainViewModel>();
        await dashVm.DashboardVm.CarregarAsync();
        await dashVm.RenovacoesVm.CarregarAsync();
        await dashVm.NovosNegociosVm.CarregarAsync();
        await dashVm.PendentesVm.CarregarAsync();
        await dashVm.RetencaoVm.CarregarAsync();

        mainWindow.Show();
    }

    private static void RegisterDataTemplates()
    {
        var resources = Current.Resources;

        var dashTemplate = new DataTemplate(typeof(DashboardViewModel));
        dashTemplate.VisualTree = new FrameworkElementFactory(typeof(DashboardView));
        resources.Add(new DataTemplateKey(typeof(DashboardViewModel)), dashTemplate);

        var renTemplate = new DataTemplate(typeof(RenovacoesViewModel));
        renTemplate.VisualTree = new FrameworkElementFactory(typeof(RenovacoesView));
        resources.Add(new DataTemplateKey(typeof(RenovacoesViewModel)), renTemplate);

        var novosTemplate = new DataTemplate(typeof(NovosNegociosViewModel));
        novosTemplate.VisualTree = new FrameworkElementFactory(typeof(NovosNegociosView));
        resources.Add(new DataTemplateKey(typeof(NovosNegociosViewModel)), novosTemplate);

        var pendTemplate = new DataTemplate(typeof(PendentesViewModel));
        pendTemplate.VisualTree = new FrameworkElementFactory(typeof(PendentesView));
        resources.Add(new DataTemplateKey(typeof(PendentesViewModel)), pendTemplate);

        var retTemplate = new DataTemplate(typeof(RetencaoViewModel));
        retTemplate.VisualTree = new FrameworkElementFactory(typeof(RetencaoView));
        resources.Add(new DataTemplateKey(typeof(RetencaoViewModel)), retTemplate);
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
