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
            if (pct >= 90) return new SolidColorBrush(Color.FromRgb(34, 197, 94));
            if (pct >= 80) return new SolidColorBrush(Color.FromRgb(245, 158, 11));
            return new SolidColorBrush(Color.FromRgb(239, 68, 68));
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
                "Ren.Palma" => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                "Ren.Outro" => new SolidColorBrush(Color.FromRgb(132, 204, 22)),
                "Procurado" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                "Pendente" => new SolidColorBrush(Color.FromRgb(249, 115, 22)),
                "Agendado" => new SolidColorBrush(Color.FromRgb(59, 130, 246)),
                "Não renovado" or "Não renov" => new SolidColorBrush(Color.FromRgb(239, 68, 68)),
                "Novo" or "novo" => new SolidColorBrush(Color.FromRgb(99, 102, 241)),
                "Renovação" => new SolidColorBrush(Color.FromRgb(34, 197, 94)),
                "Prospecção" => new SolidColorBrush(Color.FromRgb(245, 158, 11)),
                _ => Brushes.Gray
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
