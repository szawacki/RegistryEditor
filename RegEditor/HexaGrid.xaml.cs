using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using RegistryClass;
using System.Globalization;
using System.Data;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Media;

namespace RegEditor
{
    /// <summary>
    /// Interaction logic for HexaGrid.xaml
    /// </summary>
    public partial class HexaGrid : UserControl
    {
        public ObservableCollection<TableRowItem> _rowCollection = new ObservableCollection<TableRowItem>();
        private CollectionViewSource _view = new CollectionViewSource();
        private RegObject _oReg;
        private string _hexstring;
        private Regex _expression = new Regex("[A-F0-9_]", RegexOptions.IgnoreCase);
        public int _error = 0;
        private int _caretIndex = 0;

        public HexaGrid()
        {
            InitializeComponent();  
            _view.Source = _rowCollection;
        }

        /// <summary>
        /// Set registry object
        /// </summary>
        public RegObject registryObject
        {
            get
            {
                return _oReg;
            }
            set
            {
                _oReg = value;
                _hexstring = _oReg.getByteArrayToHexString();
                this.hexStringToGrid();
            }
        }

        /// <summary>
        /// Set content of registry object to grid
        /// </summary>
        public void hexStringToGrid()
        {
            int j = 0;
            string[] hexStringArray = _hexstring.Split('-');

            for (int i = 0; i < hexStringArray.Length; i += 8)
            {
                TableRowItem trItem = new TableRowItem();
                trItem.Num = (j * 8).ToString("X4");
                string asciiString = "";

                for (int k = 0; k <= 7 & i + k < hexStringArray.Length; k++)
                {
                    switch (k + 1)
                    {
                        case 1:
                            trItem.STR01 = hexStringArray[i + k].ToString();
                            asciiString += trItem.STR01;
                            break;
                        case 2:
                            trItem.STR02 = hexStringArray[i + k].ToString();
                            asciiString += trItem.STR02;
                            break;
                        case 3:
                            trItem.STR03 = hexStringArray[i + k].ToString();
                            asciiString += trItem.STR03;
                            break;
                        case 4:
                            trItem.STR04 = hexStringArray[i + k].ToString();
                            asciiString += trItem.STR04;
                            break;
                        case 5:
                            trItem.STR05 = hexStringArray[i + k].ToString();
                            asciiString += trItem.STR05;
                            break;
                        case 6:
                            trItem.STR06 = hexStringArray[i + k].ToString();
                            asciiString += trItem.STR06;
                            break;
                        case 7:
                            trItem.STR07 = hexStringArray[i + k].ToString();
                            asciiString += trItem.STR07;
                            break;
                        case 8:
                            trItem.STR08 = hexStringArray[i + k].ToString();
                            asciiString += trItem.STR08;
                            break;

                    }
                }

                trItem.STR = asciiString;
                _rowCollection.Add(trItem);
                j++;
            }

            HexaDataGrid.ItemsSource = _view.View;
        }

        #region Converter

        /// <summary>
        /// Convert hexadecimal string to string
        /// </summary>
        /// <param name="hex">string</param>
        /// <returns>string</returns>
        private string hexStringToString(string hex)
        {
            int value = Convert.ToInt32(hex, 16);
            // TODO : Special signs not displayed properly (-)
            //string c = Char.ConvertFromUtf32(value);
            //if (hex.Equals("AD"))
            //    hex = "AD";

            //insert dots for special characters, to imitate windows registry binary view
            if (value < 30 || (value >= 128 && value < 160))
                value = 46;

            return Char.ConvertFromUtf32(value);
        }

        /// <summary>
        /// Converts string into byte array
        /// </summary>
        /// <param name="str">text string</param>
        /// <returns>byte[]</returns>
        private byte[] StringToByteArray(string str)
        {
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            return enc.GetBytes(str);
        }

        /// <summary>
        /// Converts byte array into hexdeciaml string
        /// </summary>
        /// <param name="value">byte array</param>
        /// <returns>string</returns>
        public string getByteArrayToHexString(byte[] value)
        {
            return BitConverter.ToString(value);
        }

        /// <summary>
        /// Convert string to hex string
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private string ConvertStringToHex(string value)
        {
            string[] hexStringArray = value.Select(c => c.ToString()).ToArray();
            string returnStr = "";

            foreach (string str in hexStringArray)
            {
                returnStr += this.getByteArrayToHexString(this.StringToByteArray(str));
            }

            return returnStr;
        }

