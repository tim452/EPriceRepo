using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPriceRequestServiceDBEngine.Enums;

namespace EPriceRequestServiceDBEngine.Models
{
    [Serializable]
    public class ProductStock
    {
        public ProviderType Provider { get; set; }

        public Location Location { get; set; }

        public Currency Currency { get; set; }

        public float Price { get; set; }

        public int Value { get; set; }

        public DateTime Date { get; set; }
    }
}
