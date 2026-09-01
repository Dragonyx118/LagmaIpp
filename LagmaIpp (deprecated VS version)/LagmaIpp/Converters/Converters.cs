using System.Globalization;

namespace LagmaIpp.Converters;

/// <summary>
/// Converte bool → stringa.
/// ConverterParameter: "ValoreTrue|ValoreFalse"
/// </summary>
public class BoolToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && parameter is string p)
        {
            var parts = p.Split('|');
            return b ? parts[0] : (parts.Length > 1 ? parts[1] : "");
        }
        return "";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converte bool → Color.
/// ConverterParameter: "#ColorTrue|#ColorFalse"
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b && parameter is string p)
        {
            var parts = p.Split('|');
            var hex = b ? parts[0] : (parts.Length > 1 ? parts[1] : "#000000");
            return Color.FromArgb(hex);
        }
        return Colors.Black;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converte bool → colore sfondo per indicatori stato (verde/rosso).
/// </summary>
public class BoolToStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is true ? Color.FromArgb("#00C853") : Color.FromArgb("#D50000");

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// Converte int distanza → colore (verde/giallo/rosso).
/// </summary>
public class DistanzaToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string s)
        {
            if (s == "---") return Color.FromArgb("#9E9E9E");
            if (int.TryParse(s.Replace(" cm", ""), out int cm))
            {
                if (cm < 20) return Color.FromArgb("#D50000");
                if (cm < 50) return Color.FromArgb("#FF6F00");
                return Color.FromArgb("#00C853");
            }
        }
        return Color.FromArgb("#9E9E9E");
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}