using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public record ProdutorEmissaoResumo(
    string Produtor,
    int Total,
    decimal PremioTotal,
    int AssinaturaOk,
    int EmitidoOk)
{
    public int Pendentes => Total - EmitidoOk;
    public string Progresso => $"{EmitidoOk}/{Total}";
}

public partial class EmissaoDashboardViewModel : ObservableObject
{
    private readonly RelatorioRenovacaoService _service;
    private readonly SeguroNovoService _seguroNovoService;
    private readonly SessaoService _sessao;
    private readonly MetaService _metaService;
    private List<RelatorioRenovacao> _todos = [];
    private List<SeguroNovo> _todosSeguroNovos = [];

    [ObservableProperty] private int _atualMes = DateTime.Now.Month;
    [ObservableProperty] private int _atualAno = DateTime.Now.Year;

    public static int[] Meses { get; } = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
    public static int[] Anos  { get; } = Enumerable.Range(DateTime.Now.Year - 3, 7).ToArray();

    [ObservableProperty] private int _totalRenPalma;
    [ObservableProperty] private decimal _premioTotal;
    [ObservableProperty] private decimal _comissaoTotal;
    [ObservableProperty] private int _assinaturasPendentes;
    [ObservableProperty] private int _emissoesPendentes;
    [ObservableProperty] private string _filtroProdutor = string.Empty;
    [ObservableProperty] private bool _isLoading;

    // ── Filtros de Seguros Novos ──────────────────────────────────────────────
    [ObservableProperty] private string _filtroSegNovSegurado = string.Empty;
    [ObservableProperty] private string _filtroSegNovStatus   = "Todos";

    public static string[] StatusSegNovoOpcoes { get; } =
        ["Todos", "Endosso", "Mensal", "Mercado", "Novo", "Prospecção", "Renovação"];

    partial void OnFiltroSegNovSeguradoChanged(string _) => AplicarFiltroSeguroNovos();
    partial void OnFiltroSegNovStatusChanged(string _)   => AplicarFiltroSeguroNovos();

    public ObservableCollection<RelatorioRenovacao> Registros { get; } = [];
    public ObservableCollection<ProdutorEmissaoResumo> ResumoProdutor { get; } = [];
    public ObservableCollection<string> ProdutoresDisponiveis { get; } = [];
    public ObservableCollection<SeguroNovo> SeguroNovos { get; } = [];

