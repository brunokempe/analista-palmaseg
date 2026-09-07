using System.Windows;

namespace AnalistaPalmaseg.App.Views;

public partial class AlterarVigenciaDialog : Window
{
    public DateTime? VigenciaInicial { get; private set; }
    public DateTime? VigenciaFinal { get; private set; }

    public AlterarVigenciaDialog(string nomeCliente, DateTime? vigenciaInicial, DateTime? vigenciaFinal)
    {
        InitializeComponent();
        PromptText.Text = $"Alterar a vigência da apólice de \"{nomeCliente}\":";
        VigenciaInicialPicker.SelectedDate = vigenciaInicial;
        VigenciaFinalPicker.SelectedDate = vigenciaFinal;
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        if (VigenciaFinalPicker.SelectedDate == null)
        {
            ErroText.Visibility = Visibility.Visible;
            return;
        }

        VigenciaInicial = VigenciaInicialPicker.SelectedDate;
        VigenciaFinal = VigenciaFinalPicker.SelectedDate;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
