using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;
using Microsoft.Win32;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class SalvarPropostasViewModel : ObservableObject
{
    private readonly PastaSalvarPropostaService _service;

    [ObservableProperty] private ObservableCollection<PastaSalvarProposta> _pastas = [];
    [ObservableProperty] private string _mensagem = string.Empty;
    [ObservableProperty] private bool _isMensagemErro;
    [ObservableProperty] private bool _isProcessando;

    public SalvarPropostasViewModel(PastaSalvarPropostaService service)
    {
        _service = service;
    }

    public async Task CarregarAsync()
    {
        var lista = await _service.GetTodosAsync();
        Pastas = new ObservableCollection<PastaSalvarProposta>(lista);
    }

    [RelayCommand]
    private async Task AdicionarPastaAsync()
    {
        var dialog = new OpenFolderDialog { Title = "Selecione a pasta para salvar as propostas" };
        if (dialog.ShowDialog() != true) return;

        if (Pastas.Any(p => string.Equals(p.Caminho, dialog.FolderName, StringComparison.OrdinalIgnoreCase)))
        {
            ExibirErro("Este diretório já está cadastrado.");
            return;
        }

        await _service.AdicionarAsync(dialog.FolderName);
        ExibirSucesso("Diretório adicionado com sucesso.");
        await CarregarAsync();
    }

    [RelayCommand]
    private async Task RemoverPastaAsync(PastaSalvarProposta pasta)
    {
        var confirm = MessageBox.Show(
            $"Remover o diretório '{pasta.Caminho}'? Os arquivos deixarão de ser salvos nessa pasta.",
            "Confirmar remoção",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        await _service.ExcluirAsync(pasta.Id);
        ExibirSucesso("Diretório removido.");
        await CarregarAsync();
    }

    [RelayCommand]
    private async Task SelecionarArquivosAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Selecionar proposta(s) para salvar",
            Multiselect = true,
            Filter = "Todos os arquivos|*.*|PDF|*.pdf|Word|*.docx;*.doc|Excel|*.xlsx;*.xls|Imagens|*.jpg;*.jpeg;*.png"
        };
        if (dialog.ShowDialog() != true) return;

        await SalvarArquivosAsync(dialog.FileNames);
    }

    public async Task SalvarArquivosAsync(IEnumerable<string> arquivos)
    {
        if (Pastas.Count == 0)
        {
            ExibirErro("Cadastre ao menos um diretório antes de salvar arquivos.");
            return;
        }

        IsProcessando = true;
        try
        {
            var (sucesso, erros) = await _service.SalvarArquivosAsync(arquivos);
            if (erros == 0)
                ExibirSucesso($"{sucesso} arquivo(s) salvo(s) em {Pastas.Count} diretório(s) configurado(s).");
            else
                ExibirErro($"{sucesso} arquivo(s) salvo(s) com sucesso, {erros} com erro. Verifique se os diretórios configurados existem.");
        }
        finally
        {
            IsProcessando = false;
        }
    }

    private void ExibirSucesso(string msg) { Mensagem = msg; IsMensagemErro = false; }
    private void ExibirErro(string msg) { Mensagem = msg; IsMensagemErro = true; }
}
