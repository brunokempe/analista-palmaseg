using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class RenovacoesViewModel : ObservableObject
{
    private readonly RelatorioService _relatorioService;
    private List<Renovacao> _todasRenovacoes = [];

    [ObservableProperty] private ObservableCollection<ResumoImportacao> _importacoes = [];
    [ObservableProperty] private ResumoImportacao? _importacaoSelecionada;
    [ObservableProperty] private ObservableCollection<Renovacao> _renovacoes = [];
    [ObservableProperty] private string _filtroTexto = string.Empty;
    [ObservableProperty] private string _filtroStatus = "Todos";

    public string[] StatusOpcoes { get; } = ["Todos", "Ren.Palma", "Procurado", "Pendente", "Agendado", "Não renovado"];

    public RenovacoesViewModel(RelatorioService relatorioService)
    {
        _relatorioService = relatorioService;
    }

    public async Task CarregarAsync()
    {
        var lista = await _relatorioService.GetResumoAsync();
        Importacoes = new ObservableCollection<ResumoImportacao>(lista);
        ImportacaoSelecionada = Importacoes.FirstOrDefault();
    }

    partial void OnImportacaoSelecionadaChanged(ResumoImportacao? value)
    {
        if (value != null) _ = CarregarRenovacoesAsync(value.Importacao.Id);
    }

    partial void OnFiltroTextoChanged(string value) => AplicarFiltros();
    partial void OnFiltroStatusChanged(string value) => AplicarFiltros();

    private async Task CarregarRenovacoesAsync(int importacaoId)
    {
        _todasRenovacoes = await _relatorioService.GetRenovacoesAsync(importacaoId);
        AplicarFiltros();
    }

    private void AplicarFiltros()
    {
        var query = _todasRenovacoes.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(FiltroTexto))
            query = query.Where(r => r.Segurado.Contains(FiltroTexto, StringComparison.OrdinalIgnoreCase)
                                  || r.Cia.Contains(FiltroTexto, StringComparison.OrdinalIgnoreCase)
                                  || r.Ramo.Contains(FiltroTexto, StringComparison.OrdinalIgnoreCase));

        if (FiltroStatus != "Todos")
            query = query.Where(r => r.Status == FiltroStatus);

        Renovacoes = new ObservableCollection<Renovacao>(query);
    }
}
