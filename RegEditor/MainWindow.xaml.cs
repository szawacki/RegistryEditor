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
using System.Collections.ObjectModel;
using RegistryClass;
using System.ComponentModel;

namespace RegEditor
{
    //TODO : Scroll to selecetd item in treeview
    //TODO : Import / Export function
    //TODO : Compare registry

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public RegistryHelper reg = new RegistryHelper();
        public TabControl lastFocusedTabControl;
        private Point startPoint { get; set; }
        public List<string> favorites = new List<string>();
        public Double bnSlidePos {get; set;}
        public Double strSlidePos { get; set; }
        public Double dwSlidePos { get; set; }
        public Double exSlidePos { get; set; }
        public Double mulSlidePos { get; set; }
        public RegKey clipboardRegKey;
        public bool deleteKeyAfterPaste = false;
        public TreeViewItem cutTreeViewItem = null;

        public MainWindow()
        {
            InitializeComponent();
            this.loadFavorites();

            this.bnSlidePos = Convert.ToDouble(new RegistryHelper().ReadValue(RegistryHelper.ROOT_KEY.HKEY_CURRENT_USER, @"SOFTWARE\RegEditor", "bn_Slider").ToLong());
            this.strSlidePos = Convert.ToDouble(new RegistryHelper().ReadValue(RegistryHelper.ROOT_KEY.HKEY_CURRENT_USER, @"SOFTWARE\RegEditor", "str_Slider").ToLong());
            this.dwSlidePos = Convert.ToDouble(new RegistryHelper().ReadValue(RegistryHelper.ROOT_KEY.HKEY_CURRENT_USER, @"SOFTWARE\RegEditor", "dw_Slider").ToLong());
            this.exSlidePos = Convert.ToDouble(new RegistryHelper().ReadValue(RegistryHelper.ROOT_KEY.HKEY_CURRENT_USER, @"SOFTWARE\RegEditor", "ex_Slider").ToLong());
            this.mulSlidePos = Convert.ToDouble(new RegistryHelper().ReadValue(RegistryHelper.ROOT_KEY.HKEY_CURRENT_USER, @"SOFTWARE\RegEditor", "multi_Slider").ToLong());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.addTabItem(this.tabcontrol1, "localhost");
            this.addTabItem(this.tabcontrol, "localhost");
        }

        public void addTabItem(TabControl tab, string headerName)
        {
            TabItem ti = new TabItem();            
            ti.Header = headerName.ToUpper();
            ti.HeaderTemplate = this.tabcontrol1.FindResource("TabHeader") as DataTemplate;
            ti.Content = new RegTab(headerName, this);
            tab.Items.Add(ti);
            tab.SelectedIndex = tab.Items.Count -1;
            ti.Focus();
        }

        //private void btn_Add_Click(object sender, RoutedEventArgs e)
        //{
        //    this.addTabItem(this.tabcontrol, "localhost");
        //}

        private void btn_Connect_Click(object sender, RoutedEventArgs e)
        {
            string searchtext = Textbox1.Text;
            if (!String.IsNullOrEmpty(searchtext))
                this.openRemoteSession(searchtext);
        }

