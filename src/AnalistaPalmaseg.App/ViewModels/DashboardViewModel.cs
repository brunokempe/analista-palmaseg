using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly RelatorioService _relatorioService;

    [ObservableProperty] private ObservableCollection<ResumoImportacao> _resumos = [];
    [ObservableProperty] private ResumoImportacao? _resumoSelecionado;
    [ObservableProperty] private ObservableCollection<ParticipacaoSeguradora> _participacao = [];
    [ObservableProperty] private ISeries[] _seriesStatus = [];
    [ObservableProperty] private ISeries[] _seriesParticipacao = [];
    [ObservableProperty] private Axis[] _axesParticipacaoX = [];

    public DashboardViewModel(RelatorioService relatorioService)
    {
        _relatorioService = relatorioService;
    }

    public async Task CarregarAsync()
    {
        var lista = await _relatorioService.GetResumoAsync();
        Resumos = new ObservableCollection<ResumoImportacao>(lista);
        ResumoSelecionado = Resumos.FirstOrDefault();
        if (ResumoSelecionado != null)
            await AtualizarGraficosAsync(ResumoSelecionado);
    }

    partial void OnResumoSelecionadoChanged(ResumoImportacao? value)
    {
        if (value != null)
            _ = AtualizarGraficosAsync(value);
    }

    private async Task AtualizarGraficosAsync(ResumoImportacao resumo)
    {
        SeriesStatus =
        [
            new PieSeries<int>
            {
                Name = "Ren. Palma",
                Values = [resumo.RenovadasPalma],
                Fill = new SolidColorPaint(SKColor.Parse("#22c55e"))
            },
            new PieSeries<int>
            {
                Name = "Pendentes",
                Values = [resumo.Pendentes],
                Fill = new SolidColorPaint(SKColor.Parse("#f59e0b"))
            },
            new PieSeries<int>
            {
                Name = "Não renovado",
                Values = [resumo.NaoRenovado],
                Fill = new SolidColorPaint(SKColor.Parse("#ef4444"))
            }
        ];

        var part = await _relatorioService.GetParticipacaoAsync(resumo.Importacao.Id);
        Participacao = new ObservableCollection<ParticipacaoSeguradora>(part);

        AxesParticipacaoX =
        [
            new Axis
            {
                Labels = part.Select(p => p.Cia).ToArray(),
                LabelsRotation = -30,
                TextSize = 11
            }
        ];

        SeriesParticipacao =
        [
            new ColumnSeries<decimal>
            {
                Name = "PL Renovado",
                Values = part.Select(p => p.PlRenovado).ToArray(),
                Fill = new SolidColorPaint(SKColor.Parse("#378add"))
            }
        ];
    }
}
