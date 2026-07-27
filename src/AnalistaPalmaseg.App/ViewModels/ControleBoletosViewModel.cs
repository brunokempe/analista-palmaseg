using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public enum TipoOrigem { SeguroNovo, RenPalma }

public partial class ItemControleBoleto : ObservableObject
{
    public int Id { get; init; }
    public TipoOrigem Tipo { get; init; }
    public string Segurado { get; init; } = string.Empty;
    public string Seguradora { get; init; } = string.Empty;
    public DateTime? Vigencia { get; init; }
    public string Produtor { get; init; } = string.Empty;
    public int Parcelas { get; init; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Progresso), nameof(Completo))]
    private int _boletosGerados;

    public string Progresso => $"{BoletosGerados}/{Parcelas}";
    public bool Completo => Parcelas > 0 && BoletosGerados >= Parcelas;
    public string TipoLabel => Tipo == TipoOrigem.SeguroNovo ? "NOVO" : "RENOV.";
}

public partial class ControleBoletosViewModel : ObservableObject
{
    private readonly SeguroNovoService _seguroNovoService;
    private readonly RelatorioRenovacaoService _renovacaoService;

    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private bool _apenasPendentes;

    public ObservableCollection<ItemControleBoleto> Itens { get; } = [];
    public ObservableCollection<ItemControleBoleto> ItensFiltrados { get; } = [];

    public ControleBoletosViewModel(
        SeguroNovoService seguroNovoService,
        RelatorioRenovacaoService renovacaoService)
    {
        _seguroNovoService = seguroNovoService;
        _renovacaoService = renovacaoService;
    }

    [RelayCommand]
    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            Itens.Clear();

            var segurosNovos = await _seguroNovoService.GetTodosAsync();
            foreach (var s in segurosNovos)
            {
                if (!s.FormaPagamento.Contains("boleto", StringComparison.OrdinalIgnoreCase)) continue;
                var parcelas = s.Parcelas ?? 1;
                if (parcelas < 1) parcelas = 1;
                Itens.Add(new ItemControleBoleto
                {
                    Id         = s.Id,
                    Tipo       = TipoOrigem.SeguroNovo,
                    Segurado   = s.Segurado,
                    Seguradora = s.Cia,
                    Vigencia   = s.Vigencia,
                    Produtor   = s.CriadoPor ?? string.Empty,
                    Parcelas   = parcelas,
                    BoletosGerados = s.BoletosGerados,
                });
            }

            var renovacoes = await _renovacaoService.GetRenPalmaAsync();
            foreach (var r in renovacoes)
            {
                var forma = r.FechamentoFormaPagamento ?? string.Empty;
                if (!forma.Contains("boleto", StringComparison.OrdinalIgnoreCase)) continue;
                var parcelas = ParseParcelas(r.FechamentoParcelamento) ?? r.NumeroParcelas;
                if (parcelas < 1) parcelas = 1;
                Itens.Add(new ItemControleBoleto
                {
                    Id         = r.Id,
                    Tipo       = TipoOrigem.RenPalma,
                    Segurado   = r.NomeCliente ?? string.Empty,
                    Seguradora = r.FechamentoSeguradora ?? r.Seguradora ?? string.Empty,
                    Vigencia   = r.VigenciaFinal,
                    Produtor   = r.NovoProdutor ?? string.Empty,
                    Parcelas   = parcelas,
                    BoletosGerados = r.BoletosGerados,
                });
            }

            AplicarFiltro();
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnApenasPendentesChanged(bool _) => AplicarFiltro();

    private void AplicarFiltro()
    {
        ItensFiltrados.Clear();
        var fonte = ApenasPendentes ? Itens.Where(i => !i.Completo) : Itens.AsEnumerable();
        foreach (var item in fonte.OrderBy(i => i.Vigencia))
            ItensFiltrados.Add(item);
    }

    [RelayCommand]
    private async Task IncrementarBoleto(ItemControleBoleto? item)
    {
        if (item == null || item.BoletosGerados >= item.Parcelas) return;
        item.BoletosGerados++;
        await SalvarAsync(item);
        if (ApenasPendentes && item.Completo)
            ItensFiltrados.Remove(item);
    }

    [RelayCommand]
    private async Task DecrementarBoleto(ItemControleBoleto? item)
    {
        if (item == null || item.BoletosGerados <= 0) return;
        item.BoletosGerados--;
        await SalvarAsync(item);
    }

    private async Task SalvarAsync(ItemControleBoleto item)
    {
        try
        {
            if (item.Tipo == TipoOrigem.SeguroNovo)
                await _seguroNovoService.SalvarBoletosGeradosAsync(item.Id, item.BoletosGerados);
            else
                await _renovacaoService.SalvarBoletosGeradosAsync(item.Id, item.BoletosGerados);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static int? ParseParcelas(string? texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        var digits = new string(texto.Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(digits)) return 1; // "À Vista", "avista", etc. → 1 boleto
        return int.TryParse(digits, out var n) && n > 0 ? n : 1;
    }
}
