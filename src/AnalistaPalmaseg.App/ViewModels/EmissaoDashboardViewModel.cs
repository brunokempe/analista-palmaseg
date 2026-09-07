using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
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
    private readonly AnexoService _anexoService;
    private readonly PastaProdutorService _pastaProdutorService;
    private List<RelatorioRenovacao> _todos = [];
    private List<SeguroNovo> _todosSeguroNovos = [];

    [ObservableProperty] private int _atualMes = DateTime.Now.Month;
    [ObservableProperty] private int _atualAno = DateTime.Now.Year;

    public static int[] Meses { get; } = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
    public static int[] Anos  { get; } = Enumerable.Range(DateTime.Now.Year - 3, 7).ToArray();

    [ObservableProperty] private int _totalRenPalma;
    [ObservableProperty] private int _assinaturasPendentes;
    [ObservableProperty] private int _emissoesPendentes;
    [ObservableProperty] private int _segurosEmitidos;
    [ObservableProperty] private string _filtroProdutor = string.Empty;
    [ObservableProperty] private string _filtroEmissao = "Todos";
    [ObservableProperty] private DateTime? _filtroData;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private RelatorioRenovacao? _renPalmaSelecionado;
    [ObservableProperty] private SeguroNovo? _seguroNovoSelecionado;

    public static string[] EmissaoOpcoes { get; } = ["Todos", "Pendente", "Realizada"];

    // ── Filtros de Seguros Novos ──────────────────────────────────────────────
    [ObservableProperty] private string _filtroSegNovSegurado = string.Empty;
    [ObservableProperty] private string _filtroSegNovStatus   = "Todos";

    public static string[] StatusSegNovoOpcoes { get; } =
        ["Todos", "Endosso", "Mensal", "Mercado", "Novo", "Prospecção", "Renovação"];

    partial void OnFiltroSegNovSeguradoChanged(string _)
    {
        AplicarFiltroSeguroNovos();
        AtualizarCards();
    }

    partial void OnFiltroSegNovStatusChanged(string _)
    {
        AplicarFiltroSeguroNovos();
        AtualizarCards();
    }

    partial void OnFiltroEmissaoChanged(string _)
    {
        AplicarFiltro();
        AplicarFiltroSeguroNovos();
        AtualizarCards();
    }

    partial void OnFiltroDataChanged(DateTime? value)
    {
        if (value.HasValue)
        {
            FiltroEmissao = "Pendente";

            if (AtualMes != value.Value.Month || AtualAno != value.Value.Year)
            {
                AtualMes = value.Value.Month;
                AtualAno = value.Value.Year;
                _ = CarregarAsync();
                return;
            }
        }

        AplicarFiltro();
        AplicarFiltroSeguroNovos();
        AtualizarCards();
    }

    public ObservableCollection<RelatorioRenovacao> Registros { get; } = [];
    public ObservableCollection<ProdutorEmissaoResumo> ResumoProdutor { get; } = [];
    public ObservableCollection<string> ProdutoresDisponiveis { get; } = [];
    public ObservableCollection<SeguroNovo> SeguroNovos { get; } = [];

    public EmissaoDashboardViewModel(
        RelatorioRenovacaoService service,
        SeguroNovoService seguroNovoService,
        SessaoService sessao,
        MetaService metaService,
        AnexoService anexoService,
        PastaProdutorService pastaProdutorService)
    {
        _service = service;
        _seguroNovoService = seguroNovoService;
        _sessao = sessao;
        _metaService = metaService;
        _anexoService = anexoService;
        _pastaProdutorService = pastaProdutorService;
        _filtroProdutor = _sessao.NomeUsuario;
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
        var registros    = RegistrosFiltrados().ToList();
        var seguroNovos  = SeguroNovosFiltrados().ToList();
        TotalRenPalma        = registros.Count + seguroNovos.Count;
        AssinaturasPendentes = registros.Count(r => !r.AssinaturaFeita) + seguroNovos.Count(s => !s.AssinaturaFeita);
        EmissoesPendentes    = registros.Count(r => !r.SeguroEmitido)   + seguroNovos.Count(s => !s.SeguroEmitido);
        SegurosEmitidos      = registros.Count(r => r.SeguroEmitido)    + seguroNovos.Count(s => s.SeguroEmitido);
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

    partial void OnFiltroProdutorChanged(string value)
    {
        AplicarFiltro();
        AplicarFiltroSeguroNovos();
        AtualizarCards();
    }

    private IEnumerable<RelatorioRenovacao> RegistrosFiltrados()
    {
        var fonte = TodosPeriodo();

        if (!string.IsNullOrWhiteSpace(FiltroProdutor))
            fonte = fonte.Where(r =>
                (string.IsNullOrWhiteSpace(r.NovoProdutor) ? "(Sem produtor)" : r.NovoProdutor) == FiltroProdutor);

        if (FiltroData.HasValue)
            fonte = fonte.Where(r =>
                r.VigenciaFinal!.Value.Date == FiltroData.Value.Date && !r.SeguroEmitido);
        else if (FiltroEmissao == "Pendente")
            fonte = fonte.Where(r => !r.SeguroEmitido);
        else if (FiltroEmissao == "Realizada")
            fonte = fonte.Where(r => r.SeguroEmitido);

        return fonte;
    }

    private IEnumerable<SeguroNovo> SeguroNovosFiltrados()
    {
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

        if (FiltroData.HasValue)
            query = query.Where(s =>
                s.Vigencia.HasValue && s.Vigencia.Value.Date == FiltroData.Value.Date && !s.SeguroEmitido);
        else if (FiltroEmissao == "Pendente")
            query = query.Where(s => !s.SeguroEmitido);
        else if (FiltroEmissao == "Realizada")
            query = query.Where(s => s.SeguroEmitido);

        return query;
    }

    private void AplicarFiltro()
    {
        Registros.Clear();
        foreach (var r in RegistrosFiltrados())
            Registros.Add(r);
    }

    private void AplicarFiltroSeguroNovos()
    {
        SeguroNovos.Clear();
        foreach (var s in SeguroNovosFiltrados())
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
            return;
        }

        if (reg.SeguroEmitido)
            await AnexarEDistribuirRenPalmaAsync(reg);
    }

    [RelayCommand]
    private async Task ToggleAssinaturaSeguroNovo(SeguroNovo? reg)
    {
        if (reg == null) return;
        reg.AssinaturaFeita = !reg.AssinaturaFeita;
        try
        {
            await _seguroNovoService.SalvarStatusAdministrativoAsync(reg);
        }
        catch (Exception ex)
        {
            reg.AssinaturaFeita = !reg.AssinaturaFeita;
            MessageBox.Show($"Erro ao salvar:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task ToggleSeguroNovoEmitido(SeguroNovo? reg)
    {
        if (reg == null) return;
        reg.SeguroEmitido = !reg.SeguroEmitido;
        reg.EmitidoPor    = reg.SeguroEmitido ? _sessao.NomeUsuario : null;
        try
        {
            await _seguroNovoService.SalvarStatusAdministrativoAsync(reg);
        }
        catch (Exception ex)
        {
            reg.SeguroEmitido = !reg.SeguroEmitido;
            reg.EmitidoPor    = reg.SeguroEmitido ? _sessao.NomeUsuario : null;
            MessageBox.Show($"Erro ao salvar:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (reg.SeguroEmitido)
            await AnexarEDistribuirSeguroNovoAsync(reg);
    }

    [RelayCommand]
    private void LimparFiltroData() => FiltroData = null;

    [RelayCommand]
    private async Task Recarregar() => await CarregarAsync();

    // ── Anexos: Contratos Ren. Palma ──────────────────────────────────────────

    private async Task AnexarEDistribuirRenPalmaAsync(RelatorioRenovacao reg)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Anexar arquivo da apólice emitida (opcional)",
            Multiselect = true,
            Filter = "Todos os arquivos|*.*|Imagens|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|PDF|*.pdf|Word|*.docx;*.doc|Excel|*.xlsx;*.xls"
        };
        if (dialog.ShowDialog() != true) return;

        IsLoading = true;
        try
        {
            int ok = 0, erros = 0;
            foreach (var file in dialog.FileNames)
            {
                try { await _anexoService.AdicionarAsync(reg.Id, file); ok++; }
                catch { erros++; }
            }

            var pastasDistribuidas = await DistribuirParaPastasProdutorAsync(reg.NovoProdutor, dialog.FileNames);

            var msg = $"{ok} arquivo(s) anexado(s)" + (erros > 0 ? $", {erros} com erro" : "") + "." +
                (pastasDistribuidas > 0
                    ? $"\nDistribuído para {pastasDistribuidas} pasta(s) cadastrada(s) do produtor."
                    : "\nNenhuma pasta cadastrada para o produtor — arquivo não distribuído.");
            MessageBox.Show(msg, "Anexar arquivos", MessageBoxButton.OK,
                erros == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task AnexarArquivosRenPalma()
    {
        var reg = RenPalmaSelecionado;
        if (reg == null) return;

        var dialog = new OpenFileDialog
        {
            Title = "Selecionar arquivo(s) para anexar",
            Multiselect = true,
            Filter = "Todos os arquivos|*.*|Imagens|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|PDF|*.pdf|Word|*.docx;*.doc|Excel|*.xlsx;*.xls"
        };
        if (dialog.ShowDialog() != true) return;

        IsLoading = true;
        int ok = 0, erros = 0;
        foreach (var file in dialog.FileNames)
        {
            try { await _anexoService.AdicionarAsync(reg.Id, file); ok++; }
            catch { erros++; }
        }
        IsLoading = false;

        var msg = erros == 0
            ? $"{ok} arquivo(s) anexado(s) com sucesso."
            : $"{ok} arquivo(s) anexado(s), {erros} com erro.";
        MessageBox.Show(msg, "Anexar arquivos", MessageBoxButton.OK,
            erros == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    [RelayCommand]
    private void AbrirPastaAnexosRenPalma()
    {
        var reg = RenPalmaSelecionado;
        if (reg == null) return;
        var pasta = AnexoService.ObterPasta(reg.Id);
        Directory.CreateDirectory(pasta);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pasta)
            { UseShellExecute = true });
    }

    // ── Anexos: Seguros Novos ──────────────────────────────────────────────────

    private static string ObterPastaAnexosSeguroNovo(int id) =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AnalistaPalmaseg", "AnexosSeguroNovos", id.ToString());

    private async Task AnexarEDistribuirSeguroNovoAsync(SeguroNovo reg)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Anexar arquivo da apólice emitida (opcional)",
            Multiselect = true,
            Filter = "Todos os arquivos|*.*|Imagens|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|PDF|*.pdf|Word|*.docx;*.doc|Excel|*.xlsx;*.xls"
        };
        if (dialog.ShowDialog() != true) return;

        var pasta = ObterPastaAnexosSeguroNovo(reg.Id);

        IsLoading = true;
        try
        {
            int ok = 0, erros = 0;
            try { AnexoService.CopiarParaDiretorio(dialog.FileNames, pasta); ok = dialog.FileNames.Length; }
            catch { erros = dialog.FileNames.Length; }

            var pastasDistribuidas = await DistribuirParaPastasProdutorAsync(reg.CriadoPor, dialog.FileNames);

            var msg = $"{ok} arquivo(s) anexado(s)" + (erros > 0 ? $", {erros} com erro" : "") + "." +
                (pastasDistribuidas > 0
                    ? $"\nDistribuído para {pastasDistribuidas} pasta(s) cadastrada(s) do produtor."
                    : "\nNenhuma pasta cadastrada para o produtor — arquivo não distribuído.");
            MessageBox.Show(msg, "Anexos", MessageBoxButton.OK,
                erros == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
        }
        finally { IsLoading = false; }
    }

    // ── Distribuição para pastas cadastradas do produtor ───────────────────────
    private async Task<int> DistribuirParaPastasProdutorAsync(string? produtorLogin, IEnumerable<string> arquivos)
    {
        var pastas = await _pastaProdutorService.GetDiretoriosPorLoginAsync(produtorLogin);
        var lista = arquivos.ToList();
        var distribuidas = 0;
        foreach (var pasta in pastas)
        {
            try { AnexoService.CopiarParaDiretorio(lista, pasta.Caminho); distribuidas++; }
            catch { /* pasta pode estar indisponível (drive removido/rede offline) — segue para as demais */ }
        }
        return distribuidas;
    }

    [RelayCommand]
    private async Task AnexarArquivosSeguroNovo()
    {
        var reg = SeguroNovoSelecionado;
        if (reg == null) return;

        var dialog = new OpenFileDialog
        {
            Title = "Selecionar arquivo(s) para anexar",
            Multiselect = true,
            Filter = "Todos os arquivos|*.*|Imagens|*.jpg;*.jpeg;*.png;*.gif;*.bmp;*.webp|PDF|*.pdf|Word|*.docx;*.doc|Excel|*.xlsx;*.xls"
        };
        if (dialog.ShowDialog() != true) return;

        var pasta = ObterPastaAnexosSeguroNovo(reg.Id);
        Directory.CreateDirectory(pasta);

        int ok = 0, erros = 0;
        foreach (var file in dialog.FileNames)
        {
            try
            {
                var dest = Path.Combine(pasta, Path.GetFileName(file));
                File.Copy(file, dest, overwrite: true);
                ok++;
            }
            catch { erros++; }
        }

        var msg = erros == 0
            ? $"{ok} arquivo(s) anexado(s) com sucesso."
            : $"{ok} arquivo(s) anexado(s). {erros} falhou(ram).";
        MessageBox.Show(msg, "Anexos", MessageBoxButton.OK,
            erros == 0 ? MessageBoxImage.Information : MessageBoxImage.Warning);
    }

    [RelayCommand]
    private void AbrirPastaAnexosSeguroNovo()
    {
        var reg = SeguroNovoSelecionado;
        if (reg == null) return;
        var pasta = ObterPastaAnexosSeguroNovo(reg.Id);
        Directory.CreateDirectory(pasta);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pasta)
            { UseShellExecute = true });
    }

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
