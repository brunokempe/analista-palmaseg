using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class ClientesViewModel : ObservableObject
{
    private readonly ClienteService _service;
    private readonly SessaoService _sessao;
    private ObservableCollection<Cliente> _colecao = [];
    private ListCollectionView? _view;
    private bool _carregandoFormulario;
    private static readonly System.Net.Http.HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(8) };

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private Cliente? _clienteSelecionado;
    [ObservableProperty] private string _filtroTexto = string.Empty;
    [ObservableProperty] private ICollectionView? _clientesView;

    // Identificação
    [ObservableProperty] private int _editandoId;
    [ObservableProperty] private string _editandoCpf = string.Empty;
    [ObservableProperty] private string _editandoNome = string.Empty;
    [ObservableProperty] private DateTime? _editandoNascimento;
    [ObservableProperty] private string _editandoSexo = string.Empty;
    [ObservableProperty] private string _editandoEstadoCivil = string.Empty;
    [ObservableProperty] private string _editandoProfissao = string.Empty;
    [ObservableProperty] private DateTime? _editandoClienteDesde;

    // Contato
    [ObservableProperty] private string _editandoPrefixo1 = string.Empty;
    [ObservableProperty] private string _editandoTelefone1 = string.Empty;
    [ObservableProperty] private string _editandoPrefixo2 = string.Empty;
    [ObservableProperty] private string _editandoTelefone2 = string.Empty;
    [ObservableProperty] private string _editandoPrefixo3 = string.Empty;
    [ObservableProperty] private string _editandoTelefone3 = string.Empty;
    [ObservableProperty] private string _editandoEmail1 = string.Empty;
    [ObservableProperty] private string _editandoEmail2 = string.Empty;

    // Endereço
    [ObservableProperty] private string _editandoCep = string.Empty;
    [ObservableProperty] private string _editandoEndereco = string.Empty;
    [ObservableProperty] private string _editandoNumeroEndereco = string.Empty;
    [ObservableProperty] private string _editandoComplemento = string.Empty;
    [ObservableProperty] private string _editandoBairro = string.Empty;
    [ObservableProperty] private string _editandoCidade = string.Empty;
    [ObservableProperty] private string _editandoEstado = string.Empty;

    // Notas (manuais)
    [ObservableProperty] private string _editandoObservacoes = string.Empty;
    [ObservableProperty] private string _editandoHistorico = string.Empty;

    // Seguros relacionados ao cliente selecionado
    [ObservableProperty] private ObservableCollection<RelatorioRenovacao> _seguros = [];

    public bool TemClienteSelecionado => EditandoId != 0 || ClienteSelecionado != null;
    public bool IsNovo                => EditandoId == 0;
    public bool IsAdmin               => _sessao.IsAdmin;
    public bool IsNotAdmin            => !_sessao.IsAdmin;

    public ClientesViewModel(ClienteService service, SessaoService sessao)
    {
        _service = service;
        _sessao  = sessao;
    }

    partial void OnClienteSelecionadoChanged(Cliente? value)
    {
        if (value == null) return;
        PopularFormulario(value);
        _ = CarregarSegurosAsync(value.Cpf);
    }

    partial void OnEditandoCepChanged(string value)
    {
        if (_carregandoFormulario) return;
        var digitos = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digitos.Length == 8) _ = BuscarEnderecoPorCepAsync(digitos);
    }

    private async Task BuscarEnderecoPorCepAsync(string cep)
    {
        try
        {
            using var resp = await _http.GetAsync($"https://viacep.com.br/ws/{cep}/json/");
            if (!resp.IsSuccessStatusCode) return;
            using var doc = await System.Text.Json.JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync());
            var root = doc.RootElement;
            if (root.TryGetProperty("erro", out _)) return;

            // Ignora resposta atrasada se o CEP já foi alterado
            var atual = new string(EditandoCep.Where(char.IsDigit).ToArray());
            if (atual != cep) return;

            string Get(string k) => root.TryGetProperty(k, out var v) ? v.GetString() ?? string.Empty : string.Empty;

            EditandoEndereco = Get("logradouro");
            EditandoBairro   = Get("bairro");
            EditandoCidade   = Get("localidade");
            EditandoEstado   = Get("uf");
        }
        catch
        {
            // Sem conexão ou CEP inválido: o usuário pode preencher manualmente.
        }
    }

    private void PopularFormulario(Cliente c)
    {
        _carregandoFormulario = true;
        try { PopularFormularioInterno(c); }
        finally { _carregandoFormulario = false; }
    }

    private void PopularFormularioInterno(Cliente c)
    {
        EditandoId            = c.Id;
        EditandoCpf           = c.Cpf;
        EditandoNome          = c.Nome;
        EditandoNascimento    = c.Nascimento;
        EditandoSexo          = c.Sexo ?? string.Empty;
        EditandoEstadoCivil   = c.EstadoCivil ?? string.Empty;
        EditandoProfissao     = c.Profissao ?? string.Empty;
        EditandoClienteDesde  = c.ClienteDesde;
        EditandoPrefixo1      = c.Prefixo1 ?? string.Empty;
        EditandoTelefone1     = c.Telefone1 ?? string.Empty;
        EditandoPrefixo2      = c.Prefixo2 ?? string.Empty;
        EditandoTelefone2     = c.Telefone2 ?? string.Empty;
        EditandoPrefixo3      = c.Prefixo3 ?? string.Empty;
        EditandoTelefone3     = c.Telefone3 ?? string.Empty;
        EditandoEmail1        = c.Email1 ?? string.Empty;
        EditandoEmail2        = c.Email2 ?? string.Empty;
        EditandoCep           = c.Cep ?? string.Empty;
        EditandoEndereco      = c.Endereco ?? string.Empty;
        EditandoNumeroEndereco = c.NumeroEndereco ?? string.Empty;
        EditandoComplemento   = c.Complemento ?? string.Empty;
        EditandoBairro        = c.Bairro ?? string.Empty;
        EditandoCidade        = c.Cidade ?? string.Empty;
        EditandoEstado        = c.Estado ?? string.Empty;
        EditandoObservacoes   = c.Observacoes ?? string.Empty;
        EditandoHistorico     = c.Historico ?? string.Empty;
        OnPropertyChanged(nameof(TemClienteSelecionado));
        OnPropertyChanged(nameof(IsNovo));
    }

    private void LimparFormulario()
    {
        _carregandoFormulario = true;
        try { LimparFormularioInterno(); }
        finally { _carregandoFormulario = false; }
    }

    private void LimparFormularioInterno()
    {
        EditandoId            = 0;
        EditandoCpf           = string.Empty;
        EditandoNome          = string.Empty;
        EditandoNascimento    = null;
        EditandoSexo          = string.Empty;
        EditandoEstadoCivil   = string.Empty;
        EditandoProfissao     = string.Empty;
        EditandoClienteDesde  = null;
        EditandoPrefixo1      = string.Empty;
        EditandoTelefone1     = string.Empty;
        EditandoPrefixo2      = string.Empty;
        EditandoTelefone2     = string.Empty;
        EditandoPrefixo3      = string.Empty;
        EditandoTelefone3     = string.Empty;
        EditandoEmail1        = string.Empty;
        EditandoEmail2        = string.Empty;
        EditandoCep           = string.Empty;
        EditandoEndereco      = string.Empty;
        EditandoNumeroEndereco = string.Empty;
        EditandoComplemento   = string.Empty;
        EditandoBairro        = string.Empty;
        EditandoCidade        = string.Empty;
        EditandoEstado        = string.Empty;
        EditandoObservacoes   = string.Empty;
        EditandoHistorico     = string.Empty;
        ClienteSelecionado    = null;
        Seguros.Clear();
        OnPropertyChanged(nameof(TemClienteSelecionado));
        OnPropertyChanged(nameof(IsNovo));
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            var lista = await _service.GetTodosAsync();
            _colecao = new ObservableCollection<Cliente>(lista);
            _view = (ListCollectionView)CollectionViewSource.GetDefaultView(_colecao);
            _view.Filter = FiltroItem;
            ClientesView = _view;
        }
        finally { IsLoading = false; }
    }

    private async Task CarregarSegurosAsync(string cpf)
    {
        if (string.IsNullOrWhiteSpace(cpf))
        {
            Seguros.Clear();
            return;
        }
        var lista = await _service.GetSegurosDoClienteAsync(cpf);
        Seguros = new ObservableCollection<RelatorioRenovacao>(lista);
    }

    private bool FiltroItem(object obj)
    {
        if (obj is not Cliente c) return false;
        if (string.IsNullOrWhiteSpace(FiltroTexto)) return true;
        var txt = FiltroTexto.Trim().ToLowerInvariant();
        return c.Nome.ToLowerInvariant().Contains(txt) ||
               c.Cpf.ToLowerInvariant().Contains(txt) ||
               (c.Cidade?.ToLowerInvariant().Contains(txt) == true);
    }

    partial void OnFiltroTextoChanged(string value) => _view?.Refresh();

    public void SelecionarPorDocumento(string documentoPrincipal)
    {
        FiltroTexto = string.Empty;
        var cliente = _colecao.FirstOrDefault(c =>
            string.Equals(c.Cpf, documentoPrincipal, StringComparison.OrdinalIgnoreCase));
        if (cliente != null) ClienteSelecionado = cliente;
    }

    [RelayCommand]
    private void Novo() => LimparFormulario();

    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(EditandoNome))
        {
            MessageBox.Show("Informe o nome do cliente.", "Campo obrigatório",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        try
        {
            var entidade = new Cliente
            {
                Id              = EditandoId,
                Cpf             = EditandoCpf.Trim(),
                Nome            = EditandoNome.Trim(),
                Nascimento      = EditandoNascimento,
                Sexo            = Vazio(EditandoSexo),
                EstadoCivil     = Vazio(EditandoEstadoCivil),
                Profissao       = Vazio(EditandoProfissao),
                ClienteDesde    = EditandoClienteDesde,
                Prefixo1        = Vazio(EditandoPrefixo1),
                Telefone1       = Vazio(EditandoTelefone1),
                Prefixo2        = Vazio(EditandoPrefixo2),
                Telefone2       = Vazio(EditandoTelefone2),
                Prefixo3        = Vazio(EditandoPrefixo3),
                Telefone3       = Vazio(EditandoTelefone3),
                Email1          = Vazio(EditandoEmail1),
                Email2          = Vazio(EditandoEmail2),
                Cep             = Vazio(EditandoCep),
                Endereco        = Vazio(EditandoEndereco),
                NumeroEndereco  = Vazio(EditandoNumeroEndereco),
                Complemento     = Vazio(EditandoComplemento),
                Bairro          = Vazio(EditandoBairro),
                Cidade          = Vazio(EditandoCidade),
                Estado          = Vazio(EditandoEstado),
                Observacoes     = Vazio(EditandoObservacoes),
                Historico       = Vazio(EditandoHistorico),
                CriadoEm       = DateTime.Now
            };

            var salvo = await _service.SalvarAsync(entidade);

            var existente = _colecao.FirstOrDefault(c => c.Id == salvo.Id);
            if (existente != null)
                _colecao[_colecao.IndexOf(existente)] = salvo;
            else
                _colecao.Insert(0, salvo);

            ClienteSelecionado = salvo;
            PopularFormulario(salvo);
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
            $"Excluir o cliente \"{EditandoNome}\"?",
            "Confirmar exclusão",
            MessageBoxButton.YesNo, MessageBoxImage.Question);

        if (confirmacao != MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            await _service.ExcluirAsync(EditandoId);
            var existente = _colecao.FirstOrDefault(c => c.Id == EditandoId);
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

    private static string? Vazio(string s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
}
