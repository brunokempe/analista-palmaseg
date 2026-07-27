namespace AnalistaPalmaseg.Core.Models;

public class Seguradora
{
    public int Id { get; set; }
    public string Nome { get; set; } = string.Empty;
    public bool IsParceira { get; set; }
    public bool Ativo { get; set; } = true;
}
