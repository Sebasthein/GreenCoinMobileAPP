using System;
using System.Globalization;

namespace GreenCoinMovil.Converters
{
    public class NullOrEmptyToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Retorna true si el valor es null o está vacío (para mostrar imagen de fallback)
            if (value is string str)
            {
                return string.IsNullOrEmpty(str);
            }
            return value == null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Conversión inversa no necesaria
            return value;
        }
    }
}