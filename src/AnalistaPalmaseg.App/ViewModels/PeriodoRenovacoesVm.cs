using System.Collections.ObjectModel;
using System.ComponentModel;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public class PeriodoRenovacoesVm : INotifyPropertyChanged
{
    private readonly List<Renovacao> _todas;
    private ObservableCollection<Renovacao> _renovacoes;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ResumoImportacao Resumo { get; }

    public ObservableCollection<Renovacao> Renovacoes
    {
        get => _renovacoes;
        private set
        {
            _renovacoes = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Renovacoes)));
        }
    }

    public PeriodoRenovacoesVm(ResumoImportacao resumo, List<Renovacao> renovacoes)
    {
        Resumo = resumo;
        _todas = renovacoes;
        _renovacoes = new ObservableCollection<Renovacao>(renovacoes);
    }

    public void AplicarFiltro(string texto, string status)
    {
        var query = _todas.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(texto))
            query = query.Where(r =>
                r.Segurado.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                r.Cia.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                r.Ramo.Contains(texto, StringComparison.OrdinalIgnoreCase));

        if (status != "Todos")
            query = query.Where(r => r.Status == status);

        Renovacoes = new ObservableCollection<Renovacao>(query);
    }
}
