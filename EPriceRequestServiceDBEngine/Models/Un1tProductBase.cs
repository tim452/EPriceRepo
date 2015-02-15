using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPriceRequestServiceDBEngine.Enums;

namespace EPriceRequestServiceDBEngine.Models
{
    [Serializable]
    public class Un1tProductBase
    {
        private List<StockBase> _stocks = new List<StockBase>();
        private List<PropertyProductBase> _properies = new List<PropertyProductBase>();

        public DateTime Date { get; set; }

        public string Brand { get; set; }

        public string Vendor { get; set; }

        public string PartNumber { get; set; }

        public string Name { get; set; }

        public int Id { get; set; }

        public string IdProvider { get; set; }

        public IdValueType IdProviderType { get; set; }

        public int IdCategory { get; set; }

        public string IdProviderCategory { get; set; }

        public IdValueType IdProviderCategoryType { get; set; }

        public ProviderType Provider { get; set; }

        public List<StockBase> Stocks
        {
            get { return _stocks; }
        }

        public List<PropertyProductBase> Properties
        {
            get { return _properies; }
        }
    }
}
