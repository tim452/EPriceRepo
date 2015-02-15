using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using EPriceProviderServices.Models;
using EPriceProviderServices.OcsDataService;
using EPriceRequestServiceDBEngine.Enums;
using CatalogResult = EPriceProviderServices.MerlionDataService.CatalogResult;

namespace EPriceProviderServices.Helpers
{
    public class MerlionData : DataLevelBase
    {
        public MerlionData() : base(ProviderType.Merlion, "Order")
        {
        }
    }
}
