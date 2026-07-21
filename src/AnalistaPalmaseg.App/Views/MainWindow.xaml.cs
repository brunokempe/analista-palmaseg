using AnalistaPalmaseg.App.ViewModels;

namespace AnalistaPalmaseg.App.Views;

public partial class MainWindow : System.Windows.Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
