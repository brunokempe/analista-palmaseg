using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace AnalistaPalmaseg.App.Controls;

/// <summary>
/// Rola o DataGrid até o item selecionado sempre que a seleção muda (inclusive quando ela é
/// definida pelo ViewModel antes da tela ser exibida, ex.: ao abrir um registro de outra tela).
/// </summary>
public static class DataGridScroll
{
    public static readonly DependencyProperty AutoScrollToSelectedProperty =
        DependencyProperty.RegisterAttached(
            "AutoScrollToSelected", typeof(bool), typeof(DataGridScroll),
            new PropertyMetadata(false, OnAutoScrollToSelectedChanged));

    public static bool GetAutoScrollToSelected(DependencyObject obj) => (bool)obj.GetValue(AutoScrollToSelectedProperty);
    public static void SetAutoScrollToSelected(DependencyObject obj, bool value) => obj.SetValue(AutoScrollToSelectedProperty, value);

    private static void OnAutoScrollToSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid) return;
        if ((bool)e.NewValue)
        {
            grid.SelectionChanged += Grid_SelectionChanged;
            grid.Loaded += Grid_Loaded;
        }
        else
        {
            grid.SelectionChanged -= Grid_SelectionChanged;
            grid.Loaded -= Grid_Loaded;
        }
    }

    private static void Grid_SelectionChanged(object sender, SelectionChangedEventArgs e) => Rolar((DataGrid)sender);
    private static void Grid_Loaded(object sender, RoutedEventArgs e) => Rolar((DataGrid)sender);

    private static void Rolar(DataGrid grid)
    {
        // Adiado até o layout terminar — o grid pode ainda estar sendo carregado/reagrupado.
        grid.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
        {
            var item = grid.SelectedItem;
            if (item == null || !grid.IsLoaded) return;
            grid.UpdateLayout();
            grid.ScrollIntoView(item);
        }));
    }
}
