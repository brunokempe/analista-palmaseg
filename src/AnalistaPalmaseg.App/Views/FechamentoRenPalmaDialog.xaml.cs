using System.Diagnostics;
using System.IO;
using System.Windows;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.Views;

public partial class FechamentoRenPalmaDialog : Window
{
    private readonly RelatorioRenovacao _reg;
    private readonly decimal _aliquotaIof;
    private bool _atualizando;

    private record AnexoItem(string Nome, string Caminho, long TamanhoBytes)
    {
        public string Tamanho => TamanhoBytes < 1024 * 1024
            ? $"{TamanhoBytes / 1024:N0} KB"
            : $"{TamanhoBytes / (1024.0 * 1024):N1} MB";
    }

    public FechamentoRenPalmaDialog(RelatorioRenovacao reg, List<Anexo> anexos)
    {
        InitializeComponent();
        _reg = reg;

        ClienteText.Text = string.Join("  ·  ", new[]
        {
            reg.NomeCliente,
            reg.Seguradora,
            reg.VigenciaFinal.HasValue ? $"Venc. {reg.VigenciaFinal:dd/MM/yyyy}" : null
        }.Where(s => !string.IsNullOrEmpty(s)));

        // Pré-preenche com dados existentes
        SeguradoraCombo.Text = reg.FechamentoSeguradora ?? string.Empty;
        ObservacaoTextBox.Text = reg.FechamentoObservacao ?? string.Empty;
        _aliquotaIof = IofHelper.ObterAliquota(reg.Ramo);
        PremioTotalLabel.Text = $"Prêmio Total c/ IOF {_aliquotaIof:0.##}% (R$)";
        PremioTextBox.Text   = reg.FechamentoPremioLiquido?.ToString("N2") ?? string.Empty;
        AtualizarPremioTotal();
        ComissaoTextBox.Text = reg.FechamentoComissao?.ToString("N2") ?? string.Empty;

        SelecionarComboBoxItem(FormaPagamentoCombo, reg.FechamentoFormaPagamento);
        SelecionarComboBoxItem(ParcelamentoCombo, reg.FechamentoParcelamento);

        var assinatura = reg.FechamentoAssinatura ?? string.Empty;
        ChkDigital.IsChecked = assinatura.Contains("Digital");
        ChkFisica.IsChecked  = assinatura.Contains("Física");
        ChkOutros.IsChecked  = assinatura.Contains("Outros");

        // Lista de anexos
        if (anexos.Count == 0)
        {
            AnexosTitulo.Text        = "Cotações / Anexos (nenhum)";
            SemAnexos.Visibility     = Visibility.Visible;
        }
        else
        {
            AnexosTitulo.Text    = $"Cotações / Anexos ({anexos.Count})";
            AnexosList.ItemsSource = anexos
                .Select(a => new AnexoItem(a.NomeArquivo, a.CaminhoArquivo, a.TamanhoBytes))
                .ToList();
        }
    }

    // Máscara de valor: só dígitos, preenchidos da direita (centavos) → 1.234,56
    private void Valor_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e) =>
        e.Handled = e.Text.Any(c => !char.IsDigit(c));

    /// <summary>Reformata o texto como valor monetário. Retorna true se alterou (o TextChanged será disparado de novo).</summary>
    private static bool AplicarMascara(System.Windows.Controls.TextBox tb)
    {
        var digitos = new string(tb.Text.Where(char.IsDigit).ToArray()).TrimStart('0');
        var novo = digitos.Length == 0
            ? string.Empty
            : (decimal.Parse(digitos, System.Globalization.CultureInfo.InvariantCulture) / 100m)
                .ToString("N2", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));
        if (novo == tb.Text) return false;
        tb.Text = novo;
        tb.CaretIndex = novo.Length;
        return true;
    }

    private void PremioTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (AplicarMascara(PremioTextBox)) return;
        if (_atualizando) return;
        AtualizarPremioTotal();
    }

    // PL zerado: PT editável e calcula o PL automaticamente.
    private void PremioTotalTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (!PremioTotalTextBox.IsReadOnly && AplicarMascara(PremioTotalTextBox)) return;
        if (_atualizando || PremioTotalTextBox.IsReadOnly) return;
        var pt = ParseDecimal(PremioTotalTextBox.Text);
        _atualizando = true;
        PremioTextBox.Text = pt is > 0 ? IofHelper.PtParaPl(pt.Value, _aliquotaIof).ToString("N2") : string.Empty;
        _atualizando = false;
    }

    private void AtualizarPremioTotal()
    {
        var pl = ParseDecimal(PremioTextBox.Text);
        _atualizando = true;
        if (pl is > 0)
        {
            PremioTotalTextBox.IsReadOnly = true;
            PremioTotalTextBox.Text = IofHelper.PlParaPt(pl.Value, _aliquotaIof).ToString("N2");
        }
        else
        {
            PremioTotalTextBox.IsReadOnly = false;
            PremioTotalTextBox.Text = string.Empty;
        }
        _atualizando = false;
    }

    private static void SelecionarComboBoxItem(System.Windows.Controls.ComboBox combo, string? valor)
    {
        if (string.IsNullOrEmpty(valor)) return;
        foreach (System.Windows.Controls.ComboBoxItem item in combo.Items)
        {
            if (item.Content?.ToString() == valor)
            {
                combo.SelectedItem = item;
                return;
            }
        }
    }

    private void AbrirAnexo_Click(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn &&
            btn.Tag is string caminho && File.Exists(caminho))
        {
            Process.Start(new ProcessStartInfo(caminho) { UseShellExecute = true });
        }
    }

    private void Confirmar_Click(object sender, RoutedEventArgs e)
    {
        var seguradora = SeguradoraCombo.Text?.Trim();
        if (string.IsNullOrWhiteSpace(seguradora))
        {
            MessageBox.Show("Informe a seguradora para confirmar o fechamento.", "Campo obrigatório",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            SeguradoraCombo.Focus();
            return;
        }

        _reg.FechamentoSeguradora    = seguradora;
        _reg.FechamentoObservacao    = string.IsNullOrWhiteSpace(ObservacaoTextBox.Text) ? null : ObservacaoTextBox.Text.Trim();
        _reg.FechamentoPremioLiquido = ParseDecimal(PremioTextBox.Text);
        _reg.FechamentoPremioTotal   = ParseDecimal(PremioTotalTextBox.Text);
        _reg.FechamentoComissao      = ParseDecimal(ComissaoTextBox.Text);

        _reg.FechamentoFormaPagamento =
            (FormaPagamentoCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();

        _reg.FechamentoParcelamento =
            (ParcelamentoCombo.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString();

        var selecionados = new List<string>();
        if (ChkDigital.IsChecked == true) selecionados.Add("Digital");
        if (ChkFisica.IsChecked  == true) selecionados.Add("Física");
        if (ChkOutros.IsChecked  == true) selecionados.Add("Outros");
        _reg.FechamentoAssinatura = selecionados.Count > 0 ? string.Join(", ", selecionados) : null;

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
