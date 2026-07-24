namespace AnalistaPalmaseg.Core.Models;

public class Anexo
{
    public int Id { get; set; }
    public int RelatorioRenovacaoId { get; set; }
    public string NomeArquivo { get; set; } = string.Empty;
    public string CaminhoArquivo { get; set; } = string.Empty;
    public long TamanhoBytes { get; set; }
    public DateTime AdicionadoEm { get; set; }
}
