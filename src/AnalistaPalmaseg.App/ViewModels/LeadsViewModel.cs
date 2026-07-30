using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class LeadsViewModel : ObservableObject
{
    private readonly LeadService _leadService;
    private readonly SeguroNovoService _seguroNovoService;
    private readonly SessaoService _sessao;
    private ObservableCollection<Lead> _colecao = [];
    private ListCollectionView? _view;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private Lead? _leadSelecionado;
    [ObservableProperty] private string _filtroTexto = string.Empty;
    [ObservableProperty] private ICollectionView? _leadsView;

    // Campos do formulário
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemLeadSelecionado), nameof(IsNovo))]
    private int _editandoId;

    [ObservableProperty] private string _editandoSegurado   = string.Empty;
    [ObservableProperty] private string _editandoProdutor   = string.Empty;
    [ObservableProperty] private DateTime _editandoCriadoEm = DateTime.Now;
    [ObservableProperty] private string _editandoIndicacao  = string.Empty;
    [ObservableProperty] private string _editandoObservacao = string.Empty;
    [ObservableProperty] private bool   _editandoSeguroGerado;
    [ObservableProperty] private bool   _editandoFechou;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TemSeguroNovoCriado))]
    private int? _editandoSeguroNovoId;

    public bool TemLeadSelecionado  => EditandoId != 0;
    public bool IsNovo              => EditandoId == 0;
    public bool TemSeguroNovoCriado => EditandoSeguroNovoId.HasValue;

    public static string[] FontesIndicacao { get; } =
    [
        "Indicação de cliente", "Redes sociais", "Site / Google",
        "WhatsApp", "Ligação ativa", "Parceiro", "Evento", "Outros"
    ];

    public LeadsViewModel(LeadService leadService, SeguroNovoService seguroNovoService, SessaoService sessao)
    {
        _leadService       = leadService;
        _seguroNovoService = seguroNovoService;
        _sessao            = sessao;
    }

    partial void OnLeadSelecionadoChanged(Lead? value)
    {
        if (value == null) return;
        PopularFormulario(value);
    }

    partial void OnFiltroTextoChanged(string value) => _view?.Refresh();

    private void PopularFormulario(Lead l)
    {
        EditandoId           = l.Id;
        EditandoSegurado     = l.Segurado;
        EditandoProdutor     = l.Produtor;
        EditandoCriadoEm     = l.CriadoEm;
        EditandoIndicacao    = l.Indicacao   ?? string.Empty;
        EditandoObservacao   = l.Observacao  ?? string.Empty;
        EditandoSeguroGerado = l.SeguroGerado;
        EditandoFechou       = l.Fechou;
        EditandoSeguroNovoId = l.SeguroNovoId;
    }

    private void LimparFormulario()
    {
        EditandoId           = 0;
        EditandoSegurado     = string.Empty;
        EditandoProdutor     = _sessao.NomeUsuario;
        EditandoCriadoEm     = DateTime.Now;
        EditandoIndicacao    = string.Empty;
        EditandoObservacao   = string.Empty;
        EditandoSeguroGerado = false;
        EditandoFechou       = false;
        EditandoSeguroNovoId = null;
        LeadSelecionado      = null;
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            var lista = await _leadService.GetTodosAsync();
            _colecao = new ObservableCollection<Lead>(lista);
            _view    = (ListCollectionView)CollectionViewSource.GetDefaultView(_colecao);
            _view.Filter = FiltroItem;
            LeadsView = _view;
            LimparFormulario();
        }
        finally { IsLoading = false; }
    }

    private bool FiltroItem(object obj)
    {
        if (obj is not Lead l) return false;
        if (string.IsNullOrWhiteSpace(FiltroTexto)) return true;
        var txt = FiltroTexto.Trim().ToLowerInvariant();
        return (l.Segurado?.ToLowerInvariant().Contains(txt)  == true) ||
               (l.Produtor?.ToLowerInvariant().Contains(txt)  == true) ||
               (l.Indicacao?.ToLowerInvariant().Contains(txt) == true);
    }

    [RelayCommand]
    private void Novo() => LimparFormulario();

    [RelayCommand]
    private async Task SalvarAsync()
    {
        if (string.IsNullOrWhiteSpace(EditandoSegurado))
        {
            MessageBox.Show("Informe o nome do segurado/prospect.", "Campo obrigatório",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        try
        {
            bool fecharAgora = EditandoFechou && !EditandoSeguroNovoId.HasValue;

            var entidade = new Lead
            {
                Id           = EditandoId,
                Segurado     = EditandoSegurado.Trim(),
                Produtor     = EditandoProdutor.Trim(),
                CriadoEm     = EditandoCriadoEm,
                Indicacao    = string.IsNullOrWhiteSpace(EditandoIndicacao)  ? null : EditandoIndicacao.Trim(),
                Observacao   = string.IsNullOrWhiteSpace(EditandoObservacao) ? null : EditandoObservacao.Trim(),
                SeguroGerado = EditandoSeguroGerado,
                Fechou       = EditandoFechou,
                FechouEm     = EditandoFechou ? DateTime.Now : null,
                SeguroNovoId = EditandoSeguroNovoId
            };

            if (fecharAgora)
            {
                var obs = string.IsNullOrWhiteSpace(entidade.Observacao)
                    ? string.Empty
                    : $"Lead: {entidade.Observacao}";

                var seguroNovo = new SeguroNovo
                {
                    Segurado   = entidade.Segurado,
                    CriadoEm   = DateTime.Now,
                    CriadoPor  = entidade.Produtor,
                    Status     = "Novo",
                    Observacao = obs
                };
                var salvoSN           = await _seguroNovoService.SalvarAsync(seguroNovo);
                entidade.SeguroNovoId = salvoSN.Id;
                EditandoSeguroNovoId  = salvoSN.Id;

                WeakReferenceMessenger.Default.Send(
                    new DashboardRefreshMessage(DateTime.Now.Month, DateTime.Now.Year));
            }

            var salvo = await _leadService.SalvarAsync(entidade);

            var existente = _colecao.FirstOrDefault(l => l.Id == salvo.Id);
            if (existente != null)
                _colecao[_colecao.IndexOf(existente)] = salvo;
            else
                _colecao.Insert(0, salvo);

            LeadSelecionado = salvo;
            PopularFormulario(salvo);

            if (fecharAgora)
                MessageBox.Show(
                    "Lead fechado! Seguro Novo criado com sucesso.\nO registro já aparece na tela de Seguros Novos.",
                    "Lead fechado", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao salvar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private async Task ExcluirAsync()
    {
        if (EditandoId == 0) return;

        var r = MessageBox.Show(
            $"Excluir o lead de \"{EditandoSegurado}\"?",
            "Confirmar exclusão", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (r != MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            await _leadService.ExcluirAsync(EditandoId);
            var existente = _colecao.FirstOrDefault(l => l.Id == EditandoId);
            if (existente != null) _colecao.Remove(existente);
            LimparFormulario();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao excluir", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand]
    private void Cancelar() => LimparFormulario();
}
