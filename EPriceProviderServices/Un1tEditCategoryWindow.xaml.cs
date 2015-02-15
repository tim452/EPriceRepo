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
using System.Windows.Shapes;

namespace EPriceProviderServices
{
    /// <summary>
    /// Логика взаимодействия для Un1tEditCategoryWindow.xaml
    /// </summary>
    public partial class Un1tEditCategoryWindow : Window
    {
        public Un1tEditCategoryWindow()
        {
            InitializeComponent();
        }

        public Un1tEditCategoryWindow(string name)
            : this()
        {
            InitializeComponent();
            txtName.Text = name;
            txtName.Focus();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            var name = txtName.Text;
            if (!string.IsNullOrEmpty(name))
            {
                var parentWindow = Owner as MainWindow;
                Close();
                if (parentWindow != null)
                {
                    parentWindow.EditUn1tCategory(name);
                }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
