using System.Windows;
using System.Windows.Controls;
using AnalistaPalmaseg.App.ViewModels;
using AnalistaPalmaseg.Core.Models;

namespace AnalistaPalmaseg.App.Views;

public partial class AtualizarSeguroNovoDialog : Window
{
    private readonly SeguroNovo _reg;

    public AtualizarSeguroNovoDialog(SeguroNovo reg, IEnumerable<string> produtoresDisponiveis)
    {
        InitializeComponent();
        _reg = reg;

        SeguradoText.Text = string.Join("  ·  ", new[]
        {
            reg.Segurado,
            reg.Cia,
            reg.Vigencia.HasValue ? $"Venc. {reg.Vigencia:dd/MM/yyyy}" : null
        }.Where(s => !string.IsNullOrEmpty(s)));

        // Listas de cadastro (mesmas usadas na tela de Seguros Novos)
        ProdutorCombo.ItemsSource       = produtoresDisponiveis;
        CiaCombo.ItemsSource            = SeguroNovosViewModel.Seguradoras;
        SegmentoCombo.ItemsSource       = SeguroNovosViewModel.Segmentos;
        StatusCombo.ItemsSource         = SeguroNovosViewModel.StatusOpcoes;
        FormaPagamentoCombo.ItemsSource = SeguroNovosViewModel.FormasPagamento;
        ParcelasCombo.ItemsSource       = SeguroNovosViewModel.Parcelamentos;

        // Pré-preenche com todos os dados existentes do registro
        ProdutorCombo.Text        = reg.CriadoPor ?? string.Empty;
        VigenciaPicker.SelectedDate = reg.Vigencia;
        SeguradoTextBox.Text      = reg.Segurado;
        CiaCombo.Text             = reg.Cia;
        SegmentoCombo.SelectedItem = reg.Segmento;
        StatusCombo.SelectedItem  = reg.Status;
        FinanceiroTextBox.Text    = reg.Financeiro;
        ValorTextBox.Text         = reg.Valor?.ToString("N2") ?? string.Empty;
        FatorTextBox.Text         = reg.Fator?.ToString("N2") ?? string.Empty;
        FormaPagamentoCombo.Text  = reg.FormaPagamento;
        ParcelasCombo.SelectedItem = reg.Parcelas;
        AssinaturaToggle.IsChecked = reg.AssinaturaFeita;
        ObservacaoTextBox.Text    = reg.Observacao;
    }

    private void Confirmar_Click(object sender, RoutedEventArgs e)
    {
        var cia = CiaCombo.Text?.Trim();
        if (string.IsNullOrWhiteSpace(cia))
        {
            MessageBox.Show("Informe a seguradora (Cia) para confirmar.", "Campo obrigatório",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            CiaCombo.Focus();
            return;
        }

        _reg.CriadoPor  = string.IsNullOrWhiteSpace(ProdutorCombo.Text) ? _reg.CriadoPor : ProdutorCombo.Text.Trim();
        _reg.Vigencia   = VigenciaPicker.SelectedDate;
        _reg.Segurado   = SeguradoTextBox.Text?.Trim() ?? string.Empty;
        _reg.Cia        = cia;
        _reg.Segmento   = SegmentoCombo.SelectedItem as string ?? string.Empty;
        _reg.Status     = StatusCombo.SelectedItem as string ?? string.Empty;
        _reg.Financeiro = FinanceiroTextBox.Text?.Trim() ?? string.Empty;
        _reg.Valor      = ParseDecimal(ValorTextBox.Text);
        _reg.Fator      = ParseDecimal(FatorTextBox.Text);
        _reg.FormaPagamento  = FormaPagamentoCombo.Text?.Trim() ?? string.Empty;
        _reg.Parcelas   = ParcelasCombo.SelectedItem as int?;
        _reg.AssinaturaFeita = AssinaturaToggle.IsChecked == true;
        _reg.Observacao = ObservacaoTextBox.Text?.Trim() ?? string.Empty;

        DialogResult = true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private static decimal? ParseDecimal(string texto)
    {
        if (string.IsNullOrWhiteSpace(texto)) return null;
        if (decimal.TryParse(texto, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.GetCultureInfo("pt-BR"), out var v))
            return v;
        if (decimal.TryParse(texto, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v2))
            return v2;
        return null;
    }
}
