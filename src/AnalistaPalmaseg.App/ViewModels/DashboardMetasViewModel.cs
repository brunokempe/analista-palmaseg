using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public class SeguradoraAlcanceVm
{
    public string Nome { get; init; } = string.Empty;
    public bool IsParceira { get; init; }
    public decimal Meta { get; init; }
    public decimal Realizado { get; init; }
    public decimal Participacao { get; init; }
    public bool Atingiu => Meta > 0 && Realizado >= Meta;
    public decimal Saldo => Realizado - Meta;
    public bool SaldoPositivo => Saldo >= 0;
    public double PercentualAlcance => Meta > 0 ? (double)(Realizado / Meta) : 0d;
    public string PercentualFormatado => Meta > 0 ? $"{Realizado / Meta:P1}" : "—";
}

public record PremiacaoItemVm(string Label, decimal Bonus);

public partial class DashboardMetasViewModel : ObservableObject
{
    private readonly MetaService _metaService;
    private readonly SessaoService _sessao;
    private bool _suppressAutoReload;

    public bool IsAdmin    => _sessao.IsAdmin;
    public bool IsNotAdmin => !_sessao.IsAdmin;

    [ObservableProperty] private bool _isLoading;

    // ── Período selecionado ───────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RefAnoDisplay))]
    private int _atualMes = DateTime.Now.Month;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RefAnoDisplay))]
    private int _atualAno = DateTime.Now.Year;

    public int RefAnoDisplay => AtualAno - 1;

    // ── Valores de referência (ano anterior) ──────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PremioMeta10), nameof(PremioMeta15),
                              nameof(PremioAtinge10), nameof(PremioAtinge15),
                              nameof(CrescimentoPremio), nameof(BonusCrescimentoPremio), nameof(BonusTotal))]
    private decimal _refPremio;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ComissaoMeta15), nameof(ComissaoMeta20),
                              nameof(ComissaoAtinge15), nameof(ComissaoAtinge20),
                              nameof(CrescimentoComissao), nameof(BonusCrescimentoComissao), nameof(BonusTotal))]
    private decimal _refComissao;

    // "Para +X%": quanto FALTA para atingir a meta de crescimento sobre o ano anterior
    public decimal PremioMeta10   => RefPremio   * 1.10m - AtualPremio;
    public decimal PremioMeta15   => RefPremio   * 1.15m - AtualPremio;
    public decimal ComissaoMeta15 => RefComissao * 1.15m - AtualComissaoCorretora;
    public decimal ComissaoMeta20 => RefComissao * 1.20m - AtualComissaoCorretora;

    // ── Posição atual ─────────────────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PremioMeta10), nameof(PremioMeta15),
                              nameof(PremioAtinge10), nameof(PremioAtinge15),
                              nameof(CrescimentoPremio), nameof(BonusCrescimentoPremio), nameof(BonusTotal))]
    private decimal _atualPremio;

    [ObservableProperty] private decimal _atualPremioRenPalma;
    [ObservableProperty] private decimal _atualPremioSegNovos;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ComissaoMeta15), nameof(ComissaoMeta20),
                              nameof(ComissaoAtinge15), nameof(ComissaoAtinge20),
                              nameof(CrescimentoComissao), nameof(BonusCrescimentoComissao), nameof(BonusTotal))]
    private decimal _atualComissaoCorretora;

    // ── Colaboradores ─────────────────────────────────────────────────────────
    public ObservableCollection<string> Colaboradores { get; } = [];

    [ObservableProperty] private string _colaboradorSelecionado = "Todos";

    partial void OnColaboradorSelecionadoChanged(string value)
    {
        if (!_suppressAutoReload)
            _ = CarregarMetricasAsync();
    }

    // ── Crescimento calculado ─────────────────────────────────────────────────
    public decimal CrescimentoPremio   => RefPremio   > 0 ? (AtualPremio      - RefPremio)   / RefPremio   : 0m;
    public decimal CrescimentoComissao => RefComissao > 0 ? (AtualComissaoCorretora - RefComissao) / RefComissao : 0m;

    public bool PremioAtinge10   => RefPremio   > 0 && AtualPremio            >= RefPremio   * 1.10m;
    public bool PremioAtinge15   => RefPremio   > 0 && AtualPremio            >= RefPremio   * 1.15m;
    public bool ComissaoAtinge15 => RefComissao > 0 && AtualComissaoCorretora >= RefComissao * 1.15m;
    public bool ComissaoAtinge20 => RefComissao > 0 && AtualComissaoCorretora >= RefComissao * 1.20m;

    // ── Comissão do colaborador ───────────────────────────────────────────────
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ComissaoColabTotal), nameof(TotalGeralColab))]
    private decimal _comissaoColabRenPalma;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ComissaoColabTotal), nameof(TotalGeralColab))]
    private decimal _comissaoColabSegNovos;

    public decimal ComissaoColabTotal => ComissaoColabRenPalma + ComissaoColabSegNovos;
    public decimal TotalGeralColab    => ComissaoColabTotal + BonusTotal;

    // ── Seguradoras e bônus ───────────────────────────────────────────────────
    public ObservableCollection<SeguradoraAlcanceVm> SeguradorasAlcance { get; } = [];
    public ObservableCollection<PremiacaoItemVm>     PremiacaoItens     { get; } = [];

    [ObservableProperty] private decimal _totalMeta;
    [ObservableProperty] private decimal _totalRealizado;
    [ObservableProperty] private decimal _totalSaldo;
    [ObservableProperty] private decimal _totalParticipacao;
    public bool TotalSaldoPositivo => TotalSaldo >= 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(BonusPremiacao), nameof(BonusTotal), nameof(TotalGeralColab))]
    private int _qtdSeguradorasAtingidas;

    private List<MetaPremiacao>   _premiacoes   = [];
    private List<MetaCrescimento> _crescimentos = [];

    public decimal BonusPremiacao
    {
        get
        {
            // "Todas" considera apenas parceiras com meta definida (Meta > 0)
            var totalParceiraComMeta = SeguradorasAlcance.Count(s => s.IsParceira && s.Meta > 0);
            decimal bonus = 0m;
            foreach (var p in _premiacoes.OrderBy(x => x.Ordem))
            {
                if (p.EhTodas && totalParceiraComMeta > 0 && QtdSeguradorasAtingidas >= totalParceiraComMeta)
                    bonus += p.ValorBonus;
                else if (!p.EhTodas && p.QuantidadeMinima.HasValue && QtdSeguradorasAtingidas >= p.QuantidadeMinima.Value)
                    bonus += p.ValorBonus;
            }
            return bonus;
        }
    }

    public decimal BonusCrescimentoPremio
    {
        get
        {
            var b = _crescimentos
                .Where(c => c.Tipo == "Premio" && PremioAtinge10)
                .OrderByDescending(c => c.PercentualMeta)
                .FirstOrDefault(c => CrescimentoPremio >= c.PercentualMeta);
            return b?.ValorBonus ?? 0m;
        }
    }

    public decimal BonusCrescimentoComissao
    {
        get
        {
            var b = _crescimentos
                .Where(c => c.Tipo == "Comissao" && ComissaoAtinge15)
                .OrderByDescending(c => c.PercentualMeta)
                .FirstOrDefault(c => CrescimentoComissao >= c.PercentualMeta);
            return b?.ValorBonus ?? 0m;
        }
    }

    public decimal BonusTotal => BonusPremiacao + BonusCrescimentoPremio + BonusCrescimentoComissao;

    public static int[] Meses { get; } = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
    public static int[] Anos  { get; } = Enumerable.Range(DateTime.Now.Year - 3, 7).ToArray();

    public DashboardMetasViewModel(MetaService metaService, SessaoService sessao)
    {
        _metaService = metaService;
        _sessao = sessao;

        WeakReferenceMessenger.Default.Register<DashboardRefreshMessage>(this, (_, m) =>
        {
            if (m.Mes == AtualMes && m.Ano == AtualAno)
                _ = CarregarMetricasAsync();
        });
    }

    [RelayCommand]
    public async Task CarregarAsync()
    {
        IsLoading = true;
        _suppressAutoReload = true;
        try
        {
            _premiacoes   = await _metaService.GetPremiacaoAsync();
            _crescimentos = await _metaService.GetCrescimentoAsync();

            if (_sessao.IsAdmin)
            {
                var cols     = await _metaService.GetColaboradoresAsync(AtualMes, AtualAno);
                var previous = ColaboradorSelecionado;
                Colaboradores.Clear();
                foreach (var c in cols) Colaboradores.Add(c);
                ColaboradorSelecionado = Colaboradores.Contains(previous)
                    ? previous
                    : Colaboradores.FirstOrDefault() ?? string.Empty;
            }
            else
            {
                ColaboradorSelecionado = _sessao.NomeUsuario;
            }
        }
        finally
        {
            _suppressAutoReload = false;
            await CarregarMetricasAsync();
            IsLoading = false;
        }
    }

    private async Task CarregarMetricasAsync()
    {
        IsLoading = true;
        try
        {
            var colab    = string.IsNullOrEmpty(ColaboradorSelecionado) ? null : ColaboradorSelecionado;
            var refColab = _sessao.IsAdmin ? (colab ?? "") : _sessao.NomeUsuario;

            var ref_ = await _metaService.GetValorReferenciaAsync(AtualMes, AtualAno - 1, refColab);
            RefPremio   = ref_?.PremioTotal   ?? 0m;
            RefComissao = ref_?.ComissaoTotal ?? 0m;

            var (premioRen, premioNovos, comissaoCorretora) = await _metaService.GetPosicaoDetalhadaAsync(AtualMes, AtualAno, colab);
            AtualPremioRenPalma    = premioRen;
            AtualPremioSegNovos    = premioNovos;
            AtualPremio            = premioRen + premioNovos;
            AtualComissaoCorretora = comissaoCorretora;

            var (colabRen, colabNovos) = await _metaService.GetComissaoColaboradorAsync(AtualMes, AtualAno, colab);
            ComissaoColabRenPalma = colabRen;
            ComissaoColabSegNovos = colabNovos;
            OnPropertyChanged(nameof(ComissaoColabTotal));
            OnPropertyChanged(nameof(TotalGeralColab));

            await CarregarSeguradorasAlcanceAsync(colab);
        }
        finally { IsLoading = false; }
    }

    private async Task CarregarSeguradorasAlcanceAsync(string? colaborador)
    {
        SeguradorasAlcance.Clear();

        var seguradoras        = await _metaService.GetSeguradorasAsync(soAtivas: true);
        var metas              = await _metaService.GetMetasAsync(AtualMes, AtualAno);
        var realizadosPorSeg   = await _metaService.GetPremiosPorSeguradoraPorColaboradorAsync(AtualMes, AtualAno, colaborador);
        var participacaoPorSeg = await _metaService.GetParticipacaoPorSeguradoraAsync(AtualMes, AtualAno, colaborador);
        var mapaMetasPremio    = metas.ToDictionary(m => m.SeguradoraId, m => m.MetaPremio);

        var parceiras = seguradoras.Where(s => s.IsParceira).ToList();
        var naoParce  = seguradoras.Where(s => !s.IsParceira).ToList();

        var chavesClaimed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var seg in parceiras)
        {
            var metaPremio  = mapaMetasPremio.TryGetValue(seg.Id, out var mp) ? mp : 0m;
            var matchedKeys = realizadosPorSeg.Keys
                .Where(k => k.Contains(seg.Nome, StringComparison.OrdinalIgnoreCase)
                         || seg.Nome.Contains(k, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var k in matchedKeys) chavesClaimed.Add(k);

            var partKeys = participacaoPorSeg.Keys
                .Where(k => k.Contains(seg.Nome, StringComparison.OrdinalIgnoreCase)
                         || seg.Nome.Contains(k, StringComparison.OrdinalIgnoreCase));

            SeguradorasAlcance.Add(new SeguradoraAlcanceVm
            {
                Nome         = seg.Nome,
                IsParceira   = true,
                Meta         = metaPremio,
                Realizado    = matchedKeys.Sum(k => realizadosPorSeg[k]),
                Participacao = partKeys.Sum(k => participacaoPorSeg[k])
            });
        }

        // Demais: catch-all das chaves não absorvidas por parceiras
        foreach (var seg in naoParce)
        {
            var metaPremio = mapaMetasPremio.TryGetValue(seg.Id, out var mp) ? mp : 0m;

            SeguradorasAlcance.Add(new SeguradoraAlcanceVm
            {
                Nome         = seg.Nome,
                IsParceira   = false,
                Meta         = metaPremio,
                Realizado    = realizadosPorSeg.Where(kv => !chavesClaimed.Contains(kv.Key)).Sum(kv => kv.Value),
                Participacao = participacaoPorSeg.Where(kv => !chavesClaimed.Contains(kv.Key)).Sum(kv => kv.Value)
            });
        }

        QtdSeguradorasAtingidas = SeguradorasAlcance.Count(s => s.IsParceira && s.Atingiu);

        TotalMeta         = SeguradorasAlcance.Sum(s => s.Meta);
        TotalRealizado    = SeguradorasAlcance.Sum(s => s.Realizado);
        TotalSaldo        = TotalRealizado - TotalMeta;
        TotalParticipacao = SeguradorasAlcance.Sum(s => s.Participacao);
        OnPropertyChanged(nameof(TotalSaldoPositivo));

        // Reconstrói itens detalhados das regras que dispararam
        PremiacaoItens.Clear();
        var totalComMeta = SeguradorasAlcance.Count(s => s.IsParceira && s.Meta > 0);
        foreach (var p in _premiacoes.OrderBy(x => x.Ordem))
        {
            bool atingiu;
            string label;
            if (p.EhTodas)
            {
                atingiu = totalComMeta > 0 && QtdSeguradorasAtingidas >= totalComMeta;
                label   = "Atingir todas as seguradoras";
            }
            else if (p.QuantidadeMinima.HasValue)
            {
                atingiu = QtdSeguradorasAtingidas >= p.QuantidadeMinima.Value;
                label   = $"Atingir {p.QuantidadeMinima} seguradora{(p.QuantidadeMinima > 1 ? "s" : "")}";
            }
            else continue;

            PremiacaoItens.Add(new PremiacaoItemVm(label, atingiu ? p.ValorBonus : 0m));
        }

        OnPropertyChanged(nameof(BonusPremiacao));
        OnPropertyChanged(nameof(BonusCrescimentoPremio));
        OnPropertyChanged(nameof(BonusCrescimentoComissao));
        OnPropertyChanged(nameof(BonusTotal));
        OnPropertyChanged(nameof(TotalGeralColab));
    }

    [RelayCommand]
    private async Task SalvarRefAsync()
    {
        IsLoading = true;
        try
        {
            var refColab = _sessao.IsAdmin
                ? (string.IsNullOrEmpty(ColaboradorSelecionado) ? "" : ColaboradorSelecionado)
                : _sessao.NomeUsuario;
            await _metaService.SalvarValorReferenciaAsync(new ValorReferencia
            {
                Colaborador   = refColab,
                Mes           = AtualMes,
                Ano           = AtualAno - 1,
                PremioTotal   = RefPremio,
                ComissaoTotal = RefComissao
            });
            MessageBox.Show("Valores de referência salvos!", "Sucesso",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao salvar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsLoading = false; }
    }
}
