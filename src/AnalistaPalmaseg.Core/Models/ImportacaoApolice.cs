namespace AnalistaPalmaseg.Core.Models;

public class ImportacaoApolice
{
    public int Id { get; set; }
    public DateTime ImportadoEm { get; set; }
    public string ArquivoOrigem { get; set; } = string.Empty;

    public ICollection<Apolice> Apolices { get; set; } = [];
}
