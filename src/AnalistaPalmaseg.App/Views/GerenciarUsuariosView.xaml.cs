using System.Windows;
using System.Windows.Controls;
using AnalistaPalmaseg.App.ViewModels;

namespace AnalistaPalmaseg.App.Views;

public partial class GerenciarUsuariosView : UserControl
{
    public GerenciarUsuariosView()
    {
        InitializeComponent();
    }

    private void NovaSenhaBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is GerenciarUsuariosViewModel vm)
            vm.NovaSenha = NovaSenhaBox.Password;
    }
}
