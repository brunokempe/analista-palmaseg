using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class PendentesViewModel : ObservableObject
{
    private readonly RelatorioService _relatorioService;

    [ObservableProperty] private ObservableCollection<ResumoImportacao> _importacoes = [];
    [ObservableProperty] private ResumoImportacao? _importacaoSelecionada;
    [ObservableProperty] private ObservableCollection<Renovacao> _pendentes = [];

    public PendentesViewModel(RelatorioService relatorioService)
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
        if (value != null) _ = CarregarPendentesAsync(value.Importacao.Id);
    }

    private async Task CarregarPendentesAsync(int importacaoId)
    {
        var lista = await _relatorioService.GetPendentesAsync(importacaoId);
        Pendentes = new ObservableCollection<Renovacao>(lista);
    }
}