    public EmissaoDashboardViewModel(
        RelatorioRenovacaoService service,
        SeguroNovoService seguroNovoService,
        SessaoService sessao,
        MetaService metaService)
    {
        _service = service;
        _seguroNovoService = seguroNovoService;
        _sessao = sessao;
        _metaService = metaService;
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            _todos = await _service.GetRenPalmaAsync();

            var inicio = new DateTime(AtualAno, AtualMes, 1);
            var fim    = inicio.AddMonths(1);
            _todosSeguroNovos = (await _seguroNovoService.GetTodosAsync())
                .Where(s => (s.Vigencia != null && s.Vigencia >= inicio && s.Vigencia < fim)
                         || (s.Vigencia == null  && s.CriadoEm >= inicio && s.CriadoEm < fim))
                .ToList();

            await AtualizarPercentuaisComissaoRenPalmaAsync();

            AtualizarCards();
            AtualizarResumoProdutor();
            AtualizarListaFiltro();
            AplicarFiltro();
            AplicarFiltroSeguroNovos();
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task CarregarPorPeriodo() => await CarregarAsync();

    private IEnumerable<RelatorioRenovacao> TodosPeriodo() =>
        _todos.Where(r =>
            r.VigenciaFinal.HasValue &&
            r.VigenciaFinal.Value.Month == AtualMes &&
            r.VigenciaFinal.Value.Year  == AtualAno);

    private void AtualizarCards()
    {
        var periodo = TodosPeriodo().ToList();
        TotalRenPalma        = periodo.Count + _todosSeguroNovos.Count;
        PremioTotal          = periodo.Sum(r => r.FechamentoPremioLiquido ?? 0)
                             + _todosSeguroNovos.Sum(s => s.Valor ?? 0);
        ComissaoTotal        = periodo.Sum(r => r.ComissaoValor ?? 0)
                             + _todosSeguroNovos.Sum(s => s.ComissaoValor ?? 0);
        AssinaturasPendentes = periodo.Count(r => !r.AssinaturaFeita);
        EmissoesPendentes    = periodo.Count(r => !r.SeguroEmitido);
    }

    private void AtualizarResumoProdutor()
    {
        ResumoProdutor.Clear();
        foreach (var g in TodosPeriodo()
            .GroupBy(r => string.IsNullOrWhiteSpace(r.NovoProdutor) ? "(Sem produtor)" : r.NovoProdutor)
            .OrderBy(g => g.Key))
        {
            ResumoProdutor.Add(new ProdutorEmissaoResumo(
                g.Key,
                g.Count(),
                g.Sum(r => r.FechamentoPremioLiquido ?? 0),
                g.Count(r => r.AssinaturaFeita),
                g.Count(r => r.SeguroEmitido)));
        }
    }

    private void AtualizarListaFiltro()
    {
        var renPalma = TodosPeriodo()
            .Select(r => string.IsNullOrWhiteSpace(r.NovoProdutor) ? "(Sem produtor)" : r.NovoProdutor);

        var segNovos = _todosSeguroNovos
            .Select(s => string.IsNullOrWhiteSpace(s.CriadoPor) ? "(Sem produtor)" : s.CriadoPor);

        ProdutoresDisponiveis.Clear();
        ProdutoresDisponiveis.Add(string.Empty);
        foreach (var p in renPalma.Concat(segNovos).Distinct().OrderBy(p => p))
            ProdutoresDisponiveis.Add(p);
    }

    partial void OnFiltroProdutorChanged(string value) => AplicarFiltro();

    private void AplicarFiltro()
    {
        Registros.Clear();
        var fonte = _todos.Where(r =>
            r.VigenciaFinal.HasValue &&
            r.VigenciaFinal.Value.Month == AtualMes &&
            r.VigenciaFinal.Value.Year  == AtualAno);

        if (!string.IsNullOrWhiteSpace(FiltroProdutor))
            fonte = fonte.Where(r =>
                (string.IsNullOrWhiteSpace(r.NovoProdutor) ? "(Sem produtor)" : r.NovoProdutor) == FiltroProdutor);

        foreach (var r in fonte)
            Registros.Add(r);
    }

    private void AplicarFiltroSeguroNovos()
    {
        SeguroNovos.Clear();
        var query = _todosSeguroNovos.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(FiltroProdutor))
        {
            var prod = FiltroProdutor == "(Sem produtor)" ? string.Empty : FiltroProdutor;
            query = query.Where(s =>
                prod == string.Empty
                    ? string.IsNullOrWhiteSpace(s.CriadoPor)
                    : s.CriadoPor == prod);
        }

        if (!string.IsNullOrWhiteSpace(FiltroSegNovSegurado))
            query = query.Where(s => s.Segurado.Contains(
                FiltroSegNovSegurado, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(FiltroSegNovStatus) && FiltroSegNovStatus != "Todos")
            query = query.Where(s => s.Status == FiltroSegNovStatus);

        foreach (var s in query)
            SeguroNovos.Add(s);
    }

    [RelayCommand]
    private async Task ToggleAssinatura(RelatorioRenovacao? reg)
    {
        if (reg == null) return;
        reg.AssinaturaFeita = !reg.AssinaturaFeita;
        try
        {
            await _service.SalvarStatusAdministrativoAsync(reg);
            AtualizarCards();
            AtualizarResumoProdutor();
        }
        catch (Exception ex)
        {
            reg.AssinaturaFeita = !reg.AssinaturaFeita;
            MessageBox.Show($"Erro ao salvar:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ToggleSeguroEmitido(RelatorioRenovacao? reg)
    {
        if (reg == null) return;
        reg.SeguroEmitido = !reg.SeguroEmitido;
        reg.EmitidoPor    = reg.SeguroEmitido ? _sessao.NomeUsuario : null;
        try
        {
            await _service.SalvarStatusAdministrativoAsync(reg);
            AtualizarCards();
            AtualizarResumoProdutor();
        }
        catch (Exception ex)
        {
            reg.SeguroEmitido = !reg.SeguroEmitido;
            reg.EmitidoPor    = reg.SeguroEmitido ? _sessao.NomeUsuario : null;
            MessageBox.Show($"Erro ao salvar:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task Recarregar() => await CarregarAsync();

    // ── Comissão do colaborador por contrato Ren. Palma ───────────────────────
    // Regra:
    //   Parceira + atingiu meta  → 6%
    //   Parceira + não atingiu   → 4%
    //   Outra   + atingiu meta   → 4%
    //   Outra   + não atingiu    → 3%
    private async Task AtualizarPercentuaisComissaoRenPalmaAsync()
    {
        var seguradoras     = await _metaService.GetSeguradorasAsync(soAtivas: false);
        var metas           = await _metaService.GetMetasAsync(AtualMes, AtualAno);
        var mapaMetasPremio = metas.ToDictionary(m => m.SeguradoraId, m => m.MetaPremio);

        // Cache de resolução de nome → Seguradora (match parcial, igual ao DashboardMetas)
        var nomeToSeg = new Dictionary<string, Seguradora?>(StringComparer.OrdinalIgnoreCase);
        Seguradora? Resolver(string nome)
        {
            if (nomeToSeg.TryGetValue(nome, out var cached)) return cached;
            return nomeToSeg[nome] = seguradoras.FirstOrDefault(s =>
                s.Nome.Contains(nome, StringComparison.OrdinalIgnoreCase) ||
                nome.Contains(s.Nome, StringComparison.OrdinalIgnoreCase));
        }

        var periodo = TodosPeriodo().ToList();

        // Total de prêmio por (colaborador, seguradoraId) para checar se atingiu meta
        var premiosPorColabSeg = new Dictionary<(string, int), decimal>();
        foreach (var r in periodo)
        {
            var seg = Resolver((r.FechamentoSeguradora ?? r.Seguradora ?? "").Trim());
            if (seg == null) continue;
            var k = (r.NovoProdutor ?? "", seg.Id);
            premiosPorColabSeg[k] = premiosPorColabSeg.GetValueOrDefault(k) + (r.FechamentoPremioLiquido ?? 0);
        }

        foreach (var r in periodo)
        {
            var seg = Resolver((r.FechamentoSeguradora ?? r.Seguradora ?? "").Trim());

            bool isParceira  = seg?.IsParceira ?? false;
            bool atingiuMeta = false;

            if (seg != null && mapaMetasPremio.TryGetValue(seg.Id, out var meta) && meta > 0)
            {
                var realizado = premiosPorColabSeg.GetValueOrDefault((r.NovoProdutor ?? "", seg.Id));
                atingiuMeta   = realizado >= meta;
            }

            r.PercentualComissaoColab = (isParceira, atingiuMeta) switch
            {
                (true,  true)  => 6m,
                (true,  false) => 4m,
                (false, true)  => 4m,
                _              => 3m
            };
        }
    }
}
