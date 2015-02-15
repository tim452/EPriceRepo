using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPriceRequestServiceDBEngine.Enums;

namespace EPriceRequestServiceDBEngine.Models
{
    [Serializable]
    public class StockBase
    {
        public DateTime Date { get; set; }

        public Currency Currency { get; set; }

        public Location Location { get; set; }

        public int Value { get; set; }

        public float Price { get; set; }
    }
}
