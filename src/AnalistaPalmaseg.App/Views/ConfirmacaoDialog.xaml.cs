using System.Windows;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace AnalistaPalmaseg.App.Views;

/// <summary>Caixa de confirmação Sim/Não no estilo Material Design.</summary>
public partial class ConfirmacaoDialog : Window
{
    public ConfirmacaoDialog(string titulo, string mensagem,
        string textoSim = "Sim", string textoNao = "Não",
        PackIconKind icone = PackIconKind.PowerStandby)
    {
        InitializeComponent();
        TituloText.Text = titulo;
        MensagemText.Text = mensagem;
        SimButton.Content = textoSim;
        NaoButton.Content = textoNao;
        Icone.Kind = icone;

        // Sem barra de título: permite arrastar pela janela.
        MouseLeftButtonDown += (_, e) =>
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        };
        Loaded += (_, _) => NaoButton.Focus(); // padrão seguro: Enter não confirma
    }

    /// <summary>Exibe o diálogo e retorna true se o usuário confirmou.</summary>
    public static bool Perguntar(Window? dono, string titulo, string mensagem,
        string textoSim = "Sim", string textoNao = "Não",
        PackIconKind icone = PackIconKind.PowerStandby)
    {
        var dialog = new ConfirmacaoDialog(titulo, mensagem, textoSim, textoNao, icone);
        if (dono is { IsVisible: true }) dialog.Owner = dono;
        return dialog.ShowDialog() == true;
    }

    private void Sim_Click(object sender, RoutedEventArgs e) => DialogResult = true;
    private void Nao_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
