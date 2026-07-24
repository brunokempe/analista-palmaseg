namespace AnalistaPalmaseg.Core.Models;

public class Importacao
{
    public int Id { get; set; }
    public string Produtor { get; set; } = string.Empty;
    public int Mes { get; set; }
    public int Ano { get; set; }
    public DateTime ImportadoEm { get; set; }
    public string ArquivoOrigem { get; set; } = string.Empty;

    public ICollection<Renovacao> Renovacoes { get; set; } = [];
    public ICollection<NovoNegocio> NovosNegocios { get; set; } = [];
    public ICollection<ResultadoMeta> Resultados { get; set; } = [];
    public ICollection<FuncionarioResultado> FuncionariosResultados { get; set; } = [];

    public string Periodo => $"{Mes:D2}/{Ano}";
}
