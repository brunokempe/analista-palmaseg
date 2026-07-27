using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public record EmissaoLinha(
    string Produtor,
    string MesAno,
    int Renovacoes,
    int SegurosNovos,
    int Total,
    decimal Valor);

public partial class RelatorioEmissaoViewModel : ObservableObject
{
    private readonly RelatorioRenovacaoService _renovacaoService;
    private readonly SeguroNovoService _seguroNovoService;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private int _anoSelecionado;
    [ObservableProperty] private string _filtroProdutor = string.Empty;

    private List<EmissaoLinha> _linhasBase = [];

    public ObservableCollection<EmissaoLinha> Linhas { get; } = [];
    public ObservableCollection<int> AnosDisponiveis { get; } = [];

    public RelatorioEmissaoViewModel(
        RelatorioRenovacaoService renovacaoService,
        SeguroNovoService seguroNovoService)
    {
        _renovacaoService = renovacaoService;
        _seguroNovoService = seguroNovoService;
        _anoSelecionado = DateTime.Today.Year;
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            var renovacoes = await _renovacaoService.GetTodosAsync();
            var novos = await _seguroNovoService.GetTodosAsync();

            // anos disponíveis (união das duas fontes)
            var anos = renovacoes
                .Where(r => r.SeguroEmitido && !string.IsNullOrWhiteSpace(r.EmitidoPor))
                .Select(r => r.VigenciaFinal?.Year ?? r.ImportadoEm.Year)
                .Concat(novos.Select(s => s.Vigencia?.Year ?? s.CriadoEm.Year))
                .Distinct().OrderDescending().ToList();

            AnosDisponiveis.Clear();
            foreach (var a in anos) AnosDisponiveis.Add(a);
            if (!AnosDisponiveis.Contains(AnoSelecionado) && AnosDisponiveis.Count > 0)
                AnoSelecionado = AnosDisponiveis[0];

            Calcular(renovacoes, novos);
        }
        finally { IsLoading = false; }
    }

    private void Calcular(List<RelatorioRenovacao> renovacoes, List<SeguroNovo> novos)
    {
        // renovações emitidas no ano selecionado
        var renLinhas = renovacoes
            .Where(r => r.SeguroEmitido && !string.IsNullOrWhiteSpace(r.EmitidoPor))
            .Where(r => (r.VigenciaFinal?.Year ?? r.ImportadoEm.Year) == AnoSelecionado)
            .GroupBy(r => (
                Produtor: r.EmitidoPor!,
                Mes: new DateTime((r.VigenciaFinal ?? r.ImportadoEm).Year,
                                  (r.VigenciaFinal ?? r.ImportadoEm).Month, 1)))
            .Select(g => (g.Key.Produtor, g.Key.Mes,
                Renovacoes: g.Count(),
                Valor: g.Sum(r => r.FechamentoPremioLiquido ?? r.PremioLiquido)));

        // seguros novos no ano selecionado
        var novLinhas = novos
            .Where(s => !string.IsNullOrWhiteSpace(s.CriadoPor))
            .Where(s => (s.Vigencia?.Year ?? s.CriadoEm.Year) == AnoSelecionado)
            .GroupBy(s => (
                Produtor: s.CriadoPor!,
                Mes: new DateTime((s.Vigencia ?? s.CriadoEm).Year,
                                  (s.Vigencia ?? s.CriadoEm).Month, 1)))
            .Select(g => (g.Key.Produtor, g.Key.Mes,
                Novos: g.Count(),
                Valor: g.Sum(s => s.Valor ?? 0)));

        // une as duas fontes por (Produtor, Mês)
        var chaves = renLinhas.Select(r => (r.Produtor, r.Mes))
            .Union(novLinhas.Select(n => (n.Produtor, n.Mes)))
            .Distinct().OrderBy(k => k.Produtor).ThenBy(k => k.Mes);

        _linhasBase = chaves.Select(k =>
        {
            var ren = renLinhas.FirstOrDefault(r => r.Produtor == k.Produtor && r.Mes == k.Mes);
            var nov = novLinhas.FirstOrDefault(n => n.Produtor == k.Produtor && n.Mes == k.Mes);
            return new EmissaoLinha(
                k.Produtor,
                k.Mes.ToString("MMM/yy", new System.Globalization.CultureInfo("pt-BR")),
                ren.Renovacoes,
                nov.Novos,
                ren.Renovacoes + nov.Novos,
                ren.Valor + nov.Valor);
        }).ToList();

        AplicarFiltro();
    }

    partial void OnAnoSelecionadoChanged(int value) => _ = CarregarAsync();

    partial void OnFiltroProdutorChanged(string value) => AplicarFiltro();

    private void AplicarFiltro()
    {
        Linhas.Clear();
        var fonte = string.IsNullOrWhiteSpace(FiltroProdutor)
            ? _linhasBase
            : _linhasBase.Where(l => l.Produtor.Contains(FiltroProdutor, StringComparison.OrdinalIgnoreCase));

        foreach (var l in fonte)
            Linhas.Add(l);
    }

    [RelayCommand]
    private async Task Recarregar() => await CarregarAsync();
}
