using System.Windows;
using System.Windows.Controls;
using AnalistaPalmaseg.App.ViewModels;

namespace AnalistaPalmaseg.App.Views;

public partial class SalvarPropostasView : UserControl
{
    public SalvarPropostasView()
    {
        InitializeComponent();
    }

    private void DropZone_DragEnter(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void DropZone_DragLeave(object sender, DragEventArgs e)
    {
        e.Handled = true;
    }

    private async void DropZone_Drop(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        if (DataContext is not SalvarPropostasViewModel vm) return;

        var arquivos = (string[])e.Data.GetData(DataFormats.FileDrop);
        await vm.SalvarArquivosAsync(arquivos);
    }
}
