using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class NovosNegociosViewModel : ObservableObject
{
    private readonly RelatorioService _relatorioService;
    private List<NovoNegocio> _todosNegocios = [];

    [ObservableProperty] private ObservableCollection<ResumoImportacao> _importacoes = [];
    [ObservableProperty] private ResumoImportacao? _importacaoSelecionada;
    [ObservableProperty] private ObservableCollection<NovoNegocio> _negocios = [];
    [ObservableProperty] private string _filtroTexto = string.Empty;
    [ObservableProperty] private string _filtroStatus = "Todos";

    public string[] StatusOpcoes { get; } = ["Todos", "Novo", "Renovação", "Prospecção", "Mercado"];

    public NovosNegociosViewModel(RelatorioService relatorioService)
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
        if (value != null) _ = CarregarNegociosAsync(value.Importacao.Id);
    }

    partial void OnFiltroTextoChanged(string value) => AplicarFiltros();
    partial void OnFiltroStatusChanged(string value) => AplicarFiltros();

    private async Task CarregarNegociosAsync(int importacaoId)
    {
        _todosNegocios = await _relatorioService.GetNovosNegociosAsync(importacaoId);
        AplicarFiltros();
    }

    private void AplicarFiltros()
    {
        var query = _todosNegocios.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(FiltroTexto))
            query = query.Where(n => n.Segurado.Contains(FiltroTexto, StringComparison.OrdinalIgnoreCase)
                                  || n.Cia.Contains(FiltroTexto, StringComparison.OrdinalIgnoreCase)
                                  || n.Segmento.Contains(FiltroTexto, StringComparison.OrdinalIgnoreCase));

        if (FiltroStatus != "Todos")
            query = query.Where(n => n.Status.Equals(FiltroStatus, StringComparison.OrdinalIgnoreCase));

        Negocios = new ObservableCollection<NovoNegocio>(query);
    }
}
