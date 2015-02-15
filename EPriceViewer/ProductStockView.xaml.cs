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
using EPriceRequestServiceDBEngine.Models;
using EPriceViewer.Helpers;
using EPriceViewer.Models;

namespace EPriceViewer
{
    /// <summary>
    /// Логика взаимодействия для ProductStockView.xaml
    /// </summary>
    public partial class ProductStockView : UserControl
    {
        private readonly ProductStockItem _model;
        private readonly SolidColorBrush _minPriceBrush;
        public ProductStockView(ProductStockItem stockItem)
        {
            InitializeComponent();
            _minPriceBrush = new SolidColorBrush(Color.FromRgb(60, 170, 20));
            _model = stockItem;
            BindData();
        }

        private void BindData()
        {
            txtLocation.Text = ViewerHelper.GetDisplayNameForLocation(_model.Location);
            txtPriceUsd.Text = ((int)_model.PriceUsd != -1) ? _model.PriceUsd.ToString("F") + " $" : "н/д";
            txtPriceRub.Text = ((int)_model.PriceRub != -1) ? _model.PriceRub.ToString("F") + " р." : "н/д";
            var usdValue = _model.ValueUsd.ToString("G");
            switch (_model.ValueUsd)
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
            txtValueUsd.Text = usdValue;
            var rubValue = _model.ValueRub.ToString("G");
            switch (_model.ValueRub)
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
            txtValueRub.Text = rubValue;
            if (_model.IsMinUsdPrice)
            {
                txtPriceUsd.Foreground = _minPriceBrush;
                txtPriceUsd.FontWeight = FontWeights.Medium;
            }
            if (_model.IsMinRubPrice)
            {
                txtPriceRub.Foreground = _minPriceBrush;
                txtPriceRub.FontWeight = FontWeights.Medium;
            }
        }
    }
}
