using System.Windows.Input;

namespace AnalistaPalmaseg.App.Views;

public partial class SenhaDialog : System.Windows.Window
{
    public string Senha { get; private set; } = string.Empty;

    public SenhaDialog() => InitializeComponent();

    private void OK_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        Senha = PasswordBox.Password;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) OK_Click(sender, e);
    }
}
