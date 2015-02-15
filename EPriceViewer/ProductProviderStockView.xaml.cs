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
using EPriceRequestServiceDBEngine.Enums;
using EPriceRequestServiceDBEngine.Models;
using EPriceViewer.Helpers;
using EPriceViewer.Models;

namespace EPriceViewer
{
    /// <summary>
    /// Логика взаимодействия для ProductProviderStockView.xaml
    /// </summary>
    public partial class ProductProviderStockView : UserControl
    {
        private readonly ProviderType _provider;
        private readonly List<ProductStock> _stocks;

        public ProductProviderStockView(ProviderType provider, List<ProductStock> stocks)
        {
            InitializeComponent();
            _provider = provider;
            _stocks = stocks;
            BindModel();
        }

        private void BindModel()
        {
            ProviderName.Content = _provider.ToString();
            var stockItems = new List<ProductStockItem>();
            var providerStocks = _stocks.Where(x => x.Provider == _provider).ToList();
            if (providerStocks.Any())
            {
                var locations = new[] {Location.Region, Location.Moscow, Location.SanktPeterburg};
                foreach (var location in locations)
                {
                    ProductStockItem stockItem = null;
                    var locationStocks = providerStocks.Where(x => x.Location == location).ToList();
                    if (locationStocks.Any())
                    {
                        stockItem = new ProductStockItem()
                        {
                            Location = location,
                            PriceRub = -1,
                            PriceUsd = -1,
                            ValueRub = -1,
                            ValueUsd = -1
                        };
                        var usdStock = locationStocks.FirstOrDefault(x => x.Currency == Currency.USD);
                        if (usdStock != null)
                        {
                            stockItem.PriceUsd = usdStock.Price;
                            stockItem.ValueUsd = usdStock.Value;
                        }
                        var rubStock = locationStocks.FirstOrDefault(x => x.Currency == Currency.RUB);
                        if (rubStock != null)
                        {
                            stockItem.PriceRub = rubStock.Price;
                            stockItem.ValueRub = rubStock.Value;
                        }
                    }
                    if (stockItem != null) stockItems.Add(stockItem);
                }
            }
            if (stockItems.Any())
            {
                var pricesRubAvailable = _stocks.Where(x => x.Currency == Currency.RUB && (int) x.Price != -1).ToList();
                if (pricesRubAvailable.Any())
                {
                    var minRubPrice = pricesRubAvailable.Min(x => x.Price);
                    var minRubPriceStocks = stockItems.Where(x => x.PriceRub.Equals(minRubPrice)).ToList();
                    if (minRubPriceStocks.Any())
                    {
                        minRubPriceStocks.ForEach(x => x.IsMinRubPrice = true);
                    }
                }
                var pricesUsdAvailable = _stocks.Where(x => x.Currency == Currency.USD && (int) x.Price != -1).ToList();
                if (pricesUsdAvailable.Any())
                {
                    var minUsdPrice = pricesUsdAvailable.Min(x => x.Price);
                    var minUsdPriceStocks = stockItems.Where(x => x.PriceUsd.Equals(minUsdPrice)).ToList();
                    if (minUsdPriceStocks.Any())
                    {
                        minUsdPriceStocks.ForEach(x => x.IsMinUsdPrice = true);
                    }   
                }
                var viewItems = stockItems.Select(x => new ProductStockView(x)).ToList();
                if (viewItems.Any())
                {
                    foreach (var viewItem in viewItems)
                    {
                        Stocks.Children.Add(viewItem);
                    }
                }
            }
        }
    }
}
