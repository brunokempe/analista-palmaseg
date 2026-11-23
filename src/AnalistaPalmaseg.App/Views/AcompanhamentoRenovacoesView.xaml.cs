using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using AnalistaPalmaseg.App.ViewModels;
using AnalistaPalmaseg.Core.Models;

namespace AnalistaPalmaseg.App.Views;

public partial class AcompanhamentoRenovacoesView : UserControl
{
    private const string ChaveColunas = "acompanhamento-renovacoes";
    private readonly DispatcherTimer _salvarColunasTimer;

    public AcompanhamentoRenovacoesView()
    {
        InitializeComponent();
        ColunasGridStore.Restaurar(MainGrid, ChaveColunas);

        // Qualquer mudança de largura de coluna agenda a gravação. O atraso junta o arraste inteiro
        // (e o reset de largura feito ao trocar de mês) em uma única gravação, já com o valor final.
        _salvarColunasTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
        _salvarColunasTimer.Tick += (_, _) =>
        {
            _salvarColunasTimer.Stop();
            ColunasGridStore.Salvar(MainGrid, ChaveColunas);
        };
        var larguraProp = DependencyPropertyDescriptor.FromProperty(DataGridColumn.WidthProperty, typeof(DataGridColumn));
        foreach (var coluna in MainGrid.Columns)
            larguraProp.AddValueChanged(coluna, (_, _) =>
            {
                _salvarColunasTimer.Stop();
                _salvarColunasTimer.Start();
            });

        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is AcompanhamentoRenovacoesViewModel oldVm)
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        if (e.NewValue is AcompanhamentoRenovacoesViewModel newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
            AtualizarColunaDia(newVm);
        }
    }

    // Quando agrupado por data de vencimento o dia da semana vai no título do grupo,
    // então a coluna "Dia" do grid fica oculta.
    private void AtualizarColunaDia(AcompanhamentoRenovacoesViewModel vm)
    {
        var coluna = MainGrid.Columns.FirstOrDefault(c => c.Header as string == "Dia");
        if (coluna != null)
            coluna.Visibility = vm.AgrupamentoSelecionado == "Data de vencimento"
                ? Visibility.Collapsed
                : Visibility.Visible;
    }

    // Trocar de aba (mês) ou de agrupamento reaplica o filtro/grupos na mesma
    // ListCollectionView; o WPF não redistribui corretamente a largura das colunas
    // (sobretudo a coluna "*") depois desse refresh, deixando-as encolhidas até o
    // usuário redimensionar alguma manualmente. Forçar a largura de cada coluna a
    // ser reatribuída faz o DataGrid recalcular a distribuição corretamente.
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AcompanhamentoRenovacoesViewModel.MesSelecionado) &&
            e.PropertyName != nameof(AcompanhamentoRenovacoesViewModel.AgrupamentoSelecionado))
            return;

        if (sender is AcompanhamentoRenovacoesViewModel vm)
            AtualizarColunaDia(vm);

        Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() =>
        {
            foreach (var column in MainGrid.Columns)
            {
                var width = column.Width;
                column.Width = 0;
                column.Width = width;
            }
        }));
    }

    private void ClienteCell_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2) return;
        if (DataContext is not AcompanhamentoRenovacoesViewModel vm) return;
        if (sender is FrameworkElement { DataContext: RelatorioRenovacao item })
            vm.RegistroSelecionado = item;
        if (vm.AbrirCadastroClienteCommand.CanExecute(null))
            vm.AbrirCadastroClienteCommand.Execute(null);
        e.Handled = true;
    }

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
