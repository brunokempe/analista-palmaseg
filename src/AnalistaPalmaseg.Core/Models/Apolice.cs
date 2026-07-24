namespace AnalistaPalmaseg.Core.Models;

public class Apolice
{
    public int Id { get; set; }
    public int ImportacaoApoliceId { get; set; }

    public string NumeroApolice { get; set; } = string.Empty;
    public string Segurado { get; set; } = string.Empty;
    public string Seguradora { get; set; } = string.Empty;
    public string Ramo { get; set; } = string.Empty;
    public DateOnly DataVencimentoPagamento { get; set; }
    public DateOnly? DataInicioVigencia { get; set; }
    public DateOnly? DataFimVigencia { get; set; }
    public decimal Premio { get; set; }
    public string? Observacao { get; set; }

    public int DiasParaVencimento
    {
        get
        {
            var hoje = DateOnly.FromDateTime(DateTime.Today);
            return DataVencimentoPagamento.DayNumber - hoje.DayNumber;
        }
    }

    public string StatusLabel => DiasParaVencimento < 0
        ? "Vencida"
        : DiasParaVencimento <= 30
            ? "Próxima"
            : "Em dia";

    public string DiasLabel => DiasParaVencimento switch
    {
        < 0 => $"{Math.Abs(DiasParaVencimento)} dias em atraso",
        0   => "Vence hoje",
        1   => "Vence amanhã",
        _   => $"Vence em {DiasParaVencimento} dias"
    };
}
