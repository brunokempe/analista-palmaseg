using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using ClosedXML.Excel;
using Microsoft.Win32;

namespace AnalistaPalmaseg.App.Controls;

/// <summary>
/// Exporta o que o DataGrid está exibindo (linhas filtradas/ordenadas, colunas visíveis na
/// ordem de tela) para um arquivo .xlsx.
/// </summary>
public static class ExcelGridExporter
{
    public static void Exportar(DataGrid grid, string titulo, Window? dono)
    {
        try
        {
            var colunas = grid.Columns
                .Where(c => c.Visibility == Visibility.Visible)
                .OrderBy(c => c.DisplayIndex)
                .Select(c => new { Coluna = c, Extrator = CriarExtrator(c) })
                .Where(x => x.Extrator != null)
                .ToList();

            var itens = grid.Items.Cast<object>()
                .Where(i => i != CollectionView.NewItemPlaceholder)
                .ToList();

            if (colunas.Count == 0 || itens.Count == 0)
            {
                MessageBox.Show(dono, "Não há dados no grid para exportar.", "Exportar para Excel",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var nomeBase = NomeArquivoSeguro(titulo);
            var dialog = new SaveFileDialog
            {
                Title = "Exportar para Excel",
                Filter = "Planilha do Excel (*.xlsx)|*.xlsx",
                FileName = $"{nomeBase}_{DateTime.Now:yyyy-MM-dd_HHmm}.xlsx",
                AddExtension = true,
                DefaultExt = ".xlsx",
            };
            if (dialog.ShowDialog(dono) != true) return;

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(NomePlanilhaSeguro(titulo));

            for (var c = 0; c < colunas.Count; c++)
            {
                var cell = ws.Cell(1, c + 1);
                cell.Value = CabecalhoTexto(colunas[c].Coluna);
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E5E7EB");
            }

            for (var r = 0; r < itens.Count; r++)
                for (var c = 0; c < colunas.Count; c++)
                    Escrever(ws.Cell(r + 2, c + 1), colunas[c].Extrator!(itens[r]));

            ws.SheetView.FreezeRows(1);
            ws.Range(1, 1, 1, colunas.Count).SetAutoFilter();
            ws.Columns().AdjustToContents(1, Math.Min(itens.Count + 1, 200), 8, 60);

            wb.SaveAs(dialog.FileName);

            MessageBox.Show(dono,
                $"Planilha salva com {itens.Count} linha(s):\n{dialog.FileName}",
                "Exportar para Excel", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (IOException ex)
        {
            MessageBox.Show(dono,
                "Não foi possível gravar o arquivo. Se ele estiver aberto no Excel, feche-o e tente de novo.\n\n" + ex.Message,
                "Exportar para Excel", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(dono, "Erro ao exportar: " + ex.Message, "Exportar para Excel",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ── Extração de valores ────────────────────────────────────────────────────

    private static Func<object, object?>? CriarExtrator(DataGridColumn coluna)
    {
        // Colunas de texto/checkbox/combo ligadas a uma propriedade: avalia o próprio Binding
        // (inclui conversores, ex.: ramo abreviado), como o usuário vê na tela.
        if (coluna is DataGridBoundColumn { Binding: BindingBase binding })
            return item => Avaliar(binding, item);

        if (coluna is DataGridComboBoxColumn combo)
        {
            var b = combo.SelectedItemBinding ?? combo.SelectedValueBinding ?? combo.TextBinding;
            if (b != null) return item => Avaliar(b, item);
        }

        // Colunas de template: usa SortMemberPath quando existe (ex.: Novo Produtor, Situação);
        // sem ele e sem cabeçalho de texto (checkbox de marcação, botões) a coluna é ignorada.
        if (!string.IsNullOrWhiteSpace(coluna.SortMemberPath))
        {
            var porOrdenacao = new Binding(coluna.SortMemberPath);
            return item => Avaliar(porOrdenacao, item);
        }

        if (coluna is DataGridTemplateColumn { CellTemplate: { } template }
            && !string.IsNullOrWhiteSpace(CabecalhoTexto(coluna)))
            return item => TextoDoTemplate(template, item);

        return null;
    }

    private sealed class AvaliadorBinding : DependencyObject
    {
        public static readonly DependencyProperty ValorProperty =
            DependencyProperty.Register("Valor", typeof(object), typeof(AvaliadorBinding));
    }

    private static object? Avaliar(BindingBase bindingBase, object item)
    {
        if (bindingBase is not Binding original) return null;

        var binding = new Binding
        {
            Path = original.Path,
            Source = item,
            Converter = original.Converter,
            ConverterParameter = original.ConverterParameter,
            ConverterCulture = original.ConverterCulture ?? CultureInfo.CurrentCulture,
            FallbackValue = null,
            TargetNullValue = null,
            Mode = BindingMode.OneWay,
        };
        var avaliador = new AvaliadorBinding();
        BindingOperations.SetBinding(avaliador, AvaliadorBinding.ValorProperty, binding);
        var valor = avaliador.GetValue(AvaliadorBinding.ValorProperty);
        BindingOperations.ClearBinding(avaliador, AvaliadorBinding.ValorProperty);
        return valor == DependencyProperty.UnsetValue ? null : valor;
    }

    // Último recurso: renderiza o template da célula e lê o primeiro TextBlock com texto.
    private static object? TextoDoTemplate(DataTemplate template, object item)
    {
        var presenter = new ContentPresenter { Content = item, ContentTemplate = template };
        presenter.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        presenter.Arrange(new Rect(presenter.DesiredSize));
        presenter.UpdateLayout();
        return PrimeiroTexto(presenter);
    }

    private static string? PrimeiroTexto(DependencyObject raiz)
    {
        var total = VisualTreeHelper.GetChildrenCount(raiz);
        for (var i = 0; i < total; i++)
        {
            var filho = VisualTreeHelper.GetChild(raiz, i);
            if (filho is TextBlock { Text.Length: > 0 } tb) return tb.Text;
            var achado = PrimeiroTexto(filho);
            if (achado != null) return achado;
        }
        return null;
    }

    private static string CabecalhoTexto(DataGridColumn coluna) =>
        coluna.Header?.ToString()?.Trim() ?? string.Empty;

    // ── Escrita na planilha ────────────────────────────────────────────────────

    private static void Escrever(IXLCell cell, object? valor)
    {
        switch (valor)
        {
            case null:
                return;
            case DateTime dt:
                cell.Value = dt;
                cell.Style.DateFormat.Format = dt.TimeOfDay == TimeSpan.Zero ? "dd/MM/yyyy" : "dd/MM/yyyy HH:mm";
                return;
            case DateOnly d:
                cell.Value = d.ToDateTime(TimeOnly.MinValue);
                cell.Style.DateFormat.Format = "dd/MM/yyyy";
                return;
            case bool b:
                cell.Value = b ? "Sim" : "Não";
                return;
            case decimal m:
                cell.Value = m;
                cell.Style.NumberFormat.Format = "#,##0.00";
                return;
            case double or float:
                cell.Value = Convert.ToDouble(valor, CultureInfo.InvariantCulture);
                cell.Style.NumberFormat.Format = "#,##0.00";
                return;
            case byte or short or int or long or sbyte or ushort or uint or ulong:
                cell.Value = Convert.ToDouble(valor, CultureInfo.InvariantCulture);
                cell.Style.NumberFormat.Format = "0";
                return;
            default:
                // Texto (inclusive CPF/CNPJ, apólice, proposta): mantém como texto para não perder zeros.
                cell.Value = valor.ToString();
                return;
        }
    }

    private static string NomeArquivoSeguro(string titulo)
    {
        var invalidos = Path.GetInvalidFileNameChars();
        var nome = new string(titulo.Where(c => !invalidos.Contains(c)).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(nome) ? "Exportacao" : nome.Replace(' ', '_');
    }

    private static string NomePlanilhaSeguro(string titulo)
    {
        var proibidos = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var nome = new string(titulo.Where(c => !proibidos.Contains(c)).ToArray()).Trim();
        if (string.IsNullOrWhiteSpace(nome)) nome = "Dados";
        return nome.Length > 31 ? nome[..31] : nome;
    }
}
