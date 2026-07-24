using AnalistaPalmaseg.Core.Models;

namespace AnalistaPalmaseg.Core.Services;

public class SessaoService
{
    public Usuario? UsuarioAtual { get; private set; }
    public bool IsAdmin => UsuarioAtual?.TipoAcesso == TipoAcesso.Administrador;
    public string NomeUsuario => UsuarioAtual?.Login ?? string.Empty;

    public void Iniciar(Usuario usuario) => UsuarioAtual = usuario;
    public void Encerrar() => UsuarioAtual = null;
}
