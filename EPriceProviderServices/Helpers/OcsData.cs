using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using EPriceProviderServices.Models;
using EPriceProviderServices.OcsDataService;
using EPriceRequestServiceDBEngine.Enums;

namespace EPriceProviderServices.Helpers
{
    public class OcsData : DataLevelBase
    {
        public OcsData()
            : base(ProviderType.OCS, string.Empty)
        {
        }
    }
}
