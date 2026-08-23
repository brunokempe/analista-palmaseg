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

    // O ListCollectionView usa CustomSort fixo (agrupamento por cliente + vigência), o que
    // por padrão bloqueia o mecanismo automático de ordenação por clique de cabeçalho do
    // DataGrid (SortDescriptions é ignorado quando CustomSort está definido). Aqui tratamos
    // o clique manualmente: como o agrupamento por CPF/CNPJ exige que as linhas do mesmo
    // cliente fiquem contíguas, não dá para ordenar as linhas isoladamente pela coluna
    // clicada — em vez disso ordenamos os GRUPOS pelo menor valor da coluna dentro de cada
    // grupo, e dentro do grupo ordenamos pelas mesmas regras. Sem isso, clicar numa coluna
    // não tinha efeito visível para a maioria dos clientes (que têm só 1 apólice — a
    // ordenação por CPF sempre dominava e a "sub-ordenação" dentro do grupo não aparecia).
    private void MainGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (DataContext is not GerenciadorRenovacoesViewModel vm) return;
        if (vm.RegistrosView is not System.Windows.Data.ListCollectionView listView) return;

        e.Handled = true;

        var column = e.Column;
        var sortPath = column.SortMemberPath;
        if (string.IsNullOrEmpty(sortPath)) return;

        var propriedade = typeof(RelatorioRenovacao).GetProperty(sortPath);
        if (propriedade == null) return;

        var novaDirecao = column.SortDirection != System.ComponentModel.ListSortDirection.Ascending
            ? System.ComponentModel.ListSortDirection.Ascending
            : System.ComponentModel.ListSortDirection.Descending;

        foreach (var col in MainGrid.Columns)
            col.SortDirection = null;
        column.SortDirection = novaDirecao;

        var fatorDirecao = novaDirecao == System.ComponentModel.ListSortDirection.Ascending ? 1 : -1;

        int CompareValores(object? x, object? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return Comparer<object>.Default.Compare(x, y);
        }

        var itens = listView.SourceCollection.Cast<RelatorioRenovacao>().ToList();
        var menorValorPorGrupo = itens
            .GroupBy(r => r.DocumentoPrincipal ?? string.Empty)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => propriedade.GetValue(r))
                      .Aggregate((menor, atual) => CompareValores(atual, menor) < 0 ? atual : menor));

        listView.CustomSort = Comparer<object>.Create((a, b) =>
        {
            var ra = (RelatorioRenovacao)a;
            var rb = (RelatorioRenovacao)b;
            var chaveA = ra.DocumentoPrincipal ?? string.Empty;
            var chaveB = rb.DocumentoPrincipal ?? string.Empty;

            if (chaveA != chaveB)
            {
                var cmpGrupo = fatorDirecao * CompareValores(menorValorPorGrupo[chaveA], menorValorPorGrupo[chaveB]);
                return cmpGrupo != 0 ? cmpGrupo : string.CompareOrdinal(chaveA, chaveB);
            }

            var cmpValor = fatorDirecao * CompareValores(propriedade.GetValue(ra), propriedade.GetValue(rb));
            return cmpValor != 0 ? cmpValor : Nullable.Compare(ra.VigenciaFinal, rb.VigenciaFinal);
        });
    }
}
