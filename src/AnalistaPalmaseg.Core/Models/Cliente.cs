namespace AnalistaPalmaseg.Core.Models;

public class Cliente
{
    public int Id { get; set; }

    // Identificação
    public string Cpf { get; set; } = string.Empty;
    public string Nome { get; set; } = string.Empty;
    public DateTime? Nascimento { get; set; }
    public string? Sexo { get; set; }
    public string? EstadoCivil { get; set; }
    public string? Profissao { get; set; }
    public DateTime? ClienteDesde { get; set; }

    // Contato
    public string? Prefixo1 { get; set; }
    public string? Telefone1 { get; set; }
    public string? Prefixo2 { get; set; }
    public string? Telefone2 { get; set; }
    public string? Prefixo3 { get; set; }
    public string? Telefone3 { get; set; }
    public string? Email1 { get; set; }
    public string? Email2 { get; set; }

    // Endereço
    public string? Cep { get; set; }
    public string? Endereco { get; set; }
    public string? NumeroEndereco { get; set; }
    public string? Complemento { get; set; }
    public string? Bairro { get; set; }
    public string? Cidade { get; set; }
    public string? Estado { get; set; }

    // Notas
    public string? Observacoes { get; set; }
    public string? Historico { get; set; }

    public DateTime CriadoEm { get; set; }
    public DateTime? AtualizadoEm { get; set; }
}
