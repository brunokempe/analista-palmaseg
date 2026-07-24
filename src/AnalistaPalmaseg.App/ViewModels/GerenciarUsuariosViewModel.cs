using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class GerenciarUsuariosViewModel : ObservableObject
{
    private readonly UsuarioService _usuarioService;
    private readonly SessaoService _sessao;

    [ObservableProperty] private ObservableCollection<Usuario> _usuarios = [];
    [ObservableProperty] private string _novoLogin = string.Empty;
    [ObservableProperty] private string _mensagem = string.Empty;
    [ObservableProperty] private bool _isMensagemErro;
    [ObservableProperty] private TipoAcesso _novoTipo = TipoAcesso.Colaborador;

    public string NovaSenha { get; set; } = string.Empty;
    public TipoAcesso[] TiposAcesso { get; } = [TipoAcesso.Colaborador, TipoAcesso.Administrador];

    public GerenciarUsuariosViewModel(UsuarioService usuarioService, SessaoService sessao)
    {
        _usuarioService = usuarioService;
        _sessao = sessao;
    }

    public async Task CarregarAsync()
    {
        var lista = await _usuarioService.ListarAsync();
        Usuarios = new ObservableCollection<Usuario>(lista);
    }

    [RelayCommand]
    private async Task AdicionarAsync()
    {
        if (string.IsNullOrWhiteSpace(NovoLogin) || string.IsNullOrWhiteSpace(NovaSenha))
        {
            ExibirErro("Preencha o login e a senha.");
            return;
        }

        if (await _usuarioService.LoginExisteAsync(NovoLogin.Trim()))
        {
            ExibirErro($"O login '{NovoLogin.Trim()}' já está em uso.");
            return;
        }

        await _usuarioService.AdicionarAsync(NovoLogin.Trim(), NovaSenha, NovoTipo);
        NovoLogin = string.Empty;
        NovaSenha = string.Empty;
        NovoTipo = TipoAcesso.Colaborador;
        AdicionarConcluido?.Invoke(this, EventArgs.Empty);
        ExibirSucesso("Usuário adicionado com sucesso.");
        await CarregarAsync();
    }

    [RelayCommand]
    private async Task AlterarSenhaAsync(Usuario usuario)
    {
        var dialog = new Views.SenhaDialog("Alterar senha", $"Nova senha para '{usuario.Login}':")
            { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() != true || string.IsNullOrEmpty(dialog.Senha)) return;

        await _usuarioService.AlterarSenhaAsync(usuario.Id, dialog.Senha);
        ExibirSucesso($"Senha de '{usuario.Login}' alterada com sucesso.");
    }

    [RelayCommand]
    private async Task ToggleAtivoAsync(Usuario usuario)
    {
        if (usuario.Id == _sessao.UsuarioAtual?.Id)
        {
            ExibirErro("Você não pode desativar sua própria conta.");
            return;
        }
        await _usuarioService.ToggleAtivoAsync(usuario.Id);
        await CarregarAsync();
    }

    [RelayCommand]
    private async Task RemoverAsync(Usuario usuario)
    {
        if (usuario.Id == _sessao.UsuarioAtual?.Id)
        {
            ExibirErro("Você não pode remover sua própria conta.");
            return;
        }

        var confirm = MessageBox.Show(
            $"Remover o usuário '{usuario.Login}'? Esta ação não pode ser desfeita.",
            "Confirmar remoção",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        await _usuarioService.RemoverAsync(usuario.Id);
        ExibirSucesso($"Usuário '{usuario.Login}' removido.");
        await CarregarAsync();
    }

    public event EventHandler? AdicionarConcluido;

    private void ExibirSucesso(string msg) { Mensagem = msg; IsMensagemErro = false; }
    private void ExibirErro(string msg) { Mensagem = msg; IsMensagemErro = true; }
}
