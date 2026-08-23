using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AnalistaPalmaseg.App.Controls;

/// <summary>
/// Dropdown de múltipla seleção (checkbox por item) usado nos filtros das telas de renovações.
/// ItemsSource recebe os valores disponíveis; SelectedItems é a coleção (mantida pelo ViewModel)
/// que a UI mutila diretamente — o ViewModel escuta CollectionChanged para reaplicar o filtro.
/// </summary>
public partial class MultiSelectFilterControl : UserControl
{
    public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
        nameof(ItemsSource), typeof(IEnumerable), typeof(MultiSelectFilterControl),
        new PropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register(
        nameof(SelectedItems), typeof(IList), typeof(MultiSelectFilterControl),
        new PropertyMetadata(null, OnSelectedItemsChanged));

    public static readonly DependencyProperty HintProperty = DependencyProperty.Register(
        nameof(Hint), typeof(string), typeof(MultiSelectFilterControl),
        new PropertyMetadata("Selecionar", OnHintChanged));

    private static readonly DependencyPropertyKey ResumoTextoPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(ResumoTexto), typeof(string), typeof(MultiSelectFilterControl),
        new PropertyMetadata("Selecionar"));

    public static readonly DependencyProperty ResumoTextoProperty = ResumoTextoPropertyKey.DependencyProperty;

    public IEnumerable ItemsSource
    {
        get => (IEnumerable)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public IList SelectedItems
    {
        get => (IList)GetValue(SelectedItemsProperty);
        set => SetValue(SelectedItemsProperty, value);
    }

    public string Hint
    {
        get => (string)GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    public string ResumoTexto => (string)GetValue(ResumoTextoProperty);

    public ObservableCollection<OpcaoFiltro> Opcoes { get; } = [];

    public MultiSelectFilterControl()
    {
        InitializeComponent();
    }

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (MultiSelectFilterControl)d;

        if (e.OldValue is INotifyCollectionChanged antigo)
            antigo.CollectionChanged -= control.OnItemsSourceCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged novo)
            novo.CollectionChanged += control.OnItemsSourceCollectionChanged;

        control.ReconstruirOpcoes();
    }

    private void OnItemsSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        ReconstruirOpcoes();

    private static void OnSelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (MultiSelectFilterControl)d;

        if (e.OldValue is INotifyCollectionChanged antigo)
            antigo.CollectionChanged -= control.OnSelectedItemsCollectionChanged;
        if (e.NewValue is INotifyCollectionChanged novo)
            novo.CollectionChanged += control.OnSelectedItemsCollectionChanged;

        control.SincronizarSelecao();
        control.AtualizarResumo();
    }

    private void OnSelectedItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        SincronizarSelecao();
        AtualizarResumo();
    }

    private static void OnHintChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
        ((MultiSelectFilterControl)d).AtualizarResumo();

    private void ReconstruirOpcoes()
    {
        var valoresAtuais = ItemsSource?.Cast<object>().Select(o => o?.ToString() ?? string.Empty).ToList() ?? [];

        // Remove opções que não existem mais na fonte
        for (int i = Opcoes.Count - 1; i >= 0; i--)
            if (!valoresAtuais.Contains(Opcoes[i].Valor))
                Opcoes.RemoveAt(i);

        // Adiciona novas opções mantendo a ordem da fonte
        var existentes = Opcoes.Select(o => o.Valor).ToHashSet();
        foreach (var valor in valoresAtuais)
            if (!existentes.Contains(valor))
                Opcoes.Add(new OpcaoFiltro(valor, this));

        SincronizarSelecao();
        AtualizarResumo();
    }

    private void SincronizarSelecao()
    {
        var selecionados = SelectedItems;
        foreach (var opcao in Opcoes)
            opcao.SetSelecionadoSemNotificar(selecionados != null && selecionados.Contains(opcao.Valor));
    }

    internal void NotificarSelecaoAlterada(string valor, bool selecionado)
    {
        var lista = SelectedItems;
        if (lista == null) return;

        var contem = lista.Contains(valor);
        if (selecionado && !contem) lista.Add(valor);
        else if (!selecionado && contem) lista.Remove(valor);

        AtualizarResumo();
    }

    private void AtualizarResumo()
    {
        var selecionados = SelectedItems?.Cast<object>().Select(o => o?.ToString() ?? string.Empty).ToList() ?? [];

        string texto;
        if (selecionados.Count == 0)
            texto = Hint;
        else if (selecionados.Count <= 2)
            texto = string.Join(", ", selecionados);
        else
            texto = $"{selecionados[0]}, {selecionados[1]} +{selecionados.Count - 2}";

        SetValue(ResumoTextoPropertyKey, texto);
    }

    private void LimparSelecao_Click(object sender, MouseButtonEventArgs e)
    {
        SelectedItems?.Clear();
        FilterPopup.IsOpen = false;
    }

    private void FilterPopup_Opened(object? sender, EventArgs e)
    {
        // Reaplica o estado real da coleção ao abrir, cobrindo alterações feitas fora do
        // controle (ex: comando "Limpar filtros" do ViewModel).
        SincronizarSelecao();
    }
}

public sealed class OpcaoFiltro(string valor, MultiSelectFilterControl owner) : INotifyPropertyChanged
{
    private bool _selecionado;

    public string Valor { get; } = valor;

    public bool Selecionado
    {
        get => _selecionado;
        set
        {
            if (_selecionado == value) return;
            _selecionado = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selecionado)));
            owner.NotificarSelecaoAlterada(Valor, value);
        }
    }

    internal void SetSelecionadoSemNotificar(bool value)
    {
        if (_selecionado == value) return;
        _selecionado = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Selecionado)));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
}
