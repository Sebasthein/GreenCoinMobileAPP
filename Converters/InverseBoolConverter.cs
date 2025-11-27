using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GreenCoinMovil.Converters
{
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Verifica que el valor sea booleano y lo niega
            if (value is bool b)
            {
                return !b;
            }
            return value; // Retorna el valor original si no es booleano
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Invierte la conversión (doble negación)
            if (value is bool b)
            {
                return !b;
            }
            return value;
        }
    }
}
