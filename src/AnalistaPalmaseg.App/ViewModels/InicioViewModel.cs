using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public record ItemAlertaInicio(string Titulo, string Detalhe, string Prazo, Brush Cor, string Destino);
public record BarraSituacaoInicio(string Nome, int Quantidade, double Percentual, Brush Cor);

public partial class InicioViewModel : ObservableObject
{
    private const string TodosProdutores = "Todos";
    private const int DiasAlerta = 7;
    private const int MaxItensLista = 8;

    private static readonly CultureInfo PtBr = new("pt-BR");
    private static readonly Brush CorPerigo = Congelar(0xEC, 0x32, 0x37);
    private static readonly Brush CorAtencao = Congelar(0xF5, 0x9E, 0x0B);
    private static readonly Brush CorAzul = Congelar(0x4E, 0x53, 0x99);
    private static readonly Brush CorVerde = Congelar(0xA7, 0xCF, 0x45);

    private readonly RelatorioRenovacaoService _renovacaoService;
    private readonly SeguroNovoService _seguroNovoService;
    private readonly LeadService _leadService;
    private readonly SessaoService _sessao;
    private bool _produtoresCarregados;
    private bool _carregando;

    // Renovações do produtor
    [ObservableProperty] private int _renovacoesVencidas;
    [ObservableProperty] private int _renovacoesProximas;
    [ObservableProperty] private int _renovacoesHoje;

    // Pendências administrativas (Ren. Palma + Seguros Novos)
    [ObservableProperty] private int _assinaturasPendentes;
    [ObservableProperty] private int _emissoesPendentes;

    // Indicadores complementares
    [ObservableProperty] private int _mesTotal;
    [ObservableProperty] private int _mesRenovadas;
    [ObservableProperty] private double _mesTaxaRenovacao;
    [ObservableProperty] private string _mesNome = string.Empty;
    [ObservableProperty] private int _segurosNovosMes;
    [ObservableProperty] private int _cotacoesEmAberto;
    [ObservableProperty] private int _renovacoesEmTratativa;

    [ObservableProperty] private string? _produtorSelecionado;
    [ObservableProperty] private bool _isLoading;

    public ObservableCollection<string> ProdutoresDisponiveis { get; } = [];
    public ObservableCollection<ItemAlertaInicio> RenovacoesEmAtencao { get; } = [];
    public ObservableCollection<ItemAlertaInicio> PendenciasEmissao { get; } = [];
    public ObservableCollection<BarraSituacaoInicio> Situacoes { get; } = [];

    public bool IsAdmin => _sessao.IsAdmin;
    public string NomeUsuario => _sessao.NomeUsuario;
    public string DataHoje => DateTime.Today.ToString("dddd, dd 'de' MMMM 'de' yyyy", PtBr);

    public bool SemRenovacoesEmAtencao => RenovacoesEmAtencao.Count == 0;
    public bool SemPendenciasEmissao => PendenciasEmissao.Count == 0;
    public string EscopoTexto => FiltroProdutor is null ? "Todos os produtores" : $"Produtor: {FiltroProdutor}";

    // null = sem restrição (administrador vendo todos); colaborador sempre vê só o que é seu
    private string? FiltroProdutor =>
        !_sessao.IsAdmin ? _sessao.NomeUsuario
        : string.IsNullOrEmpty(ProdutorSelecionado) || ProdutorSelecionado == TodosProdutores ? null
        : ProdutorSelecionado;

    public InicioViewModel(
        RelatorioRenovacaoService renovacaoService,
        SeguroNovoService seguroNovoService,
        LeadService leadService,
        SessaoService sessao)
    {
        _renovacaoService = renovacaoService;
        _seguroNovoService = seguroNovoService;
        _leadService = leadService;
        _sessao = sessao;
    }

    partial void OnProdutorSelecionadoChanged(string? value)
    {
        if (!_carregando) _ = CarregarAsync();
    }

    [RelayCommand]
    private void Navegar(string chave) => WeakReferenceMessenger.Default.Send(new NavegarMenuMessage(chave));

