using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class PendentesViewModel : ObservableObject
{
    private readonly RelatorioService _relatorioService;

    [ObservableProperty] private ObservableCollection<string> _nomesDisponiveis = [];
    [ObservableProperty] private string? _nomeSelecionado;
    [ObservableProperty] private ObservableCollection<PeriodoPendentesVm> _periodos = [];

    public PendentesViewModel(RelatorioService relatorioService)
    {
        _relatorioService = relatorioService;
    }

    public async Task CarregarAsync()
    {
        var produtores = await _relatorioService.GetProdutoresAsync();
        NomesDisponiveis = new ObservableCollection<string>(produtores);

        if (NomeSelecionado != null && NomesDisponiveis.Contains(NomeSelecionado))
            await CarregarPeriodosAsync(NomeSelecionado);
        else
            NomeSelecionado = NomesDisponiveis.FirstOrDefault();
    }

    partial void OnNomeSelecionadoChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
            _ = CarregarPeriodosAsync(value);
        else
            Periodos = [];
    }

    private async Task CarregarPeriodosAsync(string produtor)
    {
        try
        {
            var timeline = await _relatorioService.GetTimelinePendentesAsync(produtor);
            Periodos = new ObservableCollection<PeriodoPendentesVm>(
                timeline.Select(t => new PeriodoPendentesVm(t.Resumo, t.Pendentes))
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Timeline Pendentes] ERRO: {ex}");
            Periodos = [];
        }
    }
}
