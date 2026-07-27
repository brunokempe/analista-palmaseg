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
    private string? _criadoPorOriginal;
    private ObservableCollection<SeguroNovo> _colecao = [];
    private ListCollectionView? _view;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private SeguroNovo? _registroSelecionado;
    [ObservableProperty] private string _filtroTexto = string.Empty;
    [ObservableProperty] private string _filtroProdutor = string.Empty;
    [ObservableProperty] private ICollectionView? _registrosView;

    // Campos do formulário
    [ObservableProperty] private int _editandoId;
    [ObservableProperty] private DateTime? _editandoVigencia;
    [ObservableProperty] private string _editandoSegurado = string.Empty;
    [ObservableProperty] private string _editandoCia = string.Empty;
    [ObservableProperty] private string _editandoSegmento = string.Empty;
    [ObservableProperty] private string _editandoStatus = string.Empty;
    [ObservableProperty] private string _editandoFinanceiro = string.Empty;
    [ObservableProperty] private decimal? _editandoPl;
    [ObservableProperty] private decimal? _editandoFator;
    [ObservableProperty] private decimal? _editandoValor;
    [ObservableProperty] private string _editandoFormaPagamento = string.Empty;
    [ObservableProperty] private int? _editandoParcelas;
    [ObservableProperty] private bool _editandoAssinaturaFeita;
    [ObservableProperty] private string _editandoObservacao = string.Empty;

    public bool IsAdmin => _sessao.IsAdmin;
    public bool TemRegistroSelecionado => EditandoId != 0;

    public ObservableCollection<string> ProdutoresDisponiveis { get; } = [];

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
        "Allianz", "AXA", "Bradesco Seguros", "Chubb", "Excelsior",
        "Generali", "HDI Seguros", "Liberty Seguros", "Mapfre Seguros",
        "Pottencial", "Porto Seguro", "Sompo", "SulAmérica", "Tokio Marine",
        "Zurich", "Outras"
    ];

    public static string[] FormasPagamento { get; } =
    [
        "À vista", "Boleto", "Cartão de crédito", "Débito em conta",
        "Débito automático", "Financiamento", "Parcelado no cartão"
    ];

    private static string ObterPastaAnexos(int id) =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AnalistaPalmaseg", "AnexosSeguroNovos", id.ToString());

    public SeguroNovosViewModel(SeguroNovoService service, SessaoService sessao)
    {
        _service = service;
        _sessao = sessao;
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
        EditandoVigencia     = r.Vigencia;
        EditandoSegurado     = r.Segurado;
        EditandoCia          = r.Cia;
        EditandoSegmento     = r.Segmento;
        EditandoStatus       = r.Status;
        EditandoFinanceiro   = r.Financeiro;
        EditandoPl           = r.Pl;
        EditandoFator        = r.Fator;
        EditandoValor        = r.Valor;
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
        EditandoVigencia     = DateTime.Today;
        EditandoSegurado     = string.Empty;
        EditandoCia          = string.Empty;
        EditandoSegmento     = string.Empty;
        EditandoStatus       = string.Empty;
        EditandoFinanceiro   = string.Empty;
        EditandoPl           = null;
        EditandoFator        = null;
        EditandoValor        = null;
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
            var produtorFiltro = _sessao.IsAdmin ? null : _sessao.NomeUsuario;
            var lista = await _service.GetTodosAsync(produtorFiltro);

            if (_sessao.IsAdmin)
            {
                var prods = await _service.GetProdutoresDistinctAsync();
                ProdutoresDisponiveis.Clear();
                ProdutoresDisponiveis.Add(string.Empty);
                foreach (var p in prods) ProdutoresDisponiveis.Add(p);
            }

            _colecao = new ObservableCollection<SeguroNovo>(lista);
            _view = (ListCollectionView)CollectionViewSource.GetDefaultView(_colecao);
            _view.Filter = FiltroItem;
            RegistrosView = _view;
        }
        finally { IsLoading = false; }
    }

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
    private async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(EditandoSegurado))
        {
            MessageBox.Show("Informe o nome do segurado.", "Campo obrigatório",
                MessageBoxButton.OK, MessageBoxImage.Warning);
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
                FormaPagamento  = EditandoFormaPagamento,
                Parcelas        = EditandoParcelas,
                AssinaturaFeita = EditandoAssinaturaFeita,
                Observacao      = EditandoObservacao.Trim(),
                CriadoEm        = DateTime.Now,
                CriadoPor       = EditandoId == 0 ? _sessao.NomeUsuario : _criadoPorOriginal,
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

                if (_sessao.IsAdmin && !string.IsNullOrEmpty(salvo.CriadoPor)
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
