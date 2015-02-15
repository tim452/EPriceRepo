using System;
using System.Collections.Generic;
using System.Globalization;
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
using EPriceProviderServices.Helpers;
using EPriceProviderServices.Models;

namespace EPriceProviderServices
{
    /// <summary>
    /// Логика взаимодействия для CategoryItemView.xaml
    /// </summary>
    public partial class CategoryItemView : UserControl
    {
        private ProviderServiceCategoryBase _model = null;
        private CategoryActionState _state = CategoryActionState.Stop;
        private DataLevelBase _dataLevel;
        private CategoryHighlight _highlight;
        public CategoryItemView()
        {
            InitializeComponent();
        }

        public CategoryItemView(ProviderServiceCategoryBase model, bool isAnyChildLoaded, DataLevelBase dataLevel, bool isEnabledIsLoad)
            : this()
        {
            _dataLevel = dataLevel;
            BindModel(model, isAnyChildLoaded, isEnabledIsLoad);
        }

        public void SetEnabledIsLoad(bool isEnabled)
        {
            CategoryItemIsLoad.IsEnabled = isEnabled;
        }

        public CategoryHighlight Highlight
        {
            get { return _highlight; }
        }

        private void BindModel(ProviderServiceCategoryBase model, bool isAnyChildLoaded, bool isEnabledIsLoad)
        {
            CategoryItemIsLoad.IsEnabled = isEnabledIsLoad;
            CategoryItemName.Text = model.Name;
            CategoryItemIsLoad.IsChecked = model.IsLoad;
            _state = model.IsLoad ? CategoryActionState.Load : CategoryActionState.Stop;
            if (!model.IsLoad && isAnyChildLoaded)
            {
                CategoryItemIsLoad.IsChecked = null;
            }
            _model = model;
            SetHighlight(CategoryHighlight.None);
        }

        public ProviderServiceCategoryBase GetModel()
        {
            return _model;
        }

        public void SetHighlight(CategoryHighlight highlight)
        {
            _highlight = highlight;
            var brush = GetBrushForHichlight(highlight);
            CategoryItemName.Background = brush;
        }

        private SolidColorBrush GetBrushForHichlight(CategoryHighlight highlight)
        {
            var brush = new SolidColorBrush(Colors.Transparent);
            switch (highlight)
            {
                case CategoryHighlight.FullLoad:
                    brush = new SolidColorBrush(Colors.Green);
                    break;
                case CategoryHighlight.SemiLoad:
                    brush = new SolidColorBrush(Colors.CornflowerBlue);
                    break;
                case CategoryHighlight.NoAction:
                    brush = new SolidColorBrush(Colors.Red);
                    break;
                case CategoryHighlight.SemiAction:
                    brush = new SolidColorBrush(Colors.MediumVioletRed);
                    break;
                case CategoryHighlight.SemiLoadAction:
                    brush = new SolidColorBrush(Colors.CornflowerBlue);
                    break;
            }
            return brush;
        }

        public void SetChecked(bool? isChecked)
        {
            CategoryItemIsLoad.IsChecked = isChecked;
        }

        private void CategoryItemIsLoad_Click(object sender, RoutedEventArgs e)
        {
            var item = this.Parent as TreeViewItem;
            var currentState = _state;
            var isChecked = false;
            if (currentState == CategoryActionState.Stop)
            {
                _state = CategoryActionState.Load;
                isChecked = true;
            }
            else if (currentState == CategoryActionState.Load)
            {
                _state = CategoryActionState.Stop;
            }
            CategoryItemIsLoad.IsChecked = isChecked;
            if (_dataLevel != null)
            {
                _dataLevel.RaiseCategoryLoadStateChangedEvent(_model, _state, item);
            }
        }

        private void CategoryItemName_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_highlight == CategoryHighlight.NoAction)
            {
                var item = this.Parent as TreeViewItem;
                if (item != null)
                {
                    _dataLevel.RaiseCategoryMapDeleted(item);
                }
            }
        }
    }

    public enum CategoryActionState
    {
        Stop = 1,
        Load = 2
    }

    public enum CategoryHighlight
    {
        None = 1,
        FullLoad = 2,
        SemiLoad = 3,
        NoAction = 4,
        SemiAction = 5,
        SemiLoadAction = 6
    }
}
