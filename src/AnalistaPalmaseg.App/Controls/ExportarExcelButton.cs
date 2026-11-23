using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using AnalistaPalmaseg.Core.Services;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;

namespace AnalistaPalmaseg.App.Controls;

/// <summary>
/// Botão "Exportar para Excel" de um DataGrid. Só aparece (Visible) para administradores;
/// para os demais fica Collapsed — não ocupa espaço nem aparece desabilitado.
/// Uso: &lt;controls:ExportarExcelButton Grid="{Binding ElementName=MainGrid}" Titulo="Renovações"/&gt;
/// </summary>
public class ExportarExcelButton : Button
{
    public static readonly DependencyProperty GridProperty =
        DependencyProperty.Register(nameof(Grid), typeof(DataGrid), typeof(ExportarExcelButton));

    public static readonly DependencyProperty TituloProperty =
        DependencyProperty.Register(nameof(Titulo), typeof(string), typeof(ExportarExcelButton),
            new PropertyMetadata("Exportacao"));

    /// <summary>DataGrid cujos dados (como filtrados/ordenados na tela) serão exportados.</summary>
    public DataGrid? Grid
    {
        get => (DataGrid?)GetValue(GridProperty);
        set => SetValue(GridProperty, value);
    }

    /// <summary>Nome sugerido do arquivo e da planilha.</summary>
    public string Titulo
    {
        get => (string)GetValue(TituloProperty);
        set => SetValue(TituloProperty, value);
    }

    /// <summary>x:Name do DataGrid na mesma tela (alternativa ao binding de <see cref="Grid"/>).</summary>
    public string? GridName { get; set; }

    public ExportarExcelButton()
    {
        SetResourceReference(StyleProperty, "MaterialDesignOutlinedButton");
        Height = 36;
        Padding = new Thickness(12, 0, 12, 0);
        ToolTip = "Exportar para Excel (dados conforme exibidos no grid)";
        Content = new PackIcon { Kind = PackIconKind.MicrosoftExcel, Width = 18, Height = 18 };

        // Oculto por padrão; só é exibido depois de confirmar que o usuário é administrador.
        Visibility = Visibility.Collapsed;
        Loaded += (_, _) => AtualizarVisibilidade();
    }

    private void AtualizarVisibilidade()
    {
        var sessao = (Application.Current as App)?.Services?.GetService<SessaoService>();
        Visibility = sessao?.IsAdmin == true ? Visibility.Visible : Visibility.Collapsed;
    }

    protected override void OnClick()
    {
        base.OnClick();
        // Defesa extra: mesmo que o botão fosse acionado de outra forma, só administrador exporta.
        if (Visibility != Visibility.Visible) return;

        var grid = Grid ?? LocalizarGrid();
        if (grid == null)
        {
            MessageBox.Show(Window.GetWindow(this),
                $"Não foi possível localizar o grid desta tela para exportar ({Titulo}).",
                "Exportar para Excel", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        ExcelGridExporter.Exportar(grid, Titulo, Window.GetWindow(this));
    }

    // Procura o DataGrid pelo x:Name subindo pela árvore até achar quem o declarou.
    private DataGrid? LocalizarGrid()
    {
        if (string.IsNullOrEmpty(GridName)) return null;

        DependencyObject? atual = this;
        DependencyObject? raiz = this;
        while (atual != null)
        {
            if (atual is FrameworkElement fe && fe.FindName(GridName) is DataGrid achado)
                return achado;
            raiz = atual;
            atual = LogicalTreeHelper.GetParent(atual) ?? VisualTreeHelper.GetParent(atual);
        }

        // Plano B: percorre a árvore visual inteira procurando o DataGrid pelo nome.
        return raiz == null ? null : BuscarPorNome(raiz, GridName);
    }

    private static DataGrid? BuscarPorNome(DependencyObject pai, string nome)
    {
        if (pai is DataGrid { } g && g.Name == nome) return g;
        var total = VisualTreeHelper.GetChildrenCount(pai);
        for (var i = 0; i < total; i++)
        {
            var achado = BuscarPorNome(VisualTreeHelper.GetChild(pai, i), nome);
            if (achado != null) return achado;
        }
        return null;
    }
}
