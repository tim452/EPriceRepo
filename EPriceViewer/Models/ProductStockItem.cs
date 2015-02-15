using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPriceRequestServiceDBEngine.Enums;

namespace EPriceViewer.Models
{
    public class ProductStockItem
    {
        public Location Location { get; set; }

        public float PriceUsd { get; set; }

        public float PriceRub { get; set; }

        public int ValueUsd { get; set; }

        public int ValueRub { get; set; }

        public bool IsMinRubPrice { get; set; }

        public bool IsMinUsdPrice { get; set; }
    }
}
