using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace RegEditor
{
    public class WidthConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            double width = (double)value;
            return width * (System.Convert.ToDouble(parameter) / 10); 
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            double width = (double)value;
            return width / (System.Convert.ToDouble(parameter) / 10);
        }
    }
}
