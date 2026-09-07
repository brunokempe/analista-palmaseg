namespace AnalistaPalmaseg.Core.Models;

public class PastaProdutor
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }
    public string Caminho { get; set; } = string.Empty;
    public DateTime CriadoEm { get; set; }
}