    public async Task CarregarAsync()
    {
        if (_carregando) return;
        _carregando = true;
        IsLoading = true;
        try
        {
            if (IsAdmin && !_produtoresCarregados)
            {
                var produtores = (await _renovacaoService.GetNovoProdutorDistinctAsync())
                    .Union(await _seguroNovoService.GetProdutoresDistinctAsync(), StringComparer.OrdinalIgnoreCase)
                    .OrderBy(p => p, StringComparer.CurrentCultureIgnoreCase);

                ProdutoresDisponiveis.Clear();
                ProdutoresDisponiveis.Add(TodosProdutores);
                foreach (var p in produtores) ProdutoresDisponiveis.Add(p);
                // Padrão: o próprio usuário logado (se for produtor); senão, todos
                ProdutorSelecionado ??= ProdutoresDisponiveis.FirstOrDefault(p =>
                    string.Equals(p, _sessao.NomeUsuario, StringComparison.OrdinalIgnoreCase)) ?? TodosProdutores;
                _produtoresCarregados = true;
            }

            var produtor = FiltroProdutor;
            var renovacoes = await _renovacaoService.GetResumoInicioAsync(produtor);
            var seguros = await _seguroNovoService.GetTodosAsync(produtor);
            var leads = await _leadService.GetTodosAsync();

            if (produtor is not null)
                leads = leads.Where(l => string.Equals(l.Produtor, produtor, StringComparison.OrdinalIgnoreCase)).ToList();

            AtualizarRenovacoes(renovacoes);
            AtualizarPendencias(renovacoes, seguros);
            AtualizarIndicadores(renovacoes, seguros, leads);
            OnPropertyChanged(nameof(EscopoTexto));
            OnPropertyChanged(nameof(DataHoje));
        }
        finally
        {
            _carregando = false;
            IsLoading = false;
        }
    }

