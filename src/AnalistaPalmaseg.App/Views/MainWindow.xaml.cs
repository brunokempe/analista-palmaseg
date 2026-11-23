using System.ComponentModel;
using System.Windows;
using AnalistaPalmaseg.App.ViewModels;

namespace AnalistaPalmaseg.App.Views;

public partial class MainWindow : System.Windows.Window
{
    private bool _sessaoEncerrando;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Logoff/desligamento do Windows não pode ser bloqueado por uma pergunta.
        Application.Current.SessionEnding += (_, _) => _sessaoEncerrando = true;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (e.Cancel || _sessaoEncerrando) return;

        if (!ConfirmacaoDialog.Perguntar(this, "Finalizar sistema", "Deseja finalizar o sistema?"))
            e.Cancel = true;
    }
}
