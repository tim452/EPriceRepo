using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace EPriceProviderServices.Models
{
    public class DeleteMappedCategoryEventArgs : EventArgs
    {
        public TreeViewItem Item { get; set; }
    }
}
