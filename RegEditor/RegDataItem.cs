using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using RegistryClass;

namespace RegEditor
{
    public class RegDataItem : INotifyPropertyChanged
    {
        private string _name { get; set; }
        private string _type { get; set; }
        private RegObject _value { get; set; }
        private string _image { get; set; }
        private string _displayValue { get; set; }

        public string Name
        {
            get { return _name; }
            set
            {
                if (!String.IsNullOrEmpty(value))
                    _name = value;
                else
                    _name = "(Default)";

                this.NotifyPropertyChanged("Name");
            }
        }

        public string Type
        {
            get { return _type; }
            set
            {
                _type = value;

                switch (_type)
                {
                    case "REG_BINARY":
                        _image = "Images/page_bn.png";
                        break;
                    case "REG_MULTI_SZ":
                        _image = "Images/page_mu.png";
                        break;
                    case "REG_SZ":
                        _image = "Images/page_sz.png";
                        break;
                    case "REG_DWORD":
                        _image = "Images/page_dw.png";
                        break;
                    case "REG_QWORD":
                        _image = "Images/page_qw.png";
                        break;
                    case "REG_EXPAND_SZ":
                        _image = "Images/page_ex.png";
                        break;
                    case "REG_DWORD_LITTLE_ENDIAN":
                        _image = "Images/page_dw.png";
                        _type = "REG_DWORD";
                        break;
                    case "REG_QWORD_LITTLE_ENDIAN":
                        _image = "Images/page_qw.png";
                        _type = "REG_QWORD";
                        break;
                    default:
                        _image = "Images/page_white.png";
                        break;
                }
            }
        }

        public RegObject Value
        {
            get { return _value; }
            set
            {
                _value = value;

                if (_value.Type == RegistryHelper.VALUE_TYPE.REG_MULTI_SZ)
                {
                    DisplayValue = _value.StringArrayToShortString(20);
                }
                else if (_value.Type == RegistryHelper.VALUE_TYPE.REG_BINARY)
                {
                    string s = "";
                    if (((byte[])_value.Value).Length > 50)
                        s = "...";
                    DisplayValue = BitConverter.ToString(new RegistryHelper().copyArraySegment((byte[])_value.Value, 50)) + s;
                }
                else
                {
                    DisplayValue = _value.ToString();
                }
            }
        }

        public string Image
        {
            get { return _image; }
            set
            {
                _image = value;
            }
        }

        public string DisplayValue
        {
            get { return _displayValue; }
            set
            {
                _displayValue = value;
                this.NotifyPropertyChanged("DisplayValue");
            }
        }

        #region INotifyPropertyChanged RegDataItem

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
