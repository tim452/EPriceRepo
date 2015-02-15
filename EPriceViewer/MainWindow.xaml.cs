using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EPriceRequestServiceDBEngine;
using EPriceRequestServiceDBEngine.Models;
using EPriceViewer.Enums;
using EPriceViewer.Helpers;

namespace EPriceViewer
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DataHelper _dataHelper;
        private readonly List<Un1tCategory> _categories;
        private const string LogSender = "Main App";
        private SolidColorBrush _clicableItemBrush = new SolidColorBrush(Color.FromRgb(99, 170, 20));
        public MainWindow()
        {
            InitializeComponent();
            _dataHelper = new DataHelper(new DbEngine());
            lstCatalogs.Items.Add(new TreeViewItem() {Header = new Label() {Content = "Загрузка каталогов..."}});
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await ShowCategories();
        }

        public async Task ShowCategories()
        {
            await Task.Run(() =>
            {
                try
                {
                    var treeViewItems = new List<TreeViewItem>();
                    var roots = _dataHelper.GetRootCategories();
                    Dispatcher.Invoke(() =>
                    {
                        foreach (var root in roots)
                        {
                            var item = GetTreeViewItemForUn1tCategory(root, _dataHelper.Categories);
                            treeViewItems.Add(item);
                        }
                        lstCatalogs.Items.Clear();
                        foreach (var item in treeViewItems)
                        {
                            lstCatalogs.Items.Add(item);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Log.WriteToLog(LogSender, " Ошибка в ShowCategories() " + ex.Message);
                    Dispatcher.Invoke(() => MessageBox.Show("Не удалось загрузить категории"));
                }
            });
        }

        public TreeViewItem GetTreeViewItemForUn1tCategory(Un1tCategory category, List<Un1tCategory> list)
        {
            var isMapped = _dataHelper.IsCategoryMapped(category.Id);
            var treeViewItem = new TreeViewItem() {Header = new TextBlock() {Background = new SolidColorBrush(Color.FromArgb(0,0,0,0)), Text = category.Name, Tag = -1, Margin = new Thickness(2), TextWrapping = TextWrapping.Wrap, Width = 200}};
            var childsCategory = list.Where(x => x.IdParent == category.Id).ToList();
            if (childsCategory.Any())
            {
                foreach (var childCategory in childsCategory)
                {
                    var childTreeViewItem = GetTreeViewItemForUn1tCategory(childCategory, list);
                    treeViewItem.Items.Add(childTreeViewItem);
                }
            }
            if (isMapped)
            {   
                var header = treeViewItem.Header as TextBlock;
                if (header != null)
                {
                    header.Background = _clicableItemBrush;
                    if (!childsCategory.Any())
                    {
                        header.Tag = category.Id;
                        header.Cursor = Cursors.Hand;
                    }
                }
            }
            return treeViewItem;
        }

        private async void lstCatalogs_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            LoadProducts();
        }

        private async void LoadProducts()
        {
            var selectedItem = lstCatalogs.SelectedItem as TreeViewItem;
            if (selectedItem != null)
            {
                var textBlock = selectedItem.Header as TextBlock;
                if (textBlock == null) return;
                if (textBlock.Tag == null) return;
                var categoryId = (int)textBlock.Tag;
                if (categoryId == -1) return;
                lstProducts.Items.Clear();
                lstProducts.Items.Add(new Label() { Content = "Загрузка товаров..." });
                var sort = SortType.PartNumber;
                var sortSelectedItem = ctrlSort.SelectedItem as ComboBoxItem;
                if (sortSelectedItem != null)
                {
                    if (sortSelectedItem.Tag != null)
                    {
                        sort = (SortType)(int.Parse(sortSelectedItem.Tag.ToString()));
                    }
                }
                var isNotNullStockLoad = false;
                if (IsLoadNotNullStock.IsChecked.HasValue)
                {
                    isNotNullStockLoad = IsLoadNotNullStock.IsChecked.Value;
                }
                var products = await Task.Run(() =>
                {
                    var list = new List<Product>();
                    try
                    {
                        list = _dataHelper.GetProductsForCategory(categoryId);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteToLog(LogSender, " lstCatalogs_SelectedItemChanged() " + ex.Message);
                        Dispatcher.Invoke(() => MessageBox.Show("Не удалось загрузить каталог товаров"));
                    }
                    return list;
                });
                if (isNotNullStockLoad)
                {
                    products = (from product in products
                                where product.Stocks.Max(x => x.Value) > 0 || product.Stocks.Any(x => x.Value == -2)
                                select product).ToList();
                }
                var sameNames = (from product in products
                    group product by product.Name
                    into newGroup
                    select new {newGroup, Count = newGroup.Count()}).Where(x=>x.Count > 1);
                foreach (var sameName in sameNames)
                {
                    foreach (var product in sameName.newGroup)
                    {
                        product.IsNotOne = true;
                    }
                }
                switch (sort)
                {
                    case SortType.PartNumber:
                        products = products.OrderBy(x => x.PartNumber).ToList();
                        break;
                    case SortType.Name:
                        products = products.OrderBy(x => x.Name).ToList();
                        break;
                }
                Dispatcher.Invoke(() =>
                {
                    var width = lstProducts.ActualWidth;
                    lstProducts.Items.Clear();
                    var viewItems = products.Select(x => new ProductView(x, width)).ToList();
                    foreach (var viewItem in viewItems)
                    {
                        lstProducts.Items.Add(viewItem);
                    }
                    if (!viewItems.Any())
                        lstProducts.Items.Add(new Label() { Content = "В данной категории товаров нет" });
                });
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var window = (Window) sender;
            if (window != null)
            {
                var height = window.ActualHeight;
                lstCatalogs.Height = height - 70;
                lstProducts.Height = height - 70;
                var width = window.ActualWidth;
                lstProducts.Width = width - 420;
                var items = lstProducts.Items;
                if (items != null && items.Count > 0)
                {
                    foreach (ProductView item in items)
                    {
                        item.SetWidth(lstProducts.Width);
                    }
                }
            }
        }

        private void ctrlSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadProducts();
        }

        private void IsLoadNotNullStock_Click(object sender, RoutedEventArgs e)
        {
            LoadProducts();
        }
    }
}
