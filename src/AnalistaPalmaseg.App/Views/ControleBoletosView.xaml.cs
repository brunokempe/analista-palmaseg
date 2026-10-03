using System.Windows;
using System.Windows.Controls;
using AnalistaPalmaseg.App.ViewModels;

namespace AnalistaPalmaseg.App.Views;

public partial class ControleBoletosView : UserControl
{
    public ControleBoletosView()
    {
        InitializeComponent();
    }

    private void Observacao_LostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ItemControleBoleto item } &&
            DataContext is ControleBoletosViewModel vm)
            vm.SalvarObservacaoCommand.Execute(item);
    }
}
