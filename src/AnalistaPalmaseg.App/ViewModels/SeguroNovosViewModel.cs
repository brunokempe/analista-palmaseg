using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class SeguroNovosViewModel : ObservableObject
{
    private readonly SeguroNovoService _service;
    private readonly SessaoService _sessao;
    private readonly UsuarioService _usuarios;
    private readonly ClienteService _clientes;
    private string? _criadoPorOriginal;
    private ObservableCollection<SeguroNovo> _colecao = [];
    private ListCollectionView? _view;
    private List<Cliente> _clientesCadastrados = [];

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private SeguroNovo? _registroSelecionado;
    [ObservableProperty] private string _filtroTexto = string.Empty;
    [ObservableProperty] private string _filtroProdutor = string.Empty;
    [ObservableProperty] private ICollectionView? _registrosView;

    // Campos do formulário
    [ObservableProperty] private int _editandoId;
    [ObservableProperty] private string _editandoProdutor = string.Empty;
    [ObservableProperty] private DateTime? _editandoVigencia;
    [ObservableProperty] private string _editandoSegurado = string.Empty;
    [ObservableProperty] private string _editandoCia = string.Empty;
    [ObservableProperty] private string _editandoSegmento = string.Empty;
    [ObservableProperty] private string _editandoStatus = string.Empty;
    [ObservableProperty] private string _editandoFinanceiro = string.Empty;
    [ObservableProperty] private decimal? _editandoPl;
    [ObservableProperty] private decimal? _editandoFator;
    [ObservableProperty] private decimal? _editandoValor;
    [ObservableProperty] private decimal? _editandoPremioTotal;
    [ObservableProperty] private string _editandoFormaPagamento = string.Empty;
    [ObservableProperty] private int? _editandoParcelas;
    [ObservableProperty] private bool _editandoAssinaturaFeita;
    [ObservableProperty] private string _editandoObservacao = string.Empty;

    private bool _atualizandoPremio;

    // PL informado: PT é somente leitura (PL + IOF). PL zerado: PT editável e calcula o PL.
    public bool PremioTotalEditavel => EditandoValor is not > 0;
    public bool PremioTotalSomenteLeitura => !PremioTotalEditavel;
    public decimal AliquotaIof => IofHelper.ObterAliquota(EditandoSegmento);
    public string PremioTotalLabel => $"PT c/ IOF {AliquotaIof:0.##}% (R$)";

    partial void OnEditandoValorChanged(decimal? value)
    {
        OnPropertyChanged(nameof(PremioTotalEditavel)); OnPropertyChanged(nameof(PremioTotalSomenteLeitura));
        if (_atualizandoPremio) return;
        _atualizandoPremio = true;
        EditandoPremioTotal = value is > 0 ? IofHelper.PlParaPt(value.Value, AliquotaIof) : null;
        _atualizandoPremio = false;
    }

    partial void OnEditandoPremioTotalChanged(decimal? value)
    {
        if (_atualizandoPremio || !PremioTotalEditavel) return;
        _atualizandoPremio = true;
        EditandoValor = value is > 0 ? IofHelper.PtParaPl(value.Value, AliquotaIof) : null;
        _atualizandoPremio = false;
        OnPropertyChanged(nameof(PremioTotalEditavel)); OnPropertyChanged(nameof(PremioTotalSomenteLeitura));
    }

    partial void OnEditandoSegmentoChanged(string value)
    {
        OnPropertyChanged(nameof(AliquotaIof));
        OnPropertyChanged(nameof(PremioTotalLabel));
        if (_atualizandoPremio || EditandoValor is not > 0) return;
        _atualizandoPremio = true;
        EditandoPremioTotal = IofHelper.PlParaPt(EditandoValor!.Value, AliquotaIof);
        _atualizandoPremio = false;
    }

    public bool IsAdmin => _sessao.IsAdmin;
    public bool TemRegistroSelecionado => EditandoId != 0;

    public ObservableCollection<string> ProdutoresDisponiveis { get; } = [];
    public ObservableCollection<string> ListaProdutores { get; } = [];
    public ObservableCollection<string> NomesClientesDisponiveis { get; } = [];

    public static string[] Segmentos { get; } =
    [
        "Auto", "Resid", "Empresa", "Resp. Civil", "Seguro Viagem",
        "Vida individual", "Vida empresarial", "Transporte (em parceria)",
        "Transporte", "Demais Seguros", "Cartão de crédito", "Financeiro/Outros"
    ];

    public static string[] StatusOpcoes { get; } =
    [
        "Endosso", "Mensal", "Mercado", "Novo", "Prospecção", "Renovação"
    ];

    public static string[] Seguradoras { get; } =
    [
        "Aliro", "Allianz", "Allseg", "AssistCard", "Axa", "Azul",
        "Bradesco", "Capemisa", "Chubb", "Darwin", "Ezze", "Hdi",
        "Itaú", "Mapfre", "Metlife", "Mitsui", "Pier", "Porto",
        "Sancor", "Sompo", "Suhai", "SulAm", "Sura", "Tokio",
        "Unimed", "Yelum", "Zurich", "OUTRAS"
    ];

    public static string[] FormasPagamento { get; } =
    [
        "À vista", "Boleto", "Cartão de crédito", "Débito em conta",
        "Débito automático", "Financiamento", "Parcelado no cartão"
    ];

    public static int[] Parcelamentos { get; } =
    [
        1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 24, 30, 36
    ];

    private static string ObterPastaAnexos(int id) =>
        Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Anexos", "SeguroNovos", id.ToString());

    public SeguroNovosViewModel(SeguroNovoService service, SessaoService sessao, UsuarioService usuarios, ClienteService clientes)
    {
        _service = service;
        _sessao = sessao;
        _usuarios = usuarios;
        _clientes = clientes;
        _editandoProdutor = _sessao.NomeUsuario;
        _filtroProdutor = _sessao.NomeUsuario;
    }

    partial void OnRegistroSelecionadoChanged(SeguroNovo? value)
    {
        if (value == null) return;
        PopularFormulario(value);
    }

    private void PopularFormulario(SeguroNovo r)
    {
        _criadoPorOriginal   = r.CriadoPor;
        EditandoId           = r.Id;
        EditandoProdutor     = r.CriadoPor ?? string.Empty;
        EditandoVigencia     = r.Vigencia;
        EditandoSegurado     = r.Segurado;
        EditandoCia          = r.Cia;
        EditandoSegmento     = r.Segmento;
        EditandoStatus       = r.Status;
        EditandoFinanceiro   = r.Financeiro;
        EditandoPl           = r.Pl;
        EditandoFator        = r.Fator;
        EditandoValor        = r.Valor;
        EditandoPremioTotal  = r.PremioTotal;
        EditandoFormaPagamento = r.FormaPagamento;
        EditandoParcelas     = r.Parcelas;
        EditandoAssinaturaFeita = r.AssinaturaFeita;
        EditandoObservacao   = r.Observacao;
        OnPropertyChanged(nameof(TemRegistroSelecionado));
        AnexarArquivosCommand.NotifyCanExecuteChanged();
        AbrirPastaAnexosCommand.NotifyCanExecuteChanged();
    }

    private void LimparFormulario()
    {
        _criadoPorOriginal   = null;
        EditandoId           = 0;
        EditandoProdutor     = _sessao.NomeUsuario;
        EditandoVigencia     = DateTime.Today;
        EditandoSegurado     = string.Empty;
        EditandoCia          = string.Empty;
        EditandoSegmento     = string.Empty;
        EditandoStatus       = string.Empty;
        EditandoFinanceiro   = string.Empty;
        EditandoPl           = null;
        EditandoFator        = null;
        EditandoValor        = null;
        EditandoPremioTotal  = null;
        EditandoFormaPagamento = string.Empty;
        EditandoParcelas     = null;
        EditandoAssinaturaFeita = false;
        EditandoObservacao   = string.Empty;
        RegistroSelecionado  = null;
        OnPropertyChanged(nameof(TemRegistroSelecionado));
        AnexarArquivosCommand.NotifyCanExecuteChanged();
        AbrirPastaAnexosCommand.NotifyCanExecuteChanged();
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            var lista = await _service.GetTodosAsync();

            var prods = await _service.GetProdutoresDistinctAsync();
            ProdutoresDisponiveis.Clear();
            ProdutoresDisponiveis.Add(string.Empty);
            foreach (var p in prods) ProdutoresDisponiveis.Add(p);

            var usuarios = await _usuarios.ListarAsync();
            ListaProdutores.Clear();
            foreach (var u in usuarios.Where(u => u.Ativo).OrderBy(u => u.Login))
                ListaProdutores.Add(u.Login);

            await CarregarClientesAsync();

            _colecao = new ObservableCollection<SeguroNovo>(lista);
            _view = (ListCollectionView)CollectionViewSource.GetDefaultView(_colecao);
            _view.Filter = FiltroItem;
            RegistrosView = _view;
        }
        finally { IsLoading = false; }
    }

    private async Task CarregarClientesAsync()
    {
        _clientesCadastrados = await _clientes.GetTodosAsync();
        NomesClientesDisponiveis.Clear();
        foreach (var c in _clientesCadastrados) NomesClientesDisponiveis.Add(c.Nome);
    }

    private Cliente? BuscarClienteCadastrado(string nome) =>
        _clientesCadastrados.FirstOrDefault(c =>
            string.Equals(c.Nome?.Trim(), nome.Trim(), StringComparison.OrdinalIgnoreCase));

    private bool FiltroItem(object obj)
    {
        if (obj is not SeguroNovo r) return false;

        if (!string.IsNullOrEmpty(FiltroProdutor) && r.CriadoPor != FiltroProdutor)
            return false;

        if (!string.IsNullOrWhiteSpace(FiltroTexto))
        {
            var txt = FiltroTexto.Trim().ToLowerInvariant();
            return (r.Segurado?.ToLowerInvariant().Contains(txt) == true) ||
                   (r.Cia?.ToLowerInvariant().Contains(txt) == true) ||
                   (r.Segmento?.ToLowerInvariant().Contains(txt) == true) ||
                   (r.Status?.ToLowerInvariant().Contains(txt) == true);
        }
        return true;
    }

    private void AplicarFiltro() => _view?.Refresh();

    partial void OnFiltroTextoChanged(string value) => AplicarFiltro();
    partial void OnFiltroProdutorChanged(string value) => AplicarFiltro();

    [RelayCommand]
    private void LimparFiltros()
    {
        FiltroTexto = string.Empty;
        FiltroProdutor = string.Empty;
    }

    [RelayCommand]
    private void Novo() => LimparFormulario();

    [RelayCommand]
    private async Task SelecionarClienteAsync() => await AbrirSeletorClienteAsync(EditandoSegurado);

    private async Task AbrirSeletorClienteAsync(string buscaInicial)
    {
        var dialog = new AnalistaPalmaseg.App.Views.ClienteSelectorDialog(_clientes, buscaInicial)
        {
            Owner = Application.Current.MainWindow
        };
        if (dialog.ShowDialog() != true || dialog.ClienteSelecionado == null) return;

        if (!_clientesCadastrados.Any(c => c.Id == dialog.ClienteSelecionado.Id))
            await CarregarClientesAsync();

        EditandoSegurado = dialog.ClienteSelecionado.Nome;
    }

    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(EditandoSegurado))
        {
            MessageBox.Show("Informe o nome do segurado.", "Campo obrigatório",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (BuscarClienteCadastrado(EditandoSegurado) == null)
        {
            MessageBox.Show(
                "Esse cliente ainda não está cadastrado. Selecione um cliente existente ou cadastre um novo na tela de pesquisa.",
                "Cliente não encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
            await AbrirSeletorClienteAsync(EditandoSegurado);
            return;
        }

        IsLoading = true;
        try
        {
            var entidade = new SeguroNovo
            {
                Id              = EditandoId,
                Vigencia        = EditandoVigencia,
                Segurado        = EditandoSegurado.Trim(),
                Cia             = EditandoCia.Trim(),
                Segmento        = EditandoSegmento,
                Status          = EditandoStatus,
                Financeiro      = EditandoFinanceiro.Trim(),
                Pl              = EditandoPl,
                Fator           = EditandoFator,
                Valor           = EditandoValor,
                PremioTotal     = EditandoPremioTotal,
                FormaPagamento  = EditandoFormaPagamento,
                Parcelas        = EditandoParcelas,
                AssinaturaFeita = EditandoAssinaturaFeita,
                Observacao      = EditandoObservacao.Trim(),
                CriadoEm        = DateTime.Now,
                CriadoPor       = string.IsNullOrWhiteSpace(EditandoProdutor)
                                    ? (EditandoId == 0 ? _sessao.NomeUsuario : _criadoPorOriginal)
                                    : EditandoProdutor.Trim(),
                EmitidoPor      = _sessao.NomeUsuario
            };

            var salvo = await _service.SalvarAsync(entidade);

            var existente = _colecao.FirstOrDefault(r => r.Id == salvo.Id);
            if (existente != null)
            {
                var idx = _colecao.IndexOf(existente);
                _colecao[idx] = salvo;
            }
            else
            {
                _colecao.Insert(0, salvo);

                if (!string.IsNullOrEmpty(salvo.CriadoPor)
                    && !ProdutoresDisponiveis.Contains(salvo.CriadoPor))
                    ProdutoresDisponiveis.Add(salvo.CriadoPor);
            }

            RegistroSelecionado = salvo;
            PopularFormulario(salvo);

            if (salvo.Vigencia.HasValue)
                WeakReferenceMessenger.Default.Send(
                    new DashboardRefreshMessage(salvo.Vigencia.Value.Month, salvo.Vigencia.Value.Year));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao salvar",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ExcluirAsync()
    {
        if (EditandoId == 0) return;

        var confirmacao = MessageBox.Show(
            $"Excluir o registro de \"{EditandoSegurado}\"?",
            "Confirmar exclusão",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirmacao != MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            await _service.ExcluirAsync(EditandoId);
            var existente = _colecao.FirstOrDefault(r => r.Id == EditandoId);
            if (existente != null) _colecao.Remove(existente);
            LimparFormulario();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao excluir",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void Cancelar() => LimparFormulario();

    [RelayCommand(CanExecute = nameof(TemRegistroSelecionado))]
    private async Task AnexarArquivosAsync()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Selecionar arquivos para anexar",
            Filter = "Todos os arquivos|*.*",
            Multiselect = true
        };
        if (dialog.ShowDialog() != true) return;

        var pasta = ObterPastaAnexos(EditandoId);
        Directory.CreateDirectory(pasta);

        int ok = 0, erros = 0;
        foreach (var file in dialog.FileNames)
        {
            try
            {
                var nomeSrc = Path.GetFileName(file);
                var ext = Path.GetExtension(file);
                var stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                var nomeDest = $"{Path.GetFileNameWithoutExtension(nomeSrc)}_{stamp}{ext}";
                File.Copy(file, Path.Combine(pasta, nomeDest));
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

    [RelayCommand(CanExecute = nameof(TemRegistroSelecionado))]
    private void AbrirPastaAnexos()
    {
        var pasta = ObterPastaAnexos(EditandoId);
        Directory.CreateDirectory(pasta);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(pasta)
            { UseShellExecute = true });
    }
}
