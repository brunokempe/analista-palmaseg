using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class ApolicesDashboardViewModel : ObservableObject
{
    private readonly ApoliceService _apoliceService;

    [ObservableProperty] private int _totalApolices;
    [ObservableProperty] private int _vencidas;
    [ObservableProperty] private int _proximas;
    [ObservableProperty] private int _emDia;
    [ObservableProperty] private string _infoImportacao = "Nenhuma importação realizada";
    [ObservableProperty] private bool _semDados = true;

    [ObservableProperty] private ObservableCollection<Apolice> _apolicesVencidas = [];
    [ObservableProperty] private ObservableCollection<Apolice> _apolicesProximas = [];
    [ObservableProperty] private ObservableCollection<Apolice> _todasApolices = [];

    public ApolicesDashboardViewModel(ApoliceService apoliceService)
    {
        _apoliceService = apoliceService;
    }

    public async Task CarregarAsync()
    {
        var ultima = await _apoliceService.GetUltimaImportacaoAsync();
        if (ultima != null)
            InfoImportacao = $"Importado em {ultima.ImportadoEm:dd/MM/yyyy HH:mm} — {ultima.ArquivoOrigem}";

        var todas = await _apoliceService.GetTodasAsync();
        SemDados = todas.Count == 0;

        TotalApolices = todas.Count;
        Vencidas  = todas.Count(a => a.StatusLabel == "Vencida");
        Proximas  = todas.Count(a => a.StatusLabel == "Próxima");
        EmDia     = todas.Count(a => a.StatusLabel == "Em dia");

        // Sorted: vencidas mais antigas primeiro, depois próximas, depois em dia
        var ordenadas = todas.OrderBy(a => a.DiasParaVencimento).ToList();

        ApolicesVencidas = new ObservableCollection<Apolice>(ordenadas.Where(a => a.StatusLabel == "Vencida"));
        ApolicesProximas = new ObservableCollection<Apolice>(ordenadas.Where(a => a.StatusLabel == "Próxima"));
        TodasApolices    = new ObservableCollection<Apolice>(ordenadas);
    }
}
