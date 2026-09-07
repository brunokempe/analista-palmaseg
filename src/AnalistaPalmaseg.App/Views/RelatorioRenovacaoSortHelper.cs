using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using AnalistaPalmaseg.Core.Models;

namespace AnalistaPalmaseg.App.Views;

// Compartilhado por GerenciadorRenovacoesView e AcompanhamentoRenovacoesView: ambas usam
// ListCollectionView.CustomSort para manter as apólices do mesmo cliente agrupadas e
// contíguas (exigência do agrupamento por PropertyGroupDescription). Definir CustomSort faz
// o DataGrid ignorar SortDescriptions — o mecanismo automático de ordenação por clique no
// cabeçalho de coluna não tem efeito nenhum nesse cenário — então o clique é tratado aqui
// manualmente: ordenamos os GRUPOS pelo menor valor da coluna clicada dentro de cada grupo
// e, dentro do grupo, pelas mesmas regras.
internal static class RelatorioRenovacaoSortHelper
{
    private static readonly string LogPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ordenacao.log");

    public static void HandleSorting(
        string tela,
        DataGridSortingEventArgs e,
        DataGrid grid,
        ListCollectionView? listView,
        Func<RelatorioRenovacao, string> chaveGrupo)
    {
        var column = e.Column;
        var header = column.Header?.ToString() ?? "(sem título)";

        if (listView == null)
        {
            Log(tela, $"Coluna '{header}': RegistrosView não é um ListCollectionView (ou está nulo) — ordenação cancelada.");
            return;
        }

        e.Handled = true;

        var sortPath = ResolverCaminhoDeOrdenacao(column);
        if (string.IsNullOrEmpty(sortPath))
        {
            Log(tela, $"Coluna '{header}': não foi possível determinar a propriedade de ordenação " +
                      "(SortMemberPath vazio e Binding não é um caminho simples). Clique ignorado.");
            return;
        }

        var propriedade = typeof(RelatorioRenovacao).GetProperty(sortPath);
        if (propriedade == null)
        {
            Log(tela, $"Coluna '{header}': propriedade '{sortPath}' não existe em RelatorioRenovacao. Clique ignorado.");
            return;
        }

        var novaDirecao = column.SortDirection != ListSortDirection.Ascending
            ? ListSortDirection.Ascending
            : ListSortDirection.Descending;

        foreach (var col in grid.Columns)
            col.SortDirection = null;
        column.SortDirection = novaDirecao;

        var fatorDirecao = novaDirecao == ListSortDirection.Ascending ? 1 : -1;

        int CompareValores(object? x, object? y)
        {
            if (x == null && y == null) return 0;
            if (x == null) return -1;
            if (y == null) return 1;
            return Comparer<object>.Default.Compare(x, y);
        }

        var sw = Stopwatch.StartNew();
        var itens = listView.SourceCollection.Cast<RelatorioRenovacao>().ToList();
        var menorValorPorGrupo = itens
            .GroupBy(chaveGrupo)
            .ToDictionary(
                g => g.Key,
                g => g.Select(r => propriedade.GetValue(r))
                      .Aggregate((menor, atual) => CompareValores(atual, menor) < 0 ? atual : menor));

        listView.CustomSort = Comparer<object>.Create((a, b) =>
        {
            var ra = (RelatorioRenovacao)a;
            var rb = (RelatorioRenovacao)b;
            var chaveA = chaveGrupo(ra);
            var chaveB = chaveGrupo(rb);

            if (chaveA != chaveB)
            {
                var cmpGrupo = fatorDirecao * CompareValores(menorValorPorGrupo[chaveA], menorValorPorGrupo[chaveB]);
                return cmpGrupo != 0 ? cmpGrupo : string.CompareOrdinal(chaveA, chaveB);
            }

            var cmpValor = fatorDirecao * CompareValores(propriedade.GetValue(ra), propriedade.GetValue(rb));
            return cmpValor != 0 ? cmpValor : fatorDirecao * Nullable.Compare(ra.VigenciaFinal, rb.VigenciaFinal);
        });
        sw.Stop();

        Log(tela, $"Coluna '{header}' (propriedade '{sortPath}') ordenada {novaDirecao} — " +
                  $"{itens.Count} registro(s) em {menorValorPorGrupo.Count} grupo(s), {sw.ElapsedMilliseconds}ms.");
    }

    // O DataGrid só resolve automaticamente o Binding.Path como caminho de ordenação dentro
    // do seu processamento interno padrão do evento Sorting — algo que não roda mais aqui
    // porque marcamos e.Handled = true. Por isso replicamos manualmente esse fallback.
    private static string? ResolverCaminhoDeOrdenacao(DataGridColumn column)
    {
        if (!string.IsNullOrEmpty(column.SortMemberPath))
            return column.SortMemberPath;

        if (column is DataGridBoundColumn { Binding: Binding binding })
            return !string.IsNullOrEmpty(binding.XPath) ? binding.XPath : binding.Path?.Path;

        return null;
    }

    private static void Log(string tela, string mensagem)
    {
        Debug.WriteLine($"[OrdenacaoDataGrid][{tela}] {mensagem}");
        try
        {
            File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{tela}] {mensagem}\n");
        }
        catch
        {
            // Log é diagnóstico — nunca deve derrubar a ordenação por falha de I/O.
        }
    }
}
