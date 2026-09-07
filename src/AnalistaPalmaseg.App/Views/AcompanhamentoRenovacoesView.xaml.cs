using System.Windows.Controls;
using AnalistaPalmaseg.App.ViewModels;
using AnalistaPalmaseg.Core.Models;

namespace AnalistaPalmaseg.App.Views;

public partial class AcompanhamentoRenovacoesView : UserControl
{
    public AcompanhamentoRenovacoesView() => InitializeComponent();

    private void MainGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (DataContext is not AcompanhamentoRenovacoesViewModel vm) return;

        RelatorioRenovacaoSortHelper.HandleSorting(
            "Acompanhamento de Renovações",
            e, MainGrid,
            vm.RegistrosView as System.Windows.Data.ListCollectionView,
            r => r.NomeCliente ?? string.Empty);
    }
}
