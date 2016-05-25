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
using RegistryClass;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Collections;
using System.Windows.Controls.Primitives;

namespace RegEditor
{
    /// <summary>
    /// Interaction logic for RegTab.xaml
    /// </summary>
    public partial class RegTab : UserControl
    {
        public RegistryHelper reg;
        public MainWindow objParent { get; set; }
        private object _dummyNode = null;
        private string remoteHost;
        private RegKey rightClickRegObject;
        // TODO : Check need for variable rightClickTreeViewItem
        private TreeViewItem rightClickTreeViewItem;
        //public string regObjectType = "";
        private ObservableCollection<RegDataItem> _regDataItems = new ObservableCollection<RegDataItem>();
        private CollectionViewSource view = new CollectionViewSource();
        //public static RoutedCommand myCommand = new RoutedCommand();

        public RegTab(string remoteHost, MainWindow parent)
        {
            InitializeComponent();
            this.remoteHost = remoteHost;
            this.objParent = parent;
            this.reAddHandler();
            LoadRootNodes();
            view.Source = _regDataItems;
            this.dataGrid_1.ItemsSource = view.View;
            //myCommand.InputGestures.Add(new KeyGesture(Key.E, ModifierKeys.Control));
        }

        public void reAddHandler()
        {
            this.treeview_1.AddHandler(TreeViewItem.ExpandedEvent, new RoutedEventHandler(OnTreeItemExpanded));
        }

        //http://www.codeproject.com/Articles/43088/LazyLoad-WPF-Treeview-with-Large-Amount-of-Two-Lev

        private void treeview_1_Selected(object sender, RoutedEventArgs e)
        {
            TreeViewItem tvi = e.OriginalSource as TreeViewItem;
            if (tvi != null)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                RegKey rit = (RegKey)tvi.Tag;
                RegKey regkey = new RegKey(rit.oParentObject, rit.RootKey, rit.SubKey, true, false);

                _regDataItems.Clear();

                foreach(RegObject oReg in regkey.Values)
                {
                    _regDataItems.Add(                    
                        new RegDataItem
                        {
                            Name = oReg.KeyName,
                            Type = oReg.Type.ToString(),
                            Value = oReg,
                        }
                    );
                }
                
                if (_regDataItems.Where(item => item.Name.Equals("(Default)")).Count() == 0)
                {
                    RegObject oreg = new RegObject();
                    oreg.Type = RegistryHelper.VALUE_TYPE.REG_SZ;
                    oreg.KeyName = "";
                    oreg.Value = null;

                    _regDataItems.Add(
                        new RegDataItem
                          {
                              Name = oreg.KeyName,
                              Type = oreg.Type.ToString(),
                              Value = oreg,
                          });
                }

                view.View.SortDescriptions.Add(new SortDescription("Name", ListSortDirection.Ascending));
                //_regDataItems.OrderBy(item => item.Name); // .Sort(delegate(RegDataItem p1, RegDataItem p2) { return p1.Name.CompareTo(p2.Name); });
                Mouse.OverrideCursor = null;
            }
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            this.treeview_1.RemoveHandler(TreeViewItem.ExpandedEvent,
              new RoutedEventHandler(OnTreeItemExpanded));
        }