        public byte ConvertHexValueToByte(string hexValue)
        {
            return byte.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);
        }

        #endregion

        #region Event handlers

        /// <summary>
        /// Cell editing end event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGrid1_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // Disable key press event handlers
            HexaDataGrid.PreviewKeyDown -= new KeyEventHandler(dataGrid1_PreviewKeyDown);
            HexaDataGrid.KeyUp -= new KeyEventHandler(dataGrid1_KeyUp);
        }

        /// <summary>
        /// Begin edit cell event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGrid1_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            // Enable key press event handlers
            HexaDataGrid.KeyUp += new KeyEventHandler(dataGrid1_KeyUp);
            HexaDataGrid.PreviewKeyDown += new KeyEventHandler(dataGrid1_PreviewKeyDown);
        }

        /// <summary>
        /// Preview key down event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void dataGrid1_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox tb = e.OriginalSource as TextBox;
            
            if (((DataGridCellInfo)((sender as DataGrid).CurrentCell)).Column.Header.Equals("str"))
            {
                if (_caretIndex == 0)
                    _caretIndex = tb.CaretIndex;
            }
            else
            {
                tb.MaxLength = 2;
                // Avoid type of ilegal characters
                if (!_expression.IsMatch(e.Key.ToString()) && e.Key != Key.Right)
                {
                    e.Handled = true;
                    SystemSounds.Beep.Play();
                }
            }

            // Enable cell editing end event handler
            HexaDataGrid.CellEditEnding += new EventHandler<DataGridCellEditEndingEventArgs>(dataGrid1_CellEditEnding);
        }

        /// <summary>
        /// Key up event handler
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void dataGrid1_KeyUp(object sender, KeyEventArgs e)
        {
            DataGrid dg = sender as DataGrid;
            DataGridColumn col = ((DataGridCellInfo)dg.CurrentCell).Column;
            TextBox tb = e.OriginalSource as TextBox;
            TableRowItem item = tb.DataContext as TableRowItem;

            if (e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Down)
            {
                e.Handled = true;
                _caretIndex = 0;
                return;
            }

            if (!((DataGridCellInfo)((sender as DataGrid).CurrentCell)).Column.Header.Equals("str"))
            {
                DataGridCell dgc = tb.Parent as DataGridCell;
                tb.Text = (e.OriginalSource as TextBox).Text.ToUpper();              
                string value = tb.Text;

                if (value.Equals("") || value.Length < 2)
                {
                    //Mark hex field red on illegal content
                    e.Handled = true;
                    SystemSounds.Beep.Play();
                    _error = 1;
                    dgc.Background = Brushes.Red;
                    dgc.BorderBrush = Brushes.Red;
                    return;
                }

                int index = col.DisplayIndex * 2 - 2;
                item.STR = item.STR.Remove(index, 2).Insert(index, value);
                dgc.Background = Brushes.White;
                dgc.BorderBrush = Brushes.White;
                _error = 0;
            }
            else
            {
                e.Handled = true;
                int textBoxCaretIndex = tb.CaretIndex;
                string text = this.ConvertStringToHex(tb.Text.Substring(_caretIndex, textBoxCaretIndex - _caretIndex));
                int skipNumber = _rowCollection.IndexOf(item);
                int takeNumber = _rowCollection.Count - skipNumber;
                item.STR = item.STR.Insert(_caretIndex * 2, text);
                _caretIndex = 0;
                string exString = "";

                foreach (TableRowItem tri in _rowCollection.Skip(skipNumber).Take(takeNumber))
                    exString += tri.STR;

                if (item.STR.Length != 16)
                {
                    exString = this.updatetableRowCollection(exString, skipNumber, takeNumber, item.STR.Length);

                    if (exString.Length > 0)
                    {
                        TableRowItem newTri = new TableRowItem();
                        newTri.STR = exString.Substring(0, exString.Length);
                        newTri.Num = (_rowCollection.Count * 8).ToString("X4");
                        this.setHexValue(newTri, newTri.STR);
                        _rowCollection.Add(newTri);
                    }
                }
                else
                {
                    this.setHexValue(item, text);
                }

                tb.CaretIndex += textBoxCaretIndex;

                if (text.Length > 2)
                    HexaDataGrid.CancelEdit();
            }  
        }

        #endregion

        #region Helper

        /// <summary>
        /// Set hex value fields of table row items according to string field
        /// </summary>
        /// <param name="item"></param>
        /// <param name="text"></param>
        private void setHexValue(TableRowItem item, string text)
        {
            for (int i = 0; i < text.Length / 2; i++)
            {
                switch (i)
                {
                    case 0:
                        item.STR01 = text.Substring(0, 2);
                        break;
                    case 1:
                        item.STR02 = text.Substring(2, 2);
                        break;
                    case 2:
                        item.STR03 = text.Substring(4, 2);
                        break;
                    case 3:
                        item.STR04 = text.Substring(6, 2);
                        break;
                    case 4:
                        item.STR05 = text.Substring(8, 2);
                        break;
                    case 5:
                        item.STR06 = text.Substring(10, 2);
                        break;
                    case 6:
                        item.STR07 = text.Substring(12, 2);
                        break;
                    case 7:
                        item.STR08 = text.Substring(14, 2);
                        break;
                    default:
                        break;
                }
               
            }
        }

        /// <summary>
        /// Update RowTableCollection on change of string element
        /// </summary>
        /// <param name="hexString"></param>
        /// <param name="skipNumber"></param>
        /// <param name="takeNumber"></param>
        /// <param name="textLength"></param>
        /// <returns></returns>
        private string updatetableRowCollection(string hexString, int skipNumber, int takeNumber, int textLength)
        {
            foreach (TableRowItem tri in _rowCollection.Skip(skipNumber).Take(takeNumber))
            {
                if (hexString.Length > 16)
                {
                    tri.STR = hexString.Substring(0, 16);
                    hexString = hexString.Remove(0, 16);
                }
                else
                {
                    tri.STR = hexString.Substring(0, hexString.Length);
                    hexString = hexString.Remove(0, hexString.Length);

                    if (textLength < 16)
                    {
                        if (tri.STR.Equals(""))
                            _rowCollection.RemoveAt(_rowCollection.Count - 1);
                    }
                }

                this.setHexValue(tri, tri.STR);
            }

            return hexString;
        }

        #endregion
    }

    /// <summary>
    /// TableRowItewm class
    /// </summary>
    public class TableRowItem : INotifyPropertyChanged
    {
        private string _num { get; set; }
        private string _str01 { get; set; }
        private string _str02 { get; set; }
        private string _str03 { get; set; }
        private string _str04 { get; set; }
        private string _str05 { get; set; }
        private string _str06 { get; set; }
        private string _str07 { get; set; }
        private string _str08 { get; set; }
        private string _str { get; set; }

        public string Num
        {
            get { return _num; }
            set
            {
                _num = value;
                this.NotifyPropertyChanged("Num");
            }
        }

        public string STR01
        {
            get { return _str01; }
            set
            {
                _str01 = value;
                this.NotifyPropertyChanged("STR01");
            }
        }

        public string STR02
        {
            get { return _str02; }
            set
            {
                _str02 = value;
                this.NotifyPropertyChanged("STR02");
            }
        }

        public string STR03
        {
            get { return _str03; }
            set
            {
                _str03 = value;
                this.NotifyPropertyChanged("STR03");
            }
        }

        public string STR04
        {
            get { return _str04; }
            set
            {
                _str04 = value;
                this.NotifyPropertyChanged("STR04");
            }
        }

        public string STR05
        {
            get { return _str05; }
            set
            {
                _str05 = value;
                this.NotifyPropertyChanged("STR05");
            }
        }

        public string STR06
        {
            get { return _str06; }
            set
            {
                _str06 = value;
                this.NotifyPropertyChanged("STR06");
            }
        }

        public string STR07
        {
            get { return _str07; }
            set
            {
                _str07 = value;
                this.NotifyPropertyChanged("STR07");
            }
        }

        public string STR08
        {
            get { return _str08; }
            set
            {
                _str08 = value;
                this.NotifyPropertyChanged("STR08");
            }
        }

        public string STR
        {
            get { return _str; }
            set
            {
                _str = value;
                this.NotifyPropertyChanged("STR");
            }
        }

        #region INotifyPropertyChanged TableRowItem

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Notify on property change
        /// </summary>
        /// <param name="propertyName"></param>
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
