using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPriceRequestServiceDBEngine.Enums;
using EPriceRequestServiceDBEngine.Models;

namespace EPriceRequestServiceDBEngine.Models
{
    [Serializable]
    public class StockServiceBase
    {
        public StockBase Stock { get; set; }

        public string IdProductProvider { get; set; }

        public IdValueType IdProductValueType { get; set; }
    }
}
