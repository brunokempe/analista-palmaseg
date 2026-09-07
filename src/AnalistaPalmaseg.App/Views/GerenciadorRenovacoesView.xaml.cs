using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using AnalistaPalmaseg.App.ViewModels;
using AnalistaPalmaseg.Core.Models;

namespace AnalistaPalmaseg.App.Views;

public partial class GerenciadorRenovacoesView : UserControl
{
    public GerenciadorRenovacoesView()
    {
        InitializeComponent();
    }

    // Atualiza o resumo quando o usuário marca/desmarca um checkbox
    private void CheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is GerenciadorRenovacoesViewModel vm)
            vm.NotificarMarcacao();
    }

    // Persiste edição de NovoProdutor / Observacao ao sair da linha
    private void MainGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
    {
        if (e.EditAction != DataGridEditAction.Commit) return;
        if (e.Row.Item is not RelatorioRenovacao reg) return;
        if (DataContext is not GerenciadorRenovacoesViewModel vm) return;

        Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
            new Action(() => vm.SalvarEdicaoCommand.Execute(reg)));
    }

    private void MainGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (DataContext is not GerenciadorRenovacoesViewModel vm) return;

        RelatorioRenovacaoSortHelper.HandleSorting(
            "Gerenciador de Renovações",
            e, MainGrid,
            vm.RegistrosView as System.Windows.Data.ListCollectionView,
            r => r.DocumentoPrincipal ?? string.Empty);
    }
}
