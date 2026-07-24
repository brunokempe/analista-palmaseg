using System.Windows;
using System.Windows.Input;
using AnalistaPalmaseg.App.ViewModels;

namespace AnalistaPalmaseg.App.Views;

public partial class LoginWindow : Window
{
    private readonly LoginViewModel _vm;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = viewModel;
        viewModel.LoginSucesso += (_, _) => DialogResult = true;
        Loaded += (_, _) => LoginBox.Focus();
    }

    private void SenhaBox_PasswordChanged(object sender, RoutedEventArgs e)
        => _vm.Senha = SenhaBox.Password;

    private void Input_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && _vm.EntrarCommand.CanExecute(null))
            _vm.EntrarCommand.Execute(null);
    }
}
