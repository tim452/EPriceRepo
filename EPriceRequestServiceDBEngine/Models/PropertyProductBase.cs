using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPriceRequestServiceDBEngine.Enums;

namespace EPriceRequestServiceDBEngine.Models
{
    [Serializable]
    public class PropertyProductBase
    {
        public string IdProductProvider { get; set; }

        public IdValueType IdProductProviderType { get; set; }

        public int IdProduct { get; set; }

        public ProviderType Provider { get; set; }

        public string IdPropertyProvider { get; set; }

        public IdValueType IdProvertyPropertyType { get; set; }

        public string Name { get; set; }

        public string Value { get; set; }
    }
}
