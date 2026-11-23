using System.Windows.Controls;
using System.Windows.Input;
using AnalistaPalmaseg.App.ViewModels;

namespace AnalistaPalmaseg.App.Views;

public partial class ClientesView : UserControl
{
    public ClientesView()
    {
        InitializeComponent();
    }

    private void SeguroRow_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not ClientesViewModel vm) return;
        if (sender is DataGridRow { Item: SeguroRelacionado seguro })
            vm.AbrirSeguroCommand.Execute(seguro);
        e.Handled = true;
    }
}
