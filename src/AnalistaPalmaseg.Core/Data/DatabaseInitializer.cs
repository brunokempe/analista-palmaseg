using AnalistaPalmaseg.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace AnalistaPalmaseg.Core.Data;

public class DatabaseInitializer(AppDbContext context)
{
    public void Initialize()
    {
        context.Database.Migrate();

        // Seed: seguradoras padrão (parceiras e demais)
        if (!context.Seguradoras.Any())
        {
            var parceiras = new[] { "Porto", "Unimed", "Hdi", "Allianz", "Tokio", "Zurich", "Bradesco", "Azul" };
            foreach (var nome in parceiras)
                context.Seguradoras.Add(new Models.Seguradora { Nome = nome, IsParceira = true, Ativo = true });
            context.Seguradoras.Add(new Models.Seguradora { Nome = "Demais", IsParceira = false, Ativo = true });
            context.SaveChanges();
        }

        // Seed: premiação por seguradoras atingidas
        if (!context.MetasPremiacao.Any())
        {
            context.MetasPremiacao.AddRange(
                new Models.MetaPremiacao { QuantidadeMinima = 3,  EhTodas = false, ValorBonus = 100m, Ordem = 1 },
                new Models.MetaPremiacao { QuantidadeMinima = 6,  EhTodas = false, ValorBonus = 100m, Ordem = 2 },
                new Models.MetaPremiacao { QuantidadeMinima = null, EhTodas = true, ValorBonus = 100m, Ordem = 3 }
            );
            context.SaveChanges();
        }

        // Seed: metas de crescimento
        if (!context.MetasCrescimento.Any())
        {
            context.MetasCrescimento.AddRange(
                new Models.MetaCrescimento { Tipo = "Premio",   PercentualMeta = 0.10m, ValorBonus = 100m,  EhEquipe = true  },
                new Models.MetaCrescimento { Tipo = "Premio",   PercentualMeta = 0.15m, ValorBonus = 300m,  EhEquipe = true  },
                new Models.MetaCrescimento { Tipo = "Comissao", PercentualMeta = 0.15m, ValorBonus = 100m,  EhEquipe = false },
                new Models.MetaCrescimento { Tipo = "Comissao", PercentualMeta = 0.20m, ValorBonus = 200m,  EhEquipe = false }
            );
            context.SaveChanges();
        }

        // Seed: cria admin padrão (admin / admin123) se não houver usuários
        if (!context.Usuarios.Any())
        {
            context.Usuarios.Add(new Models.Usuario
            {
                Login = "admin",
                SenhaHash = UsuarioService.HashSenha("admin123"),
                TipoAcesso = Models.TipoAcesso.Administrador,
                Ativo = true
            });
            context.SaveChanges();
        }
    }
}