        private void OnTreeItemExpanded(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TreeViewItem item = e.OriginalSource as TreeViewItem;
            if (item != null) // && item.Items.Count == 1 && item.Items[0] == _dummyNode
            {
                item.Items.Clear();
                RegKey rit = (RegKey)item.Tag;
                RegKey newRegITM = new RegKey(rit.oParentObject, rit.RootKey ,rit.SubKey, false, true);

                foreach (RegKey regKey in newRegITM.SubKeys)
                {
                    TreeViewItem subItem = new TreeViewItem();
                    subItem.Header = regKey.KeyName;
                    //TODO : Improvement, use of DataContext allows Templatebinding
                    subItem.Tag = regKey;
                    if (regKey.hasSubItems)
                        subItem.Items.Add(_dummyNode);
                    item.Items.Add(subItem);
                }

                item.Items.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));
            }
            Mouse.OverrideCursor = null;
        }

        /// <summary>
        /// Load root nodes and display in Treeview
        /// </summary>
        private void LoadRootNodes()
        {
            if (!String.IsNullOrEmpty(remoteHost) && !remoteHost.Equals("localhost"))
                reg = new RegistryHelper(remoteHost);
            else
                reg = new RegistryHelper();

            foreach (RegistryHelper.ROOT_KEY rootKey in Enum.GetValues(typeof(RegistryHelper.ROOT_KEY)))
            {
                if (reg.KeyExists(rootKey, ""))
                {
                    if (!remoteHost.Equals("localhost") && (rootKey.ToString().Equals("HKEY_CURRENT_USER") || rootKey.ToString().Equals("HKEY_CLASSES_ROOT")))
                        continue;

                    RegKey it = new RegKey(reg, rootKey, "", false, false);
                    TreeViewItem item = new TreeViewItem();
                    item.Tag = it;
                    item.Header = it.RootKey.ToString();
                    item.Items.Add(_dummyNode);
                    this.treeview_1.Items.Add(item);
                }
            }
        }

        /// <summary>
        /// ReAdd handler on foces change of UserControl
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_GotFocus(object sender, RoutedEventArgs e)
        {
            this.reAddHandler();
        }

        #region TreeView menu item event handler

        /// <summary>
        /// Copy registry hive to clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Copy_Click(object sender, RoutedEventArgs e)
        {
            this.setClipboradKey();
            this.objParent.deleteKeyAfterPaste = false;
        }

        /// <summary>
        /// Cut registry hive to clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Cut_Click(object sender, RoutedEventArgs e)
        {
            this.setClipboradKey();
            this.objParent.cutTreeViewItem = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget as TreeViewItem;
            this.objParent.deleteKeyAfterPaste = true;
        }

        /// <summary>
        /// Paste registry hive from clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Paste_Click(object sender, RoutedEventArgs e)
        {
            RegKey newRegKey = this.objParent.clipboardRegKey;

            if (newRegKey == null)
                return;

            bool success = this.pasteRegistryHive(newRegKey, newRegKey.KeyName);

            if (success)
            {
                if (this.objParent.deleteKeyAfterPaste)
                    this.Treeview_MenuItem_Delete_Click(null, null);

                this.objParent.deleteKeyAfterPaste = false;
                this.objParent.cutTreeViewItem = null;
                TreeViewItem newTvi = new TreeViewItem();
                newTvi.Header = newRegKey.KeyName;
                newRegKey.SubKey = this.rightClickRegObject.SubKey + @"\" + newRegKey.KeyName;
                newRegKey.RootKey = this.rightClickRegObject.RootKey;
                newTvi.Tag = newRegKey;

                if (newRegKey.hasSubItems)
                    newTvi.Items.Add(_dummyNode);

                TreeViewItem tvi = ((e.OriginalSource as MenuItem).Parent as ContextMenu).PlacementTarget as TreeViewItem;
                tvi.Items.Add(newTvi);
                tvi.Items.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));
                newTvi.IsSelected = true;
            }
            else
            {
                MessageBox.Show("Unable to paste key.", "RegEditor Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Delete registry hive
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Treeview_MenuItem_Delete_Click(object sender, RoutedEventArgs e)
        {
            RegKey regKey = null;
            RegKey delRegKey = null;
            bool result = false;
            //Check for paste action, if true the regKey to clipped RegKey
            if (this.objParent.deleteKeyAfterPaste)
                regKey = this.objParent.clipboardRegKey;

            if (regKey == null)
            {
                regKey = this.rightClickRegObject;
                if (regKey == null)
                    return;
            }

            if (this.objParent.deleteKeyAfterPaste)
            {
                delRegKey = this.objParent.cutTreeViewItem.Tag as RegKey;
                result = new RegistryHelper(delRegKey.oParentObject.getHostname()).DeleteKey(delRegKey.RootKey, delRegKey.SubKey);
            }
            else
            {
                result = new RegistryHelper(regKey.oParentObject.getHostname()).DeleteKey(regKey.RootKey, regKey.SubKey);
            }

            if (result)
            {
                TreeViewItem tvi = null;

                if (sender == null)
                    tvi = this.objParent.cutTreeViewItem;
                else
                    tvi = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget as TreeViewItem;

                (tvi.Parent as TreeViewItem).Items.Remove(tvi);
            }
            else
            {
                MessageBox.Show("Unable to delete key.", "RegEditor Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Add registry hive to favorites
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Add_Click(object sender, RoutedEventArgs e)
        {
            //RegKey reg = new RegKey(this.rightClickRegObject.oParentObject, this.rightClickRegObject.RootKey, this.rightClickRegObject.SubKey, true, true, true);
            if (this.rightClickRegObject == null)
                return;

            MenuItem mit = new MenuItem();
            mit.Click += new RoutedEventHandler(this.objParent.mit_Click);
            mit.Header = this.rightClickRegObject.oParentObject.rootKeyName.Replace("_", "__") + @"\" + this.rightClickRegObject.SubKey;

            mit.Icon = new System.Windows.Controls.Image
            {
                Source = new BitmapImage(new Uri("Images/star.png", UriKind.Relative))
            };
            mit.ContextMenu = this.objParent.ctmenu(mit);

            foreach (object item in this.objParent.menu_fav.Items)
            { 
                MenuItem mi = item as MenuItem;
                if (mi.Header.ToString().Equals(mit.Header.ToString()))
                    return;
            }
                this.objParent.menu_fav.Items.Add(mit);
                this.objParent.favorites.Add(mit.Header.ToString());
                this.objParent.saveFavorites();
        }

        /// <summary>
        /// Set selected TreeViewItem and RegistryObject on right click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void treeview_1_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Source.GetType() == typeof(TreeViewItem))
            {
                this.rightClickTreeViewItem = e.Source as TreeViewItem; ;
                this.rightClickRegObject = ((TreeViewItem)e.Source).Tag as RegKey;
            }
            else
            {
                this.rightClickRegObject = null;
                this.rightClickTreeViewItem = null;
            }
        }

        /// <summary>
        /// Create new registry hive on button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_CreateKey_Click(object sender, RoutedEventArgs e)
        {
            if (this.rightClickRegObject == null)
                return;

            Button btn = sender as Button;
            string name = "";
            StackPanel sp = btn.Parent as StackPanel;
            ContextMenu ctm = ((sp.TemplatedParent as MenuItem).Parent as MenuItem).Parent as ContextMenu;
            ctm.IsOpen = false;
            foreach (UIElement element in sp.Children)
            {
                if (element.GetType() == typeof(TextBox))
                {
                    name = (element as TextBox).Text;
                    break;
                }
            }

            if (String.IsNullOrEmpty(name))
                return;

            ArrayList subKeyList = new RegistryHelper(this.rightClickRegObject.oParentObject.getHostname()).EnumKeys(this.rightClickRegObject.RootKey, this.rightClickRegObject.SubKey);

            if (subKeyList.Contains(name))
                return;

            bool result = new RegistryHelper(this.rightClickRegObject.oParentObject.getHostname()).CreateKey(this.rightClickRegObject.RootKey, this.rightClickRegObject.SubKey + @"\" + name); //this.rightClickRegObject.oParentObject.getHostname()

            if (result)
            {
                RegKey newRegKey = new RegKey(this.rightClickRegObject.oParentObject, this.rightClickRegObject.RootKey, this.rightClickRegObject.SubKey + @"\" + name, false, false);
                TreeViewItem tvi = new TreeViewItem();
                tvi.Header = name;
                tvi.Tag = newRegKey;
                this.rightClickTreeViewItem.Items.Add(tvi);
                this.rightClickTreeViewItem.Items.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));
                tvi.IsSelected = true;
            }
            else
            {
                MessageBox.Show("Unable to create key.", "RegEditor Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Create new registry hive on textbox press enter
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_CreateKey_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Button btn = new Button();
                TextBox tb = sender as TextBox;
                StackPanel sp = tb.Parent as StackPanel;

                foreach (UIElement element in sp.Children)
                {
                    if (element.GetType() == typeof(Button))
                    {
                        btn = (element as Button);
                        break;
                    }
                }

                this.btn_CreateKey_Click(btn, null);
            }
        }

        #endregion

        /// <summary>
        /// Create new key on enter in textbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextBox_CreateValue_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Button btn = new Button();
                TextBox tb = sender as TextBox;
                StackPanel sp = tb.Parent as StackPanel;

                foreach (UIElement element in sp.Children)
                {
                    if (element.GetType() == typeof(Button))
                    {
                        btn = (element as Button);
                        break;
                    }
                }

                this.btn_CreateValue_Click(btn, null);
            }
        }

        /// <summary>
        /// Create new key on button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_CreateValue_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            string name = "";
            StackPanel sp = btn.Parent as StackPanel;
            ContextMenu ctm = ((sp.TemplatedParent as MenuItem).Parent as MenuItem).Parent as ContextMenu;
            ctm.IsOpen = false;
            foreach (UIElement element in sp.Children)
            {
                if (element.GetType() == typeof(TextBox))
                {
                    name = (element as TextBox).Text;
                    break;
                }
            }

            if (String.IsNullOrEmpty(name))
                return;

            DataGrid dg = ctm.PlacementTarget as DataGrid;
            string type = "";

            foreach (MenuItem item in ctm.Items)
            {
                if (item.IsHighlighted)
                {
                    type = item.Header.ToString();
                    break;
                }
            }

            ListCollectionView lcv = dg.ItemsSource as ListCollectionView;
            IEnumerable<RegDataItem> iNum = lcv.SourceCollection as IEnumerable<RegDataItem>;
            
            RegDataItem rdi = iNum.ElementAt(0) as RegDataItem;
            RegObject oReg = rdi.Value;

            RegObject newOReg = new RegObject();
            newOReg.RootKey = oReg.RootKey;
            newOReg.KeyName = name;
            newOReg.Type = this.getTypeFromString(type);
            newOReg.SubKey = oReg.SubKey;

            switch (newOReg.Type)
            {
                case RegistryHelper.VALUE_TYPE.REG_BINARY:
                    newOReg.Value = new byte[] {};
                    break;
                case RegistryHelper.VALUE_TYPE.REG_MULTI_SZ:
                    newOReg.Value = new string[] {};
                    break;
                default:
                    newOReg.Value = null;
                    break;
            }

            List<RegDataItem> testList = iNum.ToList<RegDataItem>();
            bool found = false;

            foreach (RegDataItem item in testList)
            {
                if (item.Name.Equals(newOReg.KeyName))
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                MessageBox.Show("Key exists already.", "RegEditor Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            bool result = new RegistryHelper().WriteValue(newOReg.RootKey, newOReg.SubKey, newOReg.Type, newOReg.KeyName, newOReg.Value);

            if (!result)
            {
                MessageBox.Show("Unable to create key.", "RegEditor Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            RegDataItem newRdi = new RegDataItem();

            this._regDataItems.Add(new RegDataItem
            {
                Name = newOReg.KeyName,
                Type = newOReg.Type.ToString(),
                Value = newOReg,
            });

        }

        //TODO : Check DWORD QWORD writing (value length)
        #region Submenu save button functions

        /// <summary>
        /// Save binary to registry
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Binary_Edit_OK_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            RegDataItem Item = btn.DataContext as RegDataItem;
            RegObject oReg = Item.Value;
            StackPanel spParent = (btn.Parent as StackPanel).Parent as StackPanel;
            HexaGrid hGrid = null;
            List<byte> byteList = new List<byte>();

            foreach (UIElement element in spParent.Children)
            {
                if (element.GetType() == typeof(StackPanel))
                {
                    StackPanel sp2 = element as StackPanel;

                    foreach (UIElement ele in sp2.Children)
                    {
                        if (ele.GetType() == typeof(HexaGrid))
                        {
                            hGrid = ele as HexaGrid;
                            break;
                        }
                    }

                    break;
                }
            }

            foreach (TableRowItem tri in hGrid._rowCollection)
            {
                if (!String.IsNullOrEmpty(tri.STR01)) byteList.Add(hGrid.ConvertHexValueToByte(tri.STR01));
                if (!String.IsNullOrEmpty(tri.STR02)) byteList.Add(hGrid.ConvertHexValueToByte(tri.STR02));
                if (!String.IsNullOrEmpty(tri.STR03)) byteList.Add(hGrid.ConvertHexValueToByte(tri.STR03));
                if (!String.IsNullOrEmpty(tri.STR04)) byteList.Add(hGrid.ConvertHexValueToByte(tri.STR04));
                if (!String.IsNullOrEmpty(tri.STR05)) byteList.Add(hGrid.ConvertHexValueToByte(tri.STR05));
                if (!String.IsNullOrEmpty(tri.STR06)) byteList.Add(hGrid.ConvertHexValueToByte(tri.STR06));
                if (!String.IsNullOrEmpty(tri.STR07)) byteList.Add(hGrid.ConvertHexValueToByte(tri.STR07));
                if (!String.IsNullOrEmpty(tri.STR08)) byteList.Add(hGrid.ConvertHexValueToByte(tri.STR08));
            }

            //oReg.Value = byteList.ToArray();
            this.writeValuesToRgistry(Item, oReg, byteList.ToArray());
        }

        /// <summary>
        /// Save string to registry
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_String_Edit_OK_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            RegDataItem Item = btn.DataContext as RegDataItem;
            StackPanel spParent = (btn.Parent as StackPanel).Parent as StackPanel;
            TextBox tb = null;

            foreach (UIElement element in spParent.Children)
            {
                if (element.GetType() == typeof(TextBox))
                {
                    tb = element as TextBox;
                    break;
                }
            }

            RegObject oReg = Item.Value;
            //oReg.Value = tb.Text;
            this.writeValuesToRgistry(Item, oReg, tb.Text);
        }

        /// <summary>
        /// Save multi string to registry
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Multi_Edit_OK_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            RegDataItem Item = btn.DataContext as RegDataItem;
            StackPanel spParent = (btn.Parent as StackPanel).Parent as StackPanel;
            RichTextBox rtb = null;

            foreach (UIElement element in spParent.Children)
            {
                if (element.GetType() == typeof(RichTextBox))
                {
                    rtb = element as RichTextBox;
                    break;
                }
            }

            RegObject oReg = Item.Value;

            var textRange = new TextRange(rtb.Document.ContentStart, rtb.Document.ContentEnd);
            string[] lines = textRange.Text.Split('\r');
            //oReg.Value = lines;
            this.writeValuesToRgistry(Item, oReg, lines);
        }

        /// <summary>
        /// Save DWORD and QWORD to registry
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_DW_Edit_OK_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            RegDataItem Item = btn.DataContext as RegDataItem;
            StackPanel spParent = (btn.Parent as StackPanel).Parent as StackPanel;
            TextBox tb = null;
            RadioButton rbt = null;

            foreach (UIElement element in spParent.Children)
            {
                if (element.GetType() == typeof(StackPanel))
                {
                    StackPanel sp = element as StackPanel;
                    foreach (UIElement ele in sp.Children)
                    {
                        if (ele.GetType() == typeof(TextBox))
                        {
                            tb = ele as TextBox;
                        }

                        if (ele.GetType() == typeof(StackPanel))
                        {
                            StackPanel sp2 = ele as StackPanel;

                            foreach (UIElement el in sp2.Children)
                            {
                                if (el.GetType() == typeof(RadioButton))
                                {
                                    RadioButton bt = el as RadioButton;
                                    if (bt.Name.Equals("rbtn_decimal"))
                                    {
                                        rbt = bt;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    break;
                }
            }

            RegObject oReg = Item.Value;
            //oReg.Value = this.setDwordQword(rbt, tb.Text);
            this.writeValuesToRgistry(Item, oReg, this.setDwordQword(rbt, tb.Text));
        }

        /// <summary>
        /// Save expand string to registry
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Expand_Edit_OK_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            RegDataItem Item = btn.DataContext as RegDataItem;
            StackPanel spParent = (btn.Parent as StackPanel).Parent as StackPanel;
            TextBox tb = null;

            foreach (UIElement element in spParent.Children)
            {
                if (element.GetType() == typeof(TextBox))
                {
                    tb = element as TextBox;
                    break;
                }
            }

            RegObject oReg = Item.Value;
            //oReg.Value = tb.Text;
            this.writeValuesToRgistry(Item, oReg, tb.Text);
        }

        #endregion

        #region Submenu slider events
        /// <summary>
        /// Load saved slider psotion an initializing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Slider_Initialized(object sender, EventArgs e)
        {
            Slider slide = sender as Slider;
            switch (slide.Name)
            {
                case "bn_Slider":
                    slide.Value = Convert.ToDouble(this.objParent.bnSlidePos);
                    break;
                case "multi_Slider":
                    slide.Value = Convert.ToDouble(this.objParent.mulSlidePos);
                    break;
                case "str_Slider":
                    slide.Value = Convert.ToDouble(this.objParent.strSlidePos);
                    break;
                case "dw_Slider":
                    slide.Value = Convert.ToDouble(this.objParent.dwSlidePos);
                    break;
                case "ex_Slider":
                    slide.Value = Convert.ToDouble(this.objParent.exSlidePos);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Set slider postion and save on visible state change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rtb_Slider_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Slider slide = sender as Slider;

            if (slide.IsVisible)
            {
                double value = 0;

                switch (slide.Name)
                {
                    case "bn_Slider":
                        value = Convert.ToDouble(this.objParent.bnSlidePos);
                        break;
                    case "multi_Slider":
                        value = Convert.ToDouble(this.objParent.mulSlidePos);
                        break;
                    case "str_Slider":
                        value = Convert.ToDouble(this.objParent.strSlidePos);
                        break;
                    case "dw_Slider":
                        value = Convert.ToDouble(this.objParent.dwSlidePos);
                        break;
                    case "ex_Slider":
                        value = Convert.ToDouble(this.objParent.exSlidePos);
                        break;
                    default:
                        break;
                }

                slide.Value = value;
            }

            if (!slide.IsVisible)
            {
                new RegistryHelper().WriteValue(RegistryHelper.ROOT_KEY.HKEY_CURRENT_USER, @"SOFTWARE\RegEditor", RegistryHelper.VALUE_TYPE.REG_DWORD, slide.Name, Convert.ToInt32(slide.Value));

                switch (slide.Name)
                {
                    case "bn_Slider":
                        this.objParent.bnSlidePos = slide.Value;
                        break;
                    case "multi_Slider":
                        this.objParent.mulSlidePos = slide.Value;
                        break;
                    case "str_Slider":
                        this.objParent.strSlidePos = slide.Value;
                        break;
                    case "dw_Slider":
                        this.objParent.dwSlidePos = slide.Value;
                        break;
                    case "ex_Slider":
                        this.objParent.exSlidePos = slide.Value;
                        break;
                    default:
                        break;
                }
                
            }
        }
        #endregion

        #region Submenu open / close events
        /// <summary>
        /// Set Submenu template on open
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            RegDataItem regItem = mi.DataContext as RegDataItem;
            //this.regObjectType = regItem.Type.ToString();
            mi.Items.RemoveAt(0);
            MenuItem newMI = new MenuItem();
            newMI.StaysOpenOnClick = true;

            switch (regItem.Type.ToString())
            {
                case "REG_SZ":
                    newMI.Template = FindResource("MenuItemEditStirngTemplate") as ControlTemplate;
                    break;
                case "REG_BINARY":
                    newMI.Template = FindResource("MenuItemEditBinaryTemplate") as ControlTemplate;
                    break;
                case "REG_DWORD":
                    newMI.Template = FindResource("MenuItemEditDWORDQWORDTemplate") as ControlTemplate;
                    break;
                case "REG_QWORD":
                    newMI.Template = FindResource("MenuItemEditDWORDQWORDTemplate") as ControlTemplate;
                    break;
                case "REG_EXPAND_SZ":
                    newMI.Template = FindResource("MenuItemEditExpandStirngTemplate") as ControlTemplate;
                    break;
                case "REG_MULTI_SZ":
                    newMI.Template = FindResource("MenuItemEditMultiTemplate") as ControlTemplate;
                    break;
                default:
                    newMI.Template = null;
                    break;
            }

            
            mi.Items.Add(newMI);
        }

        /// <summary>
        /// Delete Submenu on close
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_SubmenuClosed(object sender, RoutedEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            mi.Items.Clear();
            MenuItem newMI = new MenuItem();
            mi.Items.Add(newMI);
        }
        #endregion

        #region DWORD / QWORD radio button actions
        /// <summary>
        /// Radio button decimal click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadioButton_Decimal_Click(object sender, RoutedEventArgs e)
        {
            RadioButton rbtn = sender as RadioButton;
            StackPanel sp = rbtn.Parent as StackPanel;
            StackPanel supSP = sp.Parent as StackPanel;

            foreach (UIElement element in supSP.Children)
            {
                if (element.GetType() == typeof(TextBox))
                {
                    TextBox tb = element as TextBox;
                    tb.Text = HexToDec(tb.Text).ToString();
                    break;
                }
            }
        }

        /// <summary>
        /// Radio button heaxadecimal click event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RadioButton_Hexadecimal_Click(object sender, RoutedEventArgs e)
        {
            RadioButton rbtn = sender as RadioButton;
            StackPanel sp = rbtn.Parent as StackPanel;
            StackPanel supSP = sp.Parent as StackPanel;

            foreach (UIElement element in supSP.Children)
            {
                if (element.GetType() == typeof(TextBox))
                {
                    TextBox tb = element as TextBox;
                    tb.Text = DecToHex(Convert.ToUInt64(tb.Text));
                    break;
                }
            }
        }
        #endregion

        #region Submenu content loader

        /// <summary>
        /// Load multi string data to richtextbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rtb_multi_Loaded(object sender, RoutedEventArgs e)
        {
            RichTextBox rtb = sender as RichTextBox;
            RegDataItem regItem = rtb.DataContext as RegDataItem;
            rtb.AppendText(String.Join("\r", regItem.Value.ToStringArray()));
        }

        private void HexaGrid_Loaded(object sender, RoutedEventArgs e)
        {
            HexaGrid hg = sender as HexaGrid;
            hg.registryObject = (hg.DataContext as RegDataItem).Value;
        }

        #endregion

        #region Helper

        /// <summary>
        /// Convert hex string to byte array
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        private byte[] hexStringToByteArray(string hexString)
        {
            string[] stringArray = hexString.Replace("\r\n", "").Split('-');
            List<byte> byteArray = new List<byte>();

            foreach (string strElement in stringArray)
                byteArray.Add(Convert.ToByte(strElement, 16));

            return byteArray.ToArray();
        }

        /// <summary>
        /// Convert Hexdeciaml to Decimal
        /// </summary>
        /// <param name="hexValue"></param>
        /// <returns>long</returns>
        private long HexToDec(string hexValue)
        {
            return Int64.Parse(hexValue, System.Globalization.NumberStyles.HexNumber);
        }

        /// <summary>
        /// Convert Decimal to Hexdeciaml
        /// </summary>
        /// <param name="decValue"></param>
        /// <returns>string</returns>
        private string DecToHex(ulong decValue)
        {
            return string.Format("{0:x}", decValue);
        }

        /// <summary>
        /// Convert Qword and Qword text string from Hex string
        /// </summary>
        /// <param name="text"></param>
        /// <returns>string</returns>
        private string setDwordQword(RadioButton button, string text)
        {
            if (!(bool)button.IsChecked)
                return this.HexToDec(text).ToString();
            else
                return text;
        }

        /// <summary>
        /// Write new values to registry
        /// </summary>
        /// <param name="regItem"></param>
        /// <param name="oReg"></param>
        /// <param name="value"></param>
        private void writeValuesToRgistry(RegDataItem regItem, RegObject oReg, object value)
        {
            bool result = new RegistryHelper().WriteValue(oReg.RootKey, oReg.SubKey, oReg.Type, oReg.KeyName, value);
            if (!result)
                MessageBox.Show("Unable to write to registry.", "RegEditor Error", MessageBoxButton.OK, MessageBoxImage.Error);
            else
            {
                //TODO : Clear item from context menu
                oReg.Value = value;
                regItem.Value = oReg;
            }
        }

        /// <summary>
        /// Paste rgistry hive to registry
        /// </summary>
        /// <param name="registryObject"></param>
        /// <param name="keyName"></param>
        /// <returns></returns>
        private bool pasteRegistryHive(RegKey registryObject, string keyName)
        {
            foreach (RegObject oReg in registryObject.Values)
            {
                if (!reg.WriteValue(this.rightClickRegObject.RootKey, this.rightClickRegObject.SubKey + @"\" + keyName, oReg.Type, oReg.KeyName, oReg.Value))
                    return false;
            }

            foreach (RegKey oReg in registryObject.SubKeys)
            {
                if (!this.pasteRegistryHive(oReg, keyName + @"\" + oReg.KeyName))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Save RegKey to clipboard
        /// </summary>
        private void setClipboradKey()
        {
            this.objParent.clipboardRegKey = new RegKey(this.rightClickRegObject.oParentObject, this.rightClickRegObject.RootKey, this.rightClickRegObject.SubKey, true, true, true);
        }

        /// <summary>
        /// Get registry type from string
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private RegistryHelper.VALUE_TYPE getTypeFromString(string text)
        {
            if (text.Equals("Create new string"))
                return RegistryHelper.VALUE_TYPE.REG_SZ;
            else if (text.Equals("Create new binary"))
                return RegistryHelper.VALUE_TYPE.REG_BINARY;
            else if (text.Equals("Create new DWORD"))
                return RegistryHelper.VALUE_TYPE.REG_DWORD;
            else if (text.Equals("Create new QWORD"))
                return RegistryHelper.VALUE_TYPE.REG_QWORD;
            else if (text.Equals("Create new multi string"))
                return RegistryHelper.VALUE_TYPE.REG_MULTI_SZ;
            else if (text.Equals("Create new expand string"))
                return RegistryHelper.VALUE_TYPE.REG_EXPAND_SZ;
            else
                return RegistryHelper.VALUE_TYPE.REG_NONE;
        }

        #endregion

    }


}
