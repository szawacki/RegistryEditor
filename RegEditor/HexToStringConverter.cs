using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace RegEditor
{
    class HexToStringConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            string[] hexStringArray = (value as string).Select(c => c.ToString()).ToArray();
            string returnStr = "";

            for (int i = 0; i < hexStringArray.Length; i++)
            {
                int charInt = Convert.ToInt32(hexStringArray[i] + hexStringArray[i +1], 16);

                //insert dots for special characters, to imitate windows registry binary view
                if (charInt < 30 || (charInt >= 128 && charInt < 160))
                    charInt = 46;

                returnStr += Char.ConvertFromUtf32(charInt);
                i++;
            }

            return returnStr;
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}
