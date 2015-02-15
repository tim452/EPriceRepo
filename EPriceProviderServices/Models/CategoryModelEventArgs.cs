using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace EPriceProviderServices.Models
{
    public class CategoryModelEventArgs : EventArgs
    {
        public ProviderServiceCategoryBase Model { get; set; }

        public CategoryActionState NewState { get; set; }

        public TreeViewItem Item { get; set; }
    }
}
