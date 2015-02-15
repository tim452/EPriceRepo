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

namespace EPriceProviderServices
{
    /// <summary>
    /// Логика взаимодействия для Un1tCategoryItemView.xaml
    /// </summary>
    public partial class Un1tCategoryItemView : UserControl
    {
        private bool _isHighlithed = false;
        private Un1tCategory _model = null;
        private SolidColorBrush _currentBackground;
        public Un1tCategoryItemView()
        {
            InitializeComponent();
        }

        public Un1tCategoryItemView(Un1tCategory model)
            : this()
        {
            _currentBackground = new SolidColorBrush(Colors.Transparent);
            BindModel(model);
        }

        public bool IsHighlighted
        {
            get { return _isHighlithed; }
        }

        private void BindModel(Un1tCategory model)
        {
            CategoryItemName.Text = model.Name;
            _model = model;
        }

        public void SetName(string name)
        {
            CategoryItemName.Text = name;
        }

        public void SetHighlight(bool isHighlighted, bool isLazy = false)
        {
            _isHighlithed = isHighlighted;
            if (isHighlighted)
            {
                _currentBackground = new SolidColorBrush(Colors.CornflowerBlue);
            }
            else
            {
                _currentBackground = new SolidColorBrush(Colors.Transparent);
            }
            if (isLazy) return;
            CategoryViewContainer.Background = _currentBackground;
        }

        public void SetSelected(bool isSelected)
        {
            if (isSelected)
            {
                 CategoryViewContainer.Background = new SolidColorBrush(Colors.Green);
            }
            else
            {
                CategoryViewContainer.Background = _currentBackground;
            }
        }

        public Un1tCategory GetModel()
        {
            return _model;
        }
    }
}
