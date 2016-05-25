using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using RegistryClass;

namespace RegEditor
{
    class HexStringToStringConverter : IValueConverter
    {
        object IValueConverter.Convert(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            string byteArrayAsString = (string)value;
            return new RegistryHelper().ByteArrayToString(hexStringToByteArray(byteArrayAsString));
        }

        object IValueConverter.ConvertBack(object value, Type targetType, object parameter,
            System.Globalization.CultureInfo culture)
        {
            return null;
        }


        private byte[] hexStringToByteArray(string hexString)
        {
            string[] stringArray = hexString.Replace("\r\n", "").Split('-');
            List<byte> byteArray = new List<byte>();

            if (hexString.Equals(""))
                return byteArray.ToArray();

            foreach (string strElement in stringArray)
            {
                byteArray.Add(Convert.ToByte(strElement, 16));
            }

            return byteArray.ToArray();
        }
    }
}
