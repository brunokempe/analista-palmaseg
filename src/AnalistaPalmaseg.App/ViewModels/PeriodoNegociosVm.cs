using System.Collections.ObjectModel;
using System.ComponentModel;
using AnalistaPalmaseg.Core.Models;
using AnalistaPalmaseg.Core.Services;

namespace AnalistaPalmaseg.App.ViewModels;

public class PeriodoNegociosVm : INotifyPropertyChanged
{
    private readonly List<NovoNegocio> _todos;
    private ObservableCollection<NovoNegocio> _negocios;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ResumoImportacao Resumo { get; }

    public ObservableCollection<NovoNegocio> Negocios
    {
        get => _negocios;
        private set
        {
            _negocios = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Negocios)));
        }
    }

    public PeriodoNegociosVm(ResumoImportacao resumo, List<NovoNegocio> negocios)
    {
        Resumo = resumo;
        _todos = negocios;
        _negocios = new ObservableCollection<NovoNegocio>(negocios);
    }

    public void AplicarFiltro(string texto, string status)
    {
        var query = _todos.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(texto))
            query = query.Where(n =>
                n.Segurado.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                n.Cia.Contains(texto, StringComparison.OrdinalIgnoreCase) ||
                n.Segmento.Contains(texto, StringComparison.OrdinalIgnoreCase));

        if (status != "Todos")
            query = query.Where(n => n.Status == status);

        Negocios = new ObservableCollection<NovoNegocio>(query);
    }
}
