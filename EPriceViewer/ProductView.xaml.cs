using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using EPriceRequestServiceDBEngine.Enums;
using EPriceRequestServiceDBEngine.Models;

namespace EPriceViewer
{
    /// <summary>
    /// Логика взаимодействия для ProductView.xaml
    /// </summary>
    public partial class ProductView : UserControl
    {
        private readonly Product _model;
        private double _width;
        private bool _propertiesShow;
        private string _propertiesText;
        public ProductView(Product product, double width)
        {
            InitializeComponent();
            _model = product;
            _width = width - 470;
            _propertiesShow = false;
            BindData();
        }

        public void SetWidth(double width)
        {
            _width = width - 470;
            txtName.Width = _width;
        }

        private void BindData()
        {
            txtProperties.Tag = false;
            txtPartNumber.Text = _model.PartNumber;
            txtName.Text = _model.Name;
            txtName.Width = _width;
            if (_model.IsNotOne)
            {
                txtName.Foreground = new SolidColorBrush(Colors.Red);
            }
            txtProvider.Text = _model.Provider.ToString();
            txtDate.Text = _model.Date.ToString("dd.MM.yy HH:mm");
            txtMinPriceUsd.Text = ((int)_model.MinPriceUsd != -1) ? _model.MinPriceUsd.ToString("F") + " $" : "н/д";
            txtMinPriceRub.Text = ((int)_model.MinPriceRub != -1) ? _model.MinPriceRub.ToString("F") + " р." : "н/д";
            var usdValue = _model.MinStockUsdValue.ToString("G");
            switch (_model.MinStockUsdValue)
            {
                case -1:
                    usdValue = "н/д";
                    break;
                case -2:
                    usdValue = "много";
                    break;
                case -3:
                    usdValue = "мало";
                    break;
            }
            txtMinStockUsdValue.Text = usdValue;
            var rubValue = _model.MinStockRubValue.ToString("G");
            switch (_model.MinStockRubValue)
            {
                case -1:
                    rubValue = "н/д";
                    break;
                case -2:
                    rubValue = "много";
                    break;
                case -3:
                    rubValue = "мало";
                    break;
            }
            txtMinStockRubValue.Text = rubValue;
            var providers = new[] {ProviderType.Merlion, ProviderType.Treolan, ProviderType.OCS, ProviderType.OLDI};
            var stocks = new List<ProductProviderStockView>();
            foreach (var provider in providers)
            {
                var stockItem = new ProductProviderStockView(provider, _model.Stocks);
                stocks.Add(stockItem);
            }
            if (stocks.Any())
            {
                foreach (var stock in stocks)
                {
                    Stocks.Children.Add(stock);
                }
            }
        }

        private void txtPartNumber_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show(txtPartNumber.Text);
        }

        private void Properties_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var tag = txtProperties.Tag;
            if (tag != null)
            {
                var isLoad = (bool) tag;
                if (!isLoad)
                {
                    _propertiesText = "Характеристики";
                    var productId = _model.Id;
                    var db = new DbEngine();
                    var properties = db.LoadProperties(productId);
                    if (properties.Any())
                    {
                        properties = properties.OrderBy(x => x.Name).ToList();
                        foreach (var property in properties)
                        {
                            _propertiesText += "\n" + property.Name + " : " + property.Value;
                        }
                    }
                    txtProperties.Tag = true;
                }
                if (_propertiesShow)
                {
                    txtProperties.Text = "Характеристики";
                    Stocks.Visibility = Visibility.Visible;
                }
                else
                {
                    txtProperties.Text = _propertiesText;
                    Stocks.Visibility = Visibility.Collapsed;
                }
                _propertiesShow = !_propertiesShow;
            }
        }
    }
}
