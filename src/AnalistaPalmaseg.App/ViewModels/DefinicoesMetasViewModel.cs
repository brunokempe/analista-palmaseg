using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

// VM auxiliar para edição das metas por seguradora na grade
public partial class MetaSeguradoraItemVm : ObservableObject
{
    public int SeguradoraId { get; init; }
    public string Nome { get; init; } = string.Empty;
    public bool IsParceira { get; init; }
    public bool Ativo { get; init; }

    [ObservableProperty] private decimal _metaPremio;
}

public partial class DefinicoesMetasViewModel : ObservableObject
{
    private readonly MetaService _metaService;

    [ObservableProperty] private bool _isLoading;

    // ── Tab Seguradoras ───────────────────────────────────────────────────────
    public ObservableCollection<MetaSeguradoraItemVm> Seguradoras { get; } = [];

    [ObservableProperty] private MetaSeguradoraItemVm? _seguradoraSelecionada;
    [ObservableProperty] private int     _editId;
    [ObservableProperty] private string  _editNome      = string.Empty;
    [ObservableProperty] private bool    _editIsParceira;
    [ObservableProperty] private bool    _editAtivo     = true;
    [ObservableProperty] private decimal _editMetaPremio;
    [ObservableProperty] private int     _segMes = DateTime.Now.Month;
    [ObservableProperty] private int     _segAno = DateTime.Now.Year;

    private bool TemSeguradoraSelecionada => SeguradoraSelecionada != null;

    partial void OnSeguradoraSelecionadaChanged(MetaSeguradoraItemVm? value)
    {
        ExcluirSeguradoraCommand.NotifyCanExecuteChanged();
        if (value == null) return;
        EditId         = value.SeguradoraId;
        EditNome       = value.Nome;
        EditIsParceira = value.IsParceira;
        EditAtivo      = value.Ativo;
        EditMetaPremio = value.MetaPremio;
    }

    partial void OnSegMesChanged(int value) => _ = CarregarSeguradorasAsync();
    partial void OnSegAnoChanged(int value) => _ = CarregarSeguradorasAsync();

