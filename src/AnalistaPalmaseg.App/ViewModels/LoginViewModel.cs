using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    private readonly UsuarioService _usuarioService;
    private readonly SessaoService _sessao;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEntrar))]
    private bool _isProcessando;

    [ObservableProperty] private string _loginText = string.Empty;
    [ObservableProperty] private string _mensagemErro = string.Empty;

    public string Senha { get; set; } = string.Empty;
    public bool CanEntrar => !IsProcessando;

    public event EventHandler? LoginSucesso;

    public LoginViewModel(UsuarioService usuarioService, SessaoService sessao)
    {
        _usuarioService = usuarioService;
        _sessao = sessao;
    }

    [RelayCommand]
    private async Task EntrarAsync()
    {
        if (string.IsNullOrWhiteSpace(LoginText) || string.IsNullOrEmpty(Senha))
        {
            MensagemErro = "Preencha o usuário e a senha.";
            return;
        }

        MensagemErro = string.Empty;
        IsProcessando = true;
        try
        {
            var usuario = await _usuarioService.AutenticarAsync(LoginText.Trim(), Senha);
            if (usuario == null)
            {
                MensagemErro = "Usuário ou senha incorretos.";
                return;
            }
            _sessao.Iniciar(usuario);
            LoginSucesso?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            IsProcessando = false;
        }
    }
}
