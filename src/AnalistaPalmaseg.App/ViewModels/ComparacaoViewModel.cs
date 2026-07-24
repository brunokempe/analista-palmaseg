using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class ComparacaoViewModel : ObservableObject
{
    private readonly RelatorioService _relatorioService;

    [ObservableProperty] private ObservableCollection<ResumoImportacao> _resumos = [];

    public ComparacaoViewModel(RelatorioService relatorioService)
    {
        _relatorioService = relatorioService;
    }

    public async Task CarregarAsync()
    {
        var lista = await _relatorioService.GetResumoAsync();
        Resumos = new ObservableCollection<ResumoImportacao>(lista);
    }
}
