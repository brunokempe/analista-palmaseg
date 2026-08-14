using System.Globalization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public class ProdutorResumo
{
    public string Produtor         { get; set; } = string.Empty;
    public int    Qtd              { get; set; }
    public decimal PremioLiquido   { get; set; }
    public decimal Comissao        { get; set; }
    public string RamosSumarizados { get; set; } = string.Empty;
}

public class DiaVencimentoResumo
{
    public DateTime Data          { get; set; }
    public string   DiaSemana     { get; set; } = string.Empty;
    public int      Qtd           { get; set; }
    public decimal  PremioLiquido { get; set; }
    public decimal  Comissao      { get; set; }
}

public class RamoTotalResumo
{
    public string  Ramo          { get; set; } = string.Empty;
    public int     Qtd           { get; set; }
    public decimal PremioLiquido { get; set; }
    public decimal Comissao      { get; set; }
}

public partial class DistribuicaoProdutorViewModel : ObservableObject
{
    private readonly RelatorioRenovacaoService      _renovacaoService;
    private readonly DistribuicaoReferenciaService  _referenciaService;
    private List<RelatorioRenovacao> _cache = [];
    private bool _computing;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FiltroMesNome))]
    private int _filtroMes = DateTime.Now.Month;

    [ObservableProperty] private int    _filtroAno  = DateTime.Now.Year;
    [ObservableProperty] private string _filtroProdutorSelecionado = "(Todos)";
    [ObservableProperty] private bool   _isLoading;

    // Totais corretora
    [ObservableProperty] private decimal _totalPremioLiquido;
    [ObservableProperty] private decimal _totalComissao;
    [ObservableProperty] private int     _totalApolices;

    // Referência ano anterior (editável)
    [ObservableProperty] private decimal _refPremioLiquido;
    [ObservableProperty] private decimal _refComissao;
    [ObservableProperty] private int     _refQtdApolices;
    private int _refId;

    // Collections
    [ObservableProperty] private List<ProdutorResumo>       _porProdutor = [];
    [ObservableProperty] private List<DiaVencimentoResumo>  _porDia      = [];
    [ObservableProperty] private List<RamoTotalResumo>      _porRamo     = [];

    public List<string> ProdutoresParaFiltro { get; private set; } = ["(Todos)"];

    public static string[] NomesMeses { get; } =
    [
        "Todos", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
        "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"
    ];

    public string FiltroMesNome => NomesMeses[Math.Clamp(FiltroMes, 0, 12)];

    public List<int> AnosDisponiveis { get; } =
        Enumerable.Range(DateTime.Now.Year - 3, 6).ToList();

    public string LabelAnoAnterior =>
        $"Referência — {(FiltroAno > 0 ? FiltroAno - 1 : DateTime.Now.Year - 1)}";

    public DistribuicaoProdutorViewModel(
        RelatorioRenovacaoService     renovacaoService,
        DistribuicaoReferenciaService referenciaService)
    {
        _renovacaoService  = renovacaoService;
        _referenciaService = referenciaService;
    }

    partial void OnFiltroMesChanged(int value) => Computar();
    partial void OnFiltroAnoChanged(int value)
    {
        OnPropertyChanged(nameof(LabelAnoAnterior));
        _ = CarregarReferenciaAsync();
        Computar();
    }
    partial void OnFiltroProdutorSelecionadoChanged(string value) => Computar();

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            _cache = await _renovacaoService.GetTodosAsync();
            await CarregarReferenciaAsync();
            Computar();
        }
        finally { IsLoading = false; }
    }

    private async Task CarregarReferenciaAsync()
    {
        var refAno   = FiltroAno > 0 ? FiltroAno - 1 : DateTime.Now.Year - 1;
        var ref_     = await _referenciaService.GetByAnoAsync(refAno);
        _refId          = ref_?.Id ?? 0;
        RefPremioLiquido = ref_?.PremioLiquidoRef ?? 0m;
        RefComissao      = ref_?.ComissaoRef ?? 0m;
        RefQtdApolices   = ref_?.QtdApolicesRef ?? 0;
    }

    private void Computar()
    {
        if (_computing) return;
        _computing = true;
        try
        {
            // Apenas registros com NovoProdutor definido, filtrados por ano/mês
            var base_ = _cache.Where(r =>
            {
                if (!r.VigenciaFinal.HasValue) return false;
                if (string.IsNullOrWhiteSpace(r.NovoProdutor)) return false;
                if (FiltroAno > 0 && r.VigenciaFinal.Value.Year  != FiltroAno) return false;
                if (FiltroMes > 0 && r.VigenciaFinal.Value.Month != FiltroMes) return false;
                return true;
            }).ToList();

            // Atualiza lista de produtores disponíveis
            var produtores = base_
                .Select(r => r.NovoProdutor!)
                .Distinct()
                .OrderBy(p => p)
                .ToList();
            ProdutoresParaFiltro = ["(Todos)", ..produtores];
            OnPropertyChanged(nameof(ProdutoresParaFiltro));

            // Garante que a seleção atual ainda é válida
            if (!ProdutoresParaFiltro.Contains(FiltroProdutorSelecionado))
                FiltroProdutorSelecionado = "(Todos)";

            // Aplica filtro por produtor
            var filtrados = FiltroProdutorSelecionado == "(Todos)"
                ? base_
                : base_.Where(r => r.NovoProdutor == FiltroProdutorSelecionado).ToList();

            TotalPremioLiquido = filtrados.Sum(r => r.PremioLiquido);
            TotalComissao      = filtrados.Sum(r => r.ComissaoGerada);
            TotalApolices      = filtrados.Count;

            PorProdutor = filtrados
                .GroupBy(r => r.NovoProdutor!)
                .Select(g =>
                {
                    var ramos = g.Where(r => !string.IsNullOrWhiteSpace(r.Ramo))
                                 .GroupBy(r => r.Ramo!)
                                 .OrderByDescending(gr => gr.Count())
                                 .Select(gr => $"{gr.Key} ({gr.Count()})")
                                 .ToList();
                    return new ProdutorResumo
                    {
                        Produtor         = g.Key,
                        Qtd              = g.Count(),
                        PremioLiquido    = g.Sum(r => r.PremioLiquido),
                        Comissao         = g.Sum(r => r.ComissaoGerada),
                        RamosSumarizados = string.Join("  |  ", ramos)
                    };
                })
                .OrderByDescending(p => p.PremioLiquido)
                .ToList();

            var cul = new CultureInfo("pt-BR");
            PorDia = filtrados
                .Where(r => r.VigenciaFinal.HasValue)
                .GroupBy(r => r.VigenciaFinal!.Value.Date)
                .Select(g =>
                {
                    var ds = g.Key.ToString("dddd", cul);
                    return new DiaVencimentoResumo
                    {
                        Data          = g.Key,
                        DiaSemana     = ds.Length > 0 ? char.ToUpper(ds[0]) + ds[1..] : ds,
                        Qtd           = g.Count(),
                        PremioLiquido = g.Sum(r => r.PremioLiquido),
                        Comissao      = g.Sum(r => r.ComissaoGerada)
                    };
                })
                .OrderBy(d => d.Data)
                .ToList();

            PorRamo = filtrados
                .Where(r => !string.IsNullOrWhiteSpace(r.Ramo))
                .GroupBy(r => r.Ramo!)
                .Select(g => new RamoTotalResumo
                {
                    Ramo          = g.Key,
                    Qtd           = g.Count(),
                    PremioLiquido = g.Sum(r => r.PremioLiquido),
                    Comissao      = g.Sum(r => r.ComissaoGerada)
                })
                .OrderByDescending(r => r.Qtd)
                .ToList();
        }
        finally { _computing = false; }
    }

    [RelayCommand]
    private void SelecionarMes(string nomeMes)
    {
        var idx = Array.IndexOf(NomesMeses, nomeMes);
        if (idx >= 0) FiltroMes = idx;
    }

    [RelayCommand]
    private async Task SalvarReferenciaAsync()
    {
        try
        {
            var refAno = FiltroAno > 0 ? FiltroAno - 1 : DateTime.Now.Year - 1;
            await _referenciaService.SalvarAsync(new DistribuicaoReferencia
            {
                Id               = _refId,
                Ano              = refAno,
                PremioLiquidoRef = RefPremioLiquido,
                ComissaoRef      = RefComissao,
                QtdApolicesRef   = RefQtdApolices
            });
            MessageBox.Show("Referência salva com sucesso.", "Sucesso",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao salvar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
