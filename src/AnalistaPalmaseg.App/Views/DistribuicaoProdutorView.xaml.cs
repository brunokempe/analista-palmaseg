using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AnalistaPalmaseg.App.Views;

public partial class DistribuicaoProdutorView : UserControl
{
    public DistribuicaoProdutorView() => InitializeComponent();

    private void DecimalField_GotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox tb)
            Dispatcher.BeginInvoke(DispatcherPriority.Input, tb.SelectAll);
    }
}
