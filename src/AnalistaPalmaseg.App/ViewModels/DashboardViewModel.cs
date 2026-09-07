using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AnalistaPalmaseg.Core.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly RelatorioService _relatorioService;
    private readonly SessaoService _sessao;
    private List<ResumoImportacao> _todosResumos = [];

    [ObservableProperty] private ObservableCollection<string> _nomesDisponiveis = [];
    [ObservableProperty] private string? _nomeSelecionado;
    [ObservableProperty] private ObservableCollection<ResumoImportacao> _periodos = [];
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPl))]
    [NotifyPropertyChangedFor(nameof(TotalComissao))]
    private ResumoImportacao? _resumoAtual;

    public decimal TotalPl => (ResumoAtual?.PlRenovado ?? 0) + (ResumoAtual?.NovosPl ?? 0);
    public decimal TotalComissao => (ResumoAtual?.ComissaoRenovacoes ?? 0) + (ResumoAtual?.Participacao ?? 0);
    [ObservableProperty] private ObservableCollection<ParticipacaoSeguradora> _participacao = [];
    [ObservableProperty] private ISeries[] _seriesStatus = [];
    [ObservableProperty] private ISeries[] _seriesParticipacao = [];
    [ObservableProperty] private Axis[] _axesParticipacaoX = [];

    public DashboardViewModel(RelatorioService relatorioService, SessaoService sessao)
    {
        _relatorioService = relatorioService;
        _sessao = sessao;
    }

    public async Task CarregarAsync()
    {
        _todosResumos = await _relatorioService.GetResumoAsync();

        var nomes = _todosResumos
            .Select(r => r.Importacao.Produtor)
            .Distinct()
            .OrderBy(n => n)
            .ToList();

        NomesDisponiveis = new ObservableCollection<string>(nomes);

        if (NomeSelecionado != null && NomesDisponiveis.Contains(NomeSelecionado))
            await AtualizarPeriodosAsync(NomeSelecionado);
        else
            NomeSelecionado = NomesDisponiveis.Contains(_sessao.NomeUsuario)
                ? _sessao.NomeUsuario
                : NomesDisponiveis.FirstOrDefault();
    }

    partial void OnNomeSelecionadoChanged(string? value)
    {
        if (!string.IsNullOrEmpty(value))
            _ = AtualizarPeriodosAsync(value);
        else
        {
            Periodos = [];
            ResumoAtual = null;
        }
    }

    private async Task AtualizarPeriodosAsync(string produtor)
    {
        var filtrados = _todosResumos
            .Where(r => r.Importacao.Produtor == produtor)
            .ToList();

        Periodos = new ObservableCollection<ResumoImportacao>(filtrados);
        ResumoAtual = Periodos.FirstOrDefault();

        if (ResumoAtual != null)
            await AtualizarGraficosAsync(ResumoAtual);
    }

    private async Task AtualizarGraficosAsync(ResumoImportacao resumo)
    {
        SeriesStatus =
        [
            new PieSeries<int> { Name = "Ren. Palma",   Values = [resumo.RenovadasPalma], Fill = new SolidColorPaint(SKColor.Parse("#22c55e")) },
            new PieSeries<int> { Name = "Pendentes",    Values = [resumo.Pendentes],       Fill = new SolidColorPaint(SKColor.Parse("#f59e0b")) },
            new PieSeries<int> { Name = "Não renovado", Values = [resumo.NaoRenovado],     Fill = new SolidColorPaint(SKColor.Parse("#ef4444")) }
        ];

        var part = await _relatorioService.GetParticipacaoAsync(resumo.Importacao.Id);
        Participacao = new ObservableCollection<ParticipacaoSeguradora>(part);

        AxesParticipacaoX =
        [
            new Axis { Labels = part.Select(p => p.Cia).ToArray(), LabelsRotation = -30, TextSize = 11 }
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
