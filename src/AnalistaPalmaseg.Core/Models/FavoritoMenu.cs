namespace AnalistaPalmaseg.Core.Models;

public class FavoritoMenu
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public string MenuKey { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}
