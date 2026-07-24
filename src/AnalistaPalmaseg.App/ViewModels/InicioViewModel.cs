using CommunityToolkit.Mvvm.ComponentModel;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class InicioViewModel : ObservableObject
{
    private readonly RelatorioRenovacaoService _renovacaoService;
    private readonly SessaoService _sessao;

    // Acompanhamento de Renovações
    [ObservableProperty] private int _acompTotal;
    [ObservableProperty] private int _acompARenovar;
    [ObservableProperty] private int _acompRenPalma;
    [ObservableProperty] private int _acompEmitido;
    [ObservableProperty] private int _acompOutros;
    [ObservableProperty] private bool _temDadosRenovacoes;

    // Dashboard de Emissão
    [ObservableProperty] private int _emissaoTotal;
    [ObservableProperty] private decimal _emissaoPremioTotal;
    [ObservableProperty] private int _emissaoAssinaturasPendentes;
    [ObservableProperty] private int _emissaoEmissoesPendentes;

    public string NomeUsuario => _sessao.NomeUsuario;

    public InicioViewModel(
        RelatorioRenovacaoService renovacaoService,
        SessaoService sessao)
    {
        _renovacaoService = renovacaoService;
        _sessao = sessao;
    }

    public async Task CarregarAsync()
    {
        // Acompanhamento de Renovações
        var contadores = await _renovacaoService.GetContadorSituacoesAsync();
        AcompTotal = contadores.Values.Sum();
        TemDadosRenovacoes = AcompTotal > 0;
        AcompARenovar  = contadores.GetValueOrDefault("À Renovar");
        AcompRenPalma  = contadores.GetValueOrDefault("Ren. Palma");
        AcompEmitido   = contadores.GetValueOrDefault("Emitido")
                       + contadores.GetValueOrDefault("Ren. Outro");
        AcompOutros    = AcompTotal - AcompARenovar - AcompRenPalma - AcompEmitido;

        // Dashboard de Emissão
        var (total, assinaturaOk, emitidoOk, premioTotal) = await _renovacaoService.GetRenPalmaStatsAsync();
        EmissaoTotal                = total;
        EmissaoPremioTotal          = premioTotal;
        EmissaoAssinaturasPendentes = total - assinaturaOk;
        EmissaoEmissoesPendentes    = total - emitidoOk;
    }
}
