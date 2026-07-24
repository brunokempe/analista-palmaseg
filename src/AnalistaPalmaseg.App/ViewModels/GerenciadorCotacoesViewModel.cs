using CommunityToolkit.Mvvm.ComponentModel;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class GerenciadorCotacoesViewModel : ObservableObject
{
    [ObservableProperty] private string _mensagem = "Módulo de cotações em desenvolvimento.";
}
