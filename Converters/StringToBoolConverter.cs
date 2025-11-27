using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.Converters
{
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Devuelve true si la cadena no es nula, no está vacía, y no solo consiste en espacios en blanco
            return value is string s && !string.IsNullOrWhiteSpace(s);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Conversión inversa no es necesaria para este caso
            throw new NotImplementedException();
        }
    }

}
