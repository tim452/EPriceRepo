using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EPriceProviderServices.Models
{
    public class ProviderCategoryViewModel
    {
        public CategoryItemView Item { get; set; }

        public ProviderServiceCategoryBase Model { get; set; }
    }
}
