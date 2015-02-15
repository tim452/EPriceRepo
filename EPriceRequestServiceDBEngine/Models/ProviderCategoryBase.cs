using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPriceRequestServiceDBEngine.Enums;

namespace EPriceRequestServiceDBEngine.Models
{
    [Serializable]
    public class ProviderCategoryBase
    {
        public string Id { get; set; }

        public IdValueType IdType { get; set; }

        public string IdParent { get; set; }

        public IdValueType IdParentType { get; set; }

        public int IdUn1tCategory { get; set; }
    }
}
