using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AnalistaPalmaseg.Core.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class RetencaoViewModel : ObservableObject
{
    private readonly RelatorioService _relatorioService;

    [ObservableProperty] private ObservableCollection<string> _produtores = [];
    [ObservableProperty] private string? _produtorSelecionado;
    [ObservableProperty] private ISeries[] _series = [];
    [ObservableProperty] private Axis[] _axesX = [];

    public RetencaoViewModel(RelatorioService relatorioService)
    {
        _relatorioService = relatorioService;
    }

    public async Task CarregarAsync()
    {
        var resumos = await _relatorioService.GetResumoAsync();
        var produtores = resumos.Select(r => r.Importacao.Produtor).Distinct().ToList();
        Produtores = new ObservableCollection<string>(produtores);
        ProdutorSelecionado = produtores.FirstOrDefault();
    }

    partial void OnProdutorSelecionadoChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
            _ = AtualizarGraficoAsync(value);
    }

    private async Task AtualizarGraficoAsync(string produtor)
    {
        var evolucao = await _relatorioService.GetEvolucaoRetencaoAsync(produtor);

        AxesX =
        [
            new Axis
            {
                Labels = evolucao.Select(e => e.Periodo).ToArray(),
                TextSize = 11
            }
        ];

        Series =
        [
            new LineSeries<decimal>
            {
                Name = $"Retenção — {produtor}",
                Values = evolucao.Select(e => e.Retencao).ToArray(),
                Stroke = new SolidColorPaint(SKColor.Parse("#378add"), 2),
                GeometryFill = new SolidColorPaint(SKColor.Parse("#378add")),
                GeometrySize = 8,
                Fill = null
            }
        ];
    }
}
