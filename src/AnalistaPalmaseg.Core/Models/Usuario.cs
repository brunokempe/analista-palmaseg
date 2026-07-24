namespace AnalistaPalmaseg.Core.Models;

public enum TipoAcesso { Colaborador = 0, Administrador = 1 }

public class Usuario
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string SenhaHash { get; set; } = string.Empty;
    public TipoAcesso TipoAcesso { get; set; }
    public bool Ativo { get; set; } = true;
}
