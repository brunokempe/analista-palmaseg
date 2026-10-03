using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.Views;

public partial class ClienteSelectorDialog : Window
{
    private readonly ClienteService _clienteService;
    private readonly ObservableCollection<Cliente> _clientes = [];
    private List<Cliente> _todos = [];

    public Cliente? ClienteSelecionado { get; private set; }

    public ClienteSelectorDialog(ClienteService clienteService, string? buscaInicial = null)
    {
        InitializeComponent();
        _clienteService = clienteService;
        ClientesGrid.ItemsSource = _clientes;
        FiltroBox.Text = buscaInicial?.Trim() ?? string.Empty;
        Loaded += async (_, _) => await CarregarClientesAsync();
    }

    private async Task CarregarClientesAsync()
    {
        try
        {
            _todos = await _clienteService.GetTodosAsync();
            AplicarFiltro();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao carregar clientes:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AplicarFiltro()
    {
        var termo = FiltroBox.Text?.Trim();
        var filtrados = string.IsNullOrEmpty(termo)
            ? _todos
            : _todos.Where(c =>
                (c.Nome?.Contains(termo, StringComparison.OrdinalIgnoreCase) == true) ||
                (c.Cpf?.Contains(termo, StringComparison.OrdinalIgnoreCase) == true))
              .ToList();

        _clientes.Clear();
        foreach (var c in filtrados) _clientes.Add(c);
    }

    private void FiltroBox_TextChanged(object sender, TextChangedEventArgs e) => AplicarFiltro();

    private void ClientesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ClientesGrid.SelectedItem is Cliente cliente) Confirmar(cliente);
    }

    private void Selecionar_Click(object sender, RoutedEventArgs e)
    {
        if (ClientesGrid.SelectedItem is Cliente cliente) Confirmar(cliente);
        else
            MessageBox.Show("Selecione um cliente na lista ou cadastre um novo.", "Nenhum cliente selecionado",
                MessageBoxButton.OK, MessageBoxImage.Warning);
    }

    private void Confirmar(Cliente cliente)
    {
        ClienteSelecionado = cliente;
        DialogResult = true;
    }

    private void Cancelar_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void NovoCliente_Click(object sender, RoutedEventArgs e)
    {
        NovoNomeBox.Text = FiltroBox.Text?.Trim() ?? string.Empty;
        NovoCpfBox.Text = string.Empty;
        NovoTelefoneBox.Text = string.Empty;
        NovoEmailBox.Text = string.Empty;
        NovoObservacoesBox.Text = string.Empty;
        NovoErroText.Visibility = Visibility.Collapsed;

        PesquisaPanel.Visibility = Visibility.Collapsed;
        NovoClientePanel.Visibility = Visibility.Visible;
        NovoNomeBox.Focus();
    }

    private static string ApenasDigitos(string? texto) =>
        new([.. (texto ?? string.Empty).Where(char.IsDigit)]);

    private static string FormatarTelefone(string digitos)
    {
        if (digitos.Length > 11) digitos = digitos[..11];
        return digitos.Length switch
        {
            0 => string.Empty,
            <= 2 => $"({digitos}",
            <= 6 => $"({digitos[..2]}) {digitos[2..]}",
            <= 10 => $"({digitos[..2]}) {digitos[2..6]}-{digitos[6..]}",
            _ => $"({digitos[..2]}) {digitos[2..7]}-{digitos[7..]}"
        };
    }

    private static string FormatarCpfCnpj(string digitos)
    {
        if (digitos.Length <= 11)
        {
            return digitos.Length switch
            {
                <= 3 => digitos,
                <= 6 => $"{digitos[..3]}.{digitos[3..]}",
                <= 9 => $"{digitos[..3]}.{digitos[3..6]}.{digitos[6..]}",
                _ => $"{digitos[..3]}.{digitos[3..6]}.{digitos[6..9]}-{digitos[9..]}"
            };
        }

        if (digitos.Length > 14) digitos = digitos[..14];
        return digitos.Length switch
        {
            <= 2 => digitos,
            <= 5 => $"{digitos[..2]}.{digitos[2..]}",
            <= 8 => $"{digitos[..2]}.{digitos[2..5]}.{digitos[5..]}",
            <= 12 => $"{digitos[..2]}.{digitos[2..5]}.{digitos[5..8]}/{digitos[8..]}",
            _ => $"{digitos[..2]}.{digitos[2..5]}.{digitos[5..8]}/{digitos[8..12]}-{digitos[12..]}"
        };
    }

    private static void AplicarMascara(TextBox textBox, Func<string, string> formatar)
    {
        var formatado = formatar(ApenasDigitos(textBox.Text));
        if (textBox.Text == formatado) return;

        textBox.Text = formatado;
        textBox.CaretIndex = formatado.Length;
    }

    private void NovoTelefoneBox_TextChanged(object sender, TextChangedEventArgs e) =>
        AplicarMascara(NovoTelefoneBox, FormatarTelefone);

    private void NovoCpfBox_TextChanged(object sender, TextChangedEventArgs e) =>
        AplicarMascara(NovoCpfBox, FormatarCpfCnpj);

    private void VoltarPesquisa_Click(object sender, RoutedEventArgs e)
    {
        NovoClientePanel.Visibility = Visibility.Collapsed;
        PesquisaPanel.Visibility = Visibility.Visible;
    }

    private async void SalvarNovoCliente_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NovoNomeBox.Text))
        {
            NovoErroText.Visibility = Visibility.Visible;
            NovoNomeBox.Focus();
            return;
        }

        var cliente = new Cliente
        {
            Nome = NovoNomeBox.Text.Trim(),
            Cpf = NovoCpfBox.Text?.Trim() ?? string.Empty,
            Telefone1 = string.IsNullOrWhiteSpace(NovoTelefoneBox.Text) ? null : NovoTelefoneBox.Text.Trim(),
            Email1 = string.IsNullOrWhiteSpace(NovoEmailBox.Text) ? null : NovoEmailBox.Text.Trim(),
            Observacoes = string.IsNullOrWhiteSpace(NovoObservacoesBox.Text) ? null : NovoObservacoesBox.Text.Trim()
        };

        try
        {
            var salvo = await _clienteService.SalvarAsync(cliente);
            Confirmar(salvo);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Erro ao salvar cliente:\n{ex.Message}", "Erro",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
