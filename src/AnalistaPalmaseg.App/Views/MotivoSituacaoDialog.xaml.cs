using System.Windows;
using System.Windows.Input;

namespace AnalistaPalmaseg.App.Views;

public partial class MotivoSituacaoDialog : Window
{
    public string Motivo { get; private set; } = string.Empty;

    public MotivoSituacaoDialog(string novaSituacao)
    {
        InitializeComponent();
        PromptText.Text = $"Situação alterada para \"{novaSituacao}\".\nInforme o motivo:";
        Loaded += (_, _) => MotivoBox.Focus();
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(MotivoBox.Text))
        {
            ErroText.Visibility = Visibility.Visible;
            MotivoBox.Focus();
            return;
        }

        Motivo = MotivoBox.Text.Trim();
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void MotivoBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) OK_Click(sender, e);
    }
}
