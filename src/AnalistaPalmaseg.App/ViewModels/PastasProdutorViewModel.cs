using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;
using Microsoft.Win32;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class PastasProdutorViewModel : ObservableObject
{
    private readonly PastaProdutorService _service;

    [ObservableProperty] private ObservableCollection<Usuario> _produtores = [];
    [ObservableProperty] private Usuario? _produtorSelecionado;
    [ObservableProperty] private ObservableCollection<PastaProdutor> _diretorios = [];
    [ObservableProperty] private bool _isLoading;

    public bool TemProdutorSelecionado => ProdutorSelecionado != null;

    public PastasProdutorViewModel(PastaProdutorService service) => _service = service;

    public async Task CarregarAsync()
    {
        IsLoading = true;
        try
        {
            var lista = await _service.GetProdutoresAsync();
            Produtores = new ObservableCollection<Usuario>(lista);
        }
        finally { IsLoading = false; }
    }

    partial void OnProdutorSelecionadoChanged(Usuario? value)
    {
        OnPropertyChanged(nameof(TemProdutorSelecionado));
        _ = CarregarDiretoriosAsync(value);
    }

    private async Task CarregarDiretoriosAsync(Usuario? produtor)
    {
        if (produtor == null)
        {
            Diretorios.Clear();
            return;
        }

        var lista = await _service.GetDiretoriosAsync(produtor.Id);
        Diretorios = new ObservableCollection<PastaProdutor>(lista);
    }

    [RelayCommand]
    private async Task AdicionarDiretorioAsync()
    {
        if (ProdutorSelecionado == null) return;

        var dialog = new OpenFolderDialog { Title = $"Selecione a pasta para {ProdutorSelecionado.Login}" };
        if (dialog.ShowDialog() != true) return;

        if (Diretorios.Any(p => string.Equals(p.Caminho, dialog.FolderName, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show("Este diretório já está cadastrado para o produtor.", "Diretório duplicado",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        await _service.AdicionarDiretorioAsync(ProdutorSelecionado.Id, dialog.FolderName);
        await CarregarDiretoriosAsync(ProdutorSelecionado);
    }

    [RelayCommand]
    private async Task RemoverDiretorioAsync(PastaProdutor pasta)
    {
        var confirmacao = MessageBox.Show(
            $"Remover o diretório '{pasta.Caminho}'?",
            "Confirmar remoção",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirmacao != MessageBoxResult.Yes) return;

        await _service.RemoverDiretorioAsync(pasta.Id);
        Diretorios.Remove(Diretorios.First(p => p.Id == pasta.Id));
    }
}
