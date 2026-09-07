using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace AnalistaPalmaseg.App.Converters;

public class RetencaoColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal pct)
        {
            // Palma brand palette: green / yellow / red
            if (pct >= 90) return new SolidColorBrush(Color.FromRgb(0xA7, 0xCF, 0x45)); // #A7CF45
            if (pct >= 80) return new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0x29)); // #FFCC29
            return new SolidColorBrush(Color.FromRgb(0xEC, 0x32, 0x37));                // #EC3237
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class StatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status switch
            {
                // Palma brand palette
                "Ren.Palma"                     => new SolidColorBrush(Color.FromRgb(0xA7, 0xCF, 0x45)), // brand green
                "Ren.Outro"                     => new SolidColorBrush(Color.FromRgb(0x50, 0xA7, 0xB0)), // brand teal
                "Procurado"                     => new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0x29)), // brand yellow
                "Pendente"                      => new SolidColorBrush(Color.FromRgb(0xF1, 0x70, 0x77)), // brand pink-red
                "Agendado"                      => new SolidColorBrush(Color.FromRgb(0x4E, 0x53, 0x99)), // brand blue
                "Não renovado" or "Não renov"   => new SolidColorBrush(Color.FromRgb(0xEC, 0x32, 0x37)), // brand red
                "Novo" or "novo"                => new SolidColorBrush(Color.FromRgb(0x4E, 0x53, 0x99)), // brand blue
                "Renovação"                     => new SolidColorBrush(Color.FromRgb(0xA7, 0xCF, 0x45)), // brand green
                "Prospecção"                    => new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0x29)), // brand yellow
                _                               => new SolidColorBrush(Color.FromRgb(0x72, 0x73, 0x75))  // neutral gray
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class PercentFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d) return $"{d:F1}%";
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class CurrencyFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d) return d.ToString("C2", new CultureInfo("pt-BR"));
        return value?.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class ApoliceStatusColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string status)
        {
            return status switch
            {
                "Vencida" => new SolidColorBrush(Color.FromRgb(0xEC, 0x32, 0x37)), // brand red
                "Próxima" => new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0x29)), // brand yellow
                "Em dia"  => new SolidColorBrush(Color.FromRgb(0xA7, 0xCF, 0x45)), // brand green
                _         => new SolidColorBrush(Color.FromRgb(0x72, 0x73, 0x75))
            };
        }
        return Brushes.Gray;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToChevronAngleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? 0.0 : -90.0;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

// Returns a white/green/orange brush depending on % atingimento thresholds
public class AtingimentoColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal pct)
        {
            if (pct >= 100) return new SolidColorBrush(Color.FromRgb(0x69, 0xF0, 0xAE)); // verde
            if (pct >= 80)  return new SolidColorBrush(Color.FromRgb(0xFF, 0xCC, 0x29)); // amarelo
            return new SolidColorBrush(Color.FromRgb(0xFF, 0x72, 0x43));                 // laranja/vermelho
        }
        return Brushes.White;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool boolValue = value is bool b && b;
        bool invert = parameter?.ToString() == "Invert";
        if (invert) boolValue = !boolValue;
        return boolValue ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToSimNaoConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "Sim" : "Não";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToCheckKindConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true
            ? MaterialDesignThemes.Wpf.PackIconKind.CheckCircleOutline
            : MaterialDesignThemes.Wpf.PackIconKind.CloseCircleOutline;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BoolToStarKindConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true
            ? MaterialDesignThemes.Wpf.PackIconKind.Star
            : MaterialDesignThemes.Wpf.PackIconKind.StarOutline;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class SeguradoraAbrevConverter : IValueConverter
{
    private static readonly Dictionary<string, string> _mapa = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Porto Seguro"]     = "Porto",
        ["Bradesco Seguros"] = "Bradesco",
        ["Tokio Marine"]     = "Tokio",
        ["HDI Seguros"]      = "HDI",
        ["Liberty Seguros"]  = "Liberty",
        ["Mapfre Seguros"]   = "Mapfre",
        ["Zurich"]           = "Zurich",
        ["SulAmérica"]       = "SulAm.",
        ["Generali"]         = "Generali",
        ["Allianz"]          = "Allianz",
        ["AXA"]              = "AXA",
        ["Chubb"]            = "Chubb",
        ["Pottencial"]       = "Pottenc.",
        ["Sompo"]            = "Sompo",
        ["Excelsior"]        = "Excels.",
        ["Outras"]           = "Outras",
    };

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s)) return value ?? string.Empty;
        foreach (var kv in _mapa)
            if (s.StartsWith(kv.Key, StringComparison.OrdinalIgnoreCase))
                return kv.Value;
        return s.Length > 10 ? s[..10] + "." : s;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class MesFormatConverter : IValueConverter
{
    private static readonly string[] _nomes = ["", "Janeiro", "Fevereiro", "Março", "Abril", "Maio", "Junho",
        "Julho", "Agosto", "Setembro", "Outubro", "Novembro", "Dezembro"];

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int mes && mes >= 1 && mes <= 12)
            return $"{mes} — {_nomes[mes]}";
        return value?.ToString() ?? "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
