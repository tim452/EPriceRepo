using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPriceRequestServiceDBEngine.Models;

namespace EPriceProviderServices.Models
{
    [Serializable]
    public class ProviderServiceCategoryBase : ProviderCategoryBase
    {
        public string Name { get; set; }

        public bool IsLoad { get; set; }
    }
}
