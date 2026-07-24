using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public class PeriodoPendentesVm
{
    public ResumoImportacao Resumo { get; }
    public List<Renovacao> Pendentes { get; }

    public PeriodoPendentesVm(ResumoImportacao resumo, List<Renovacao> pendentes)
    {
        Resumo = resumo;
        Pendentes = pendentes;
    }

    public decimal TotalPlBase => Pendentes.Sum(r => r.PlBase);
}