    public static int[] Meses { get; } = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12];
    public static int[] Anos  { get; } = Enumerable.Range(DateTime.Now.Year - 2, 6).ToArray();

    // ── Tab Premiação ─────────────────────────────────────────────────────────
    public ObservableCollection<MetaPremiacao>    Premiacoes   { get; } = [];
    public ObservableCollection<MetaCrescimento>  Crescimentos { get; } = [];

    [ObservableProperty] private MetaPremiacao? _premiacaoSelecionada;

    private bool TemPremiacaoSelecionada => PremiacaoSelecionada != null;

    partial void OnPremiacaoSelecionadaChanged(MetaPremiacao? value) =>
        RemoverPremiacaoCommand.NotifyCanExecuteChanged();

    public DefinicoesMetasViewModel(MetaService metaService)
    {
        _metaService = metaService;
    }

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            await CarregarSeguradorasAsync();
            await CarregarPremiacaoAsync();
        }
        finally { IsLoading = false; }
    }

    // ── Seguradoras ───────────────────────────────────────────────────────────

    private async Task CarregarSeguradorasAsync()
    {
        var lista       = await _metaService.GetSeguradorasAsync();
        var metasSalvas = await _metaService.GetMetasAsync(SegMes, SegAno);
        var mapaMetasSalvas = metasSalvas.ToDictionary(m => m.SeguradoraId, m => m.MetaPremio);

        Seguradoras.Clear();
        foreach (var s in lista)
        {
            Seguradoras.Add(new MetaSeguradoraItemVm
            {
                SeguradoraId = s.Id,
                Nome         = s.Nome,
                IsParceira   = s.IsParceira,
                Ativo        = s.Ativo,
                MetaPremio   = mapaMetasSalvas.TryGetValue(s.Id, out var v) ? v : 0m
            });
        }
        LimparFormularioSeguradora();
    }

    private void LimparFormularioSeguradora()
    {
        EditId         = 0;
        EditNome       = string.Empty;
        EditIsParceira = false;
        EditAtivo      = true;
        EditMetaPremio = 0m;
        SeguradoraSelecionada = null;
    }

    [RelayCommand]
    private void NovaSeguradora() => LimparFormularioSeguradora();

    [RelayCommand]
    private async Task SalvarSeguradoraAsync()
    {
        if (string.IsNullOrWhiteSpace(EditNome))
        {
            MessageBox.Show("Informe o nome da seguradora.", "Campo obrigatório",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        try
        {
            var entidade = new Seguradora
            {
                Id         = EditId,
                Nome       = EditNome.Trim(),
                IsParceira = EditIsParceira,
                Ativo      = EditAtivo
            };

            var salva = await _metaService.SalvarSeguradoraAsync(entidade);

            await _metaService.SalvarMetasAsync([new MetaSeguradora
            {
                SeguradoraId = salva.Id,
                Mes          = SegMes,
                Ano          = SegAno,
                MetaPremio   = EditMetaPremio
            }]);

            var novoItem = new MetaSeguradoraItemVm
            {
                SeguradoraId = salva.Id,
                Nome         = salva.Nome,
                IsParceira   = salva.IsParceira,
                Ativo        = salva.Ativo,
                MetaPremio   = EditMetaPremio
            };

            var existente = Seguradoras.FirstOrDefault(s => s.SeguradoraId == salva.Id);
            if (existente != null)
            {
                var idx = Seguradoras.IndexOf(existente);
                Seguradoras[idx] = novoItem;
            }
            else
            {
                Seguradoras.Add(novoItem);
            }

            SeguradoraSelecionada = Seguradoras.FirstOrDefault(s => s.SeguradoraId == salva.Id);
            EditId = salva.Id;
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao salvar", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsLoading = false; }
    }

    [RelayCommand(CanExecute = nameof(TemSeguradoraSelecionada))]
    private async Task ExcluirSeguradoraAsync()
    {
        if (SeguradoraSelecionada == null) return;

        var conf = MessageBox.Show(
            $"Excluir a seguradora \"{SeguradoraSelecionada.Nome}\"?\nIsso também excluirá todas as metas vinculadas.",
            "Confirmar exclusão", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (conf != MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            await _metaService.ExcluirSeguradoraAsync(SeguradoraSelecionada.SeguradoraId);
            Seguradoras.Remove(SeguradoraSelecionada);
            LimparFormularioSeguradora();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao excluir", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsLoading = false; }
    }

    // ── Premiação ─────────────────────────────────────────────────────────────

    private async Task CarregarPremiacaoAsync()
    {
        var premiacoes   = await _metaService.GetPremiacaoAsync();
        var crescimentos = await _metaService.GetCrescimentoAsync();

        Premiacoes.Clear();
        foreach (var p in premiacoes) Premiacoes.Add(p);

        Crescimentos.Clear();
        foreach (var c in crescimentos) Crescimentos.Add(c);
    }

    [RelayCommand]
    private void AdicionarPremiacao()
    {
        var nova = new MetaPremiacao
        {
            QuantidadeMinima = 1,
            EhTodas          = false,
            ValorBonus       = 0m,
            Ordem            = Premiacoes.Count + 1
        };
        Premiacoes.Add(nova);
        PremiacaoSelecionada = nova;
    }

    [RelayCommand(CanExecute = nameof(TemPremiacaoSelecionada))]
    private void RemoverPremiacao()
    {
        if (PremiacaoSelecionada == null) return;
        Premiacoes.Remove(PremiacaoSelecionada);
        PremiacaoSelecionada = null;
    }

    [RelayCommand]
    private async Task SalvarPremiacaoAsync()
    {
        IsLoading = true;
        try
        {
            await _metaService.SalvarPremiacaoAsync([.. Premiacoes]);
            await _metaService.SalvarCrescimentoAsync([.. Crescimentos]);
            MessageBox.Show("Premiação salva com sucesso!", "Sucesso",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Erro ao salvar premiação", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally { IsLoading = false; }
    }
}