    private void AtualizarRenovacoes(List<RelatorioRenovacao> renovacoes)
    {
        var hoje = DateTime.Today;
        var criticas = renovacoes
            .Where(r => r.SituacaoPendenteCritica && r.VigenciaFinal.HasValue)
            .ToList();

        RenovacoesVencidas = criticas.Count(r => r.VigenciaFinal!.Value.Date < hoje);
        RenovacoesHoje = criticas.Count(r => r.VigenciaFinal!.Value.Date == hoje);
        RenovacoesProximas = criticas.Count(r =>
        {
            var d = (r.VigenciaFinal!.Value.Date - hoje).Days;
            return d >= 0 && d <= DiasAlerta;
        });

        RenovacoesEmAtencao.Clear();
        foreach (var r in criticas
                     .Where(r => (r.VigenciaFinal!.Value.Date - hoje).Days <= DiasAlerta)
                     .OrderBy(r => r.VigenciaFinal)
                     .Take(MaxItensLista))
        {
            var dias = (r.VigenciaFinal!.Value.Date - hoje).Days;
            var (prazo, cor) = dias switch
            {
                < 0 => ($"Vencida há {-dias} dia{(dias == -1 ? "" : "s")}", CorPerigo),
                0 => ("Vence hoje", CorPerigo),
                1 => ("Vence amanhã", CorAtencao),
                _ => ($"Vence em {dias} dias", CorAtencao)
            };

            var detalhe = string.Join(" · ", new[] { r.Ramo, r.Seguradora, $"{r.VigenciaFinal:dd/MM/yyyy}" }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
            RenovacoesEmAtencao.Add(new ItemAlertaInicio(
                r.NomeCliente ?? "(sem nome)", detalhe, prazo, cor, "AcompanhamentoRenovacoes"));
        }
        OnPropertyChanged(nameof(SemRenovacoesEmAtencao));

        // Distribuição por situação (barras)
        var total = renovacoes.Count;
        int Conta(params string[] sit) => renovacoes.Count(r => sit.Contains(r.SituacaoAcompanhamento));
        var grupos = new (string Nome, int Qtd, Brush Cor)[]
        {
            ("À renovar / em tratativa", Conta("À Renovar", "Calculado", "Procurado", "Agendado"), CorAtencao),
            ("Renovadas (Palma)", Conta("Ren. Palma"), CorVerde),
            ("Emitidas", Conta("Emitido"), CorAzul),
            ("Perdidas / encerradas", Conta("Ren. Outro", "Não renovado", "Recusado", "Cancelado"), CorPerigo)
        };

        Situacoes.Clear();
        foreach (var g in grupos)
            Situacoes.Add(new BarraSituacaoInicio(g.Nome, g.Qtd, total == 0 ? 0 : g.Qtd * 100.0 / total, g.Cor));

        RenovacoesEmTratativa = grupos[0].Qtd;
    }

    private void AtualizarPendencias(List<RelatorioRenovacao> renovacoes, List<SeguroNovo> seguros)
    {
        var renPalma = renovacoes.Where(r => r.SituacaoAcompanhamento == "Ren. Palma").ToList();

        AssinaturasPendentes = renPalma.Count(r => !r.AssinaturaFeita) + seguros.Count(s => !s.AssinaturaFeita);
        EmissoesPendentes = renPalma.Count(r => !r.SeguroEmitido) + seguros.Count(s => !s.SeguroEmitido);

        var itens = new List<(DateTime? Data, ItemAlertaInicio Item)>();

        foreach (var r in renPalma.Where(r => !r.AssinaturaFeita || !r.SeguroEmitido))
            itens.Add((r.VigenciaFinal, new ItemAlertaInicio(
                r.NomeCliente ?? "(sem nome)",
                string.Join(" · ", new[] { "Renovação", r.Seguradora }.Where(s => !string.IsNullOrWhiteSpace(s))),
                Falta(r.AssinaturaFeita, r.SeguroEmitido),
                r.AssinaturaFeita ? CorAtencao : CorPerigo,
                "EmissaoDashboard")));

        foreach (var s in seguros.Where(s => !s.AssinaturaFeita || !s.SeguroEmitido))
            itens.Add((s.Vigencia, new ItemAlertaInicio(
                s.Segurado,
                string.Join(" · ", new[] { "Seguro novo", s.Cia }.Where(x => !string.IsNullOrWhiteSpace(x))),
                Falta(s.AssinaturaFeita, s.SeguroEmitido),
                s.AssinaturaFeita ? CorAtencao : CorPerigo,
                "EmissaoDashboard")));

        PendenciasEmissao.Clear();
        foreach (var (_, item) in itens
                     .OrderBy(i => i.Data ?? DateTime.MaxValue)
                     .Take(MaxItensLista))
            PendenciasEmissao.Add(item);
        OnPropertyChanged(nameof(SemPendenciasEmissao));

        static string Falta(bool assinatura, bool emitido) =>
            !assinatura && !emitido ? "Falta assinatura e emissão"
            : !assinatura ? "Falta assinatura"
            : "Falta emissão";
    }

    private void AtualizarIndicadores(List<RelatorioRenovacao> renovacoes, List<SeguroNovo> seguros, List<Lead> leads)
    {
        var hoje = DateTime.Today;

        var doMes = renovacoes
            .Where(r => r.VigenciaFinal.HasValue
                        && r.VigenciaFinal.Value.Year == hoje.Year
                        && r.VigenciaFinal.Value.Month == hoje.Month
                        && r.SituacaoAcompanhamento != "Cancelado")
            .ToList();

        MesTotal = doMes.Count;
        MesRenovadas = doMes.Count(r => r.RenovacaoRealizada);
        MesTaxaRenovacao = MesTotal == 0 ? 0 : MesRenovadas * 100.0 / MesTotal;
        var nome = hoje.ToString("MMMM", PtBr);
        MesNome = nome.Length > 0 ? char.ToUpper(nome[0], PtBr) + nome[1..] : nome;

        SegurosNovosMes = seguros.Count(s => s.CriadoEm.Year == hoje.Year && s.CriadoEm.Month == hoje.Month);
        CotacoesEmAberto = leads.Count(l => !l.Fechou);
    }

    private static Brush Congelar(byte r, byte g, byte b)
    {
        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
        brush.Freeze();
        return brush;
    }
}
