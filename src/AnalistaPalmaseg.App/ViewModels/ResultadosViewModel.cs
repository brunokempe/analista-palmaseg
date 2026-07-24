using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class ResultadosViewModel : ObservableObject
{
    private readonly RelatorioService _relatorioService;

    [ObservableProperty] private ObservableCollection<ResumoImportacao> _importacoes = [];
    [ObservableProperty] private ResumoImportacao? _importacaoSelecionada;
    [ObservableProperty] private ObservableCollection<ResultadoMeta> _resultados = [];

    public ResultadosViewModel(RelatorioService relatorioService)
    {
        _relatorioService = relatorioService;
    }

    public async Task CarregarAsync()
    {
        var lista = await _relatorioService.GetResumoAsync();
        Importacoes = new ObservableCollection<ResumoImportacao>(lista);

        var currentId = ImportacaoSelecionada?.Importacao.Id;
        var target = currentId.HasValue
            ? Importacoes.FirstOrDefault(i => i.Importacao.Id == currentId.Value) ?? Importacoes.FirstOrDefault()
            : Importacoes.FirstOrDefault();

        ImportacaoSelecionada = null;
        ImportacaoSelecionada = target;

        // Aguarda explicitamente para garantir que os dados estão prontos ao exibir a view
        if (target != null)
            await CarregarResultadosAsync(target.Importacao.Id);
    }

    partial void OnImportacaoSelecionadaChanged(ResumoImportacao? value)
    {
        if (value != null)
            _ = CarregarResultadosAsync(value.Importacao.Id);
    }

    private async Task CarregarResultadosAsync(int importacaoId)
    {
        var lista = await _relatorioService.GetResultadosAsync(importacaoId);
        Resultados = new ObservableCollection<ResultadoMeta>(lista);
    }
}
