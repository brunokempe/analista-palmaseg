using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class FuncionariosDashboardViewModel : ObservableObject
{
    private readonly RelatorioService _relatorioService;

    [ObservableProperty] private ObservableCollection<string> _nomesDisponiveis = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Periodos))]
    private string? _nomeSelecionado;

    [ObservableProperty] private ObservableCollection<PeriodoFuncionarioVm> _periodos = [];

    public FuncionariosDashboardViewModel(RelatorioService relatorioService)
    {
        _relatorioService = relatorioService;
    }

    public async Task CarregarAsync()
    {
        var nomes = await _relatorioService.GetFuncionariosNomesAsync();
        NomesDisponiveis = new ObservableCollection<string>(nomes);

        if (NomeSelecionado != null && NomesDisponiveis.Contains(NomeSelecionado))
        {
            // Força recarga dos períodos para o funcionário já selecionado
            await CarregarPeriodosAsync(NomeSelecionado);
        }
        else
        {
            NomeSelecionado = NomesDisponiveis.FirstOrDefault();
        }
    }

    partial void OnNomeSelecionadoChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
            _ = CarregarPeriodosAsync(value);
        else
            Periodos = [];
    }

    private async Task CarregarPeriodosAsync(string nome)
    {
        try
        {
            var timeline = await _relatorioService.GetTimelineFuncionarioAsync(nome);
            Periodos = new ObservableCollection<PeriodoFuncionarioVm>(
                timeline.Select(t => new PeriodoFuncionarioVm
                {
                    Importacao = t.Importacao,
                    Resultados = t.Resultados
                })
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Timeline] ERRO: {ex}");
            Periodos = [];
        }
    }
}
