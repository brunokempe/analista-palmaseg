using System.Windows.Controls;
using System.Windows.Input;

namespace AnalistaPalmaseg.App.Views;

public partial class EmissaoDashboardView : UserControl
{
    public EmissaoDashboardView()
    {
        InitializeComponent();
    }

    private void DataGridRow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRow row)
            row.IsSelected = true;
    }
}