        private void Textbox1_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string searchtext = (sender as TextBox).Text;
                if (!String.IsNullOrEmpty(searchtext))
                    this.openRemoteSession(searchtext);
            }
        }

        private void openRemoteSession(string remoteHost)
        {
        //http://msdn.microsoft.com/en-us/magazine/cc163328.aspx
            if (!remoteHost.ToLower().Equals("localhost"))
            {
                if (!new Command().checkRemoteHostAvailablity(remoteHost))
                {
                    MessageBox.Show("Remote host: " + remoteHost + " is not available.", "RegEditor", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            TabControl tabC = this.lastFocusedTabControl;
            if (tabC == null)
                tabC = this.tabcontrol;

            this.addTabItem(tabC, remoteHost);
            //this.tabcontrol.SelectedItem = this.tabcontrol.Items.Cast<TabItem>().Where(item => item.Header.ToString() == remoteHost).Last();
        }

        private void btn_dual_Click(object sender, RoutedEventArgs e)
        {
            if (this.view_btnText.Text.Equals("Single view"))
            {
                this.view_btnText.Text = "Dual view";
                this.view_image.Source = new BitmapImage(new Uri("Images/application_tile_horizontal.png", UriKind.Relative));
                this.grid_tab_splitter.ColumnDefinitions.ElementAt(1).Width = new GridLength(0);
                this.grid_tab_splitter.ColumnDefinitions.ElementAt(2).Width = new GridLength(0);
                this.lastFocusedTabControl = this.tabcontrol;
                this.tabcontrol1.Focusable = false;
            }
            else 
            {
                this.view_btnText.Text = "Single view";
                this.view_image.Source = new BitmapImage(new Uri("Images/application.png", UriKind.Relative));
                this.grid_tab_splitter.ColumnDefinitions.ElementAt(0).Width = new GridLength(1, GridUnitType.Star);
                this.grid_tab_splitter.ColumnDefinitions.ElementAt(1).Width = new GridLength(2);
                this.grid_tab_splitter.ColumnDefinitions.ElementAt(2).Width = new GridLength(1, GridUnitType.Star);
                this.tabcontrol1.Focusable = true;
            }
        }

        private void tabcontrol_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }

        private void tabcontrol_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = e.GetPosition(null);
            Vector diff = startPoint - mousePos;
            var tabItem = e.Source as TabItem;

            if (tabItem == null)
                return;

            if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
            }
        }

        private void tabcontrol_Drop(object sender, DragEventArgs e)
        {
            var tabItemTarget = e.Source as TabItem;
            var tabItemSource = e.Data.GetData(typeof(TabItem)) as TabItem;          

            if (tabItemTarget == tabItemSource)
                return;

            var sourceTabControl = tabItemSource.Parent as TabControl;
            TabControl targetTabControl;
            int targetIndex;

            if (tabItemTarget == null)
            {
                targetTabControl = e.Source as TabControl;
                if (targetTabControl == null)
                    return;
                targetIndex = targetTabControl.Items.Count;
            }    
            else
            {
                targetTabControl = tabItemTarget.Parent as TabControl;
                targetIndex = targetTabControl.Items.IndexOf(tabItemTarget);
            }
          
            sourceTabControl.Items.Remove(tabItemSource);
            targetTabControl.Items.Insert(targetIndex, tabItemSource);
            //tabItemSource.Tag = targetTabControl;
            targetTabControl.SelectedIndex = targetIndex;
            tabItemSource.Focus();
        }

        private void grid_BG_Drop(object sender, DragEventArgs e)
        {
            if (e.Source.GetType() != typeof(Grid))
                return;

            var tabItemSource = e.Data.GetData(typeof(TabItem)) as TabItem;
            var sourceTabControl = tabItemSource.Parent as TabControl;
            TabControl targetTabControl;
            double x = e.GetPosition(sender as Grid).X;
            double start = 0.0;
            int column = 0;

            foreach (UIElement element in (sender as Grid).Children)
            {
                if (element.GetType() == typeof(Grid))
                {
                    Grid newGrid = element as Grid;
                    foreach (ColumnDefinition cd in newGrid.ColumnDefinitions)
                    {
                        start += cd.ActualWidth;
                        if (x < start)
                        {
                            break;
                        }
                        column++;
                    }
                    break;
                }
            }

            switch (column)
            {
                case 0:
                    targetTabControl = this.tabcontrol;
                    break;
                case 2:
                    targetTabControl = this.tabcontrol1;
                    break;
                default:
                    targetTabControl = null;
                    break;
            }

            if (targetTabControl == null)
                return;

            sourceTabControl.Items.Remove(tabItemSource);
            int targetIndex = targetTabControl.Items.Count;
            targetTabControl.Items.Insert(targetIndex, tabItemSource);
            //tabItemSource.Parent = targetTabControl;
            targetTabControl.SelectedIndex = targetIndex;
            tabItemSource.Focus();
        }

        private void tabcontrol_GotFocus(object sender, RoutedEventArgs e)
        {
            this.lastFocusedTabControl = sender as TabControl;
        }

        //private void open_Treeview_Click(object sender, RoutedEventArgs e)
        //{
        //    string test = "HKEY_LOCAL_MACHINE";

        //    var ti = this.tabcontrol.Items[this.tabcontrol.SelectedIndex] as TabItem;
        //    RegTab tab = ti.Content as RegTab;
        //    foreach (object item in tab.treeview_1.Items)
        //    {
        //        TreeViewItem tvi = item as TreeViewItem;
        //        if (tvi.Header.Equals(test))
        //        {
        //            tvi.IsExpanded = true;

        //        }
        //    }
        //}

        public void saveFavorites()
        {
            new RegistryHelper().WriteValue(RegistryHelper.ROOT_KEY.HKEY_CURRENT_USER, @"SOFTWARE\RegEditor", RegistryHelper.VALUE_TYPE.REG_MULTI_SZ, "Favorites", this.favorites.ToArray());
        }

        private void loadFavorites()
        {
            string[] favArr = new RegistryHelper().ReadValue(RegistryHelper.ROOT_KEY.HKEY_CURRENT_USER, @"SOFTWARE\RegEditor", "Favorites").ToStringArray();

            if (favArr == null)
                return;

            foreach (string fav in favArr)
            {
                MenuItem mit = new MenuItem();
                mit.Click += new RoutedEventHandler(mit_Click);
                mit.Header = fav;

                mit.Icon = new System.Windows.Controls.Image
                {
                    Source = new BitmapImage(new Uri("Images/star.png", UriKind.Relative))
                };
                mit.ContextMenu = this.ctmenu(mit);

                foreach (object item in this.menu_fav.Items)
                {
                    MenuItem mi = item as MenuItem;
                    if (mi.Header.ToString().Equals(mit.Header.ToString()))
                        return;
                }
                this.menu_fav.Items.Add(mit);
                this.favorites.Add(mit.Header.ToString());
            }
        }

        public void mit_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;
            TabItem ti = this.lastFocusedTabControl.Items[this.lastFocusedTabControl.SelectedIndex] as TabItem;
            RegTab tab = ti.Content as RegTab;

            foreach (ItemsControl item in tab.treeview_1.Items)
            {
                this.openChildItems(item, (sender as MenuItem).Header.ToString());
            }
            Mouse.OverrideCursor = null;
        }

        private void openChildItems(ItemsControl itemsControl, string text)
        {
            string[] search = text.Split('\\');
            text = text.Replace(search[0] + @"\", "");
            TreeViewItem treeViewItem = itemsControl as TreeViewItem;

            if (treeViewItem != null)
                if (treeViewItem.Header.ToString().Equals(search[0].Replace("__", "_")))
                {
                    treeViewItem.IsExpanded = true;
                    treeViewItem.IsSelected = true;

                    if (treeViewItem.HasItems)
                    {
                        foreach (ItemsControl objControl in treeViewItem.Items)
                        {
                            if (objControl != null)
                                openChildItems(objControl, text);
                        }
                    }
                }
        }

        public ContextMenu ctmenu(MenuItem parent)
        {
            ContextMenu ctMenu = new ContextMenu();
            MenuItem item = new MenuItem();
            item.Header = "Delete";
            item.Tag = parent;
            item.Icon = new System.Windows.Controls.Image
            {
                Source = new BitmapImage(new Uri("Images/cross.png", UriKind.Relative))
            };
            item.Click += new RoutedEventHandler(item_Click);
            ctMenu.Items.Add(item);
            return ctMenu;
        }

        public void item_Click(object sender, RoutedEventArgs e)
        {
            this.menu_fav.Items.Remove(((sender as MenuItem).Tag as MenuItem));
            this.favorites.Remove(((sender as MenuItem).Tag as MenuItem).Header.ToString());
            this.saveFavorites();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            ContentPresenter cP = (btn.Parent as DockPanel).TemplatedParent as ContentPresenter;
            TabItem item = cP.TemplatedParent as TabItem;
            TabControl TabCtrl = item.Parent as TabControl;
            TabCtrl.Items.Remove(item);
        }

        private string RandomString(int size)
        {
            StringBuilder builder = new StringBuilder();
            Random random = new Random();
            char ch;
            for (int i = 0; i < size; i++)
            {
                ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)));
                builder.Append(ch);
            }

            return builder.ToString();
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            
        }

    }
}
