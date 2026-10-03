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
        DataContextChanged += (_, e) =>
        {
            if (e.OldValue is GerenciadorRenovacoesViewModel antigo)
                antigo.PropertyChanged -= Vm_PropertyChanged;
            if (e.NewValue is GerenciadorRenovacoesViewModel novo)
            {
                novo.PropertyChanged += Vm_PropertyChanged;
                AtualizarColunaDiaSemana(novo);
            }
        };
    }

    private void Vm_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GerenciadorRenovacoesViewModel.AgrupamentoSelecionado)
            && sender is GerenciadorRenovacoesViewModel vm)
            AtualizarColunaDiaSemana(vm);
    }

    // Quando agrupado por data de vencimento o dia da semana vai no título do grupo,
    // então a coluna do grid fica oculta.
    private void AtualizarColunaDiaSemana(GerenciadorRenovacoesViewModel vm)
    {
        var coluna = MainGrid.Columns.FirstOrDefault(c => c.Header as string == "Dia da Semana");
        if (coluna != null)
            coluna.Visibility = vm.AgrupamentoSelecionado == "Data de vencimento"
                ? System.Windows.Visibility.Collapsed
                : System.Windows.Visibility.Visible;
    }

    // Atualiza o resumo quando o usuário marca/desmarca um checkbox
    private void CheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is GerenciadorRenovacoesViewModel vm)
            vm.NotificarMarcacao();
    }

    // Seleciona a linha sob o cursor antes de abrir o menu de contexto
    private void DataGridRow_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (sender is DataGridRow row)
            row.IsSelected = true;
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
