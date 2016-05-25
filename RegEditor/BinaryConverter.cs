using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using RegistryClass;

namespace RegEditor
{
    class BinaryConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            RegObject oReg = (RegObject)value;
            return oReg.getByteArrayToString();
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            RegObject oReg = (RegObject)value;
            return oReg.ToByteArray();
        }
    }
}
