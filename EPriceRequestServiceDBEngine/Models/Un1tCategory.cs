using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace EPriceRequestServiceDBEngine.Models
{
    [Serializable]
    public class Un1tCategory
    {
        public int Id { get; set; }

        public int IdParent { get; set; }

        public string Name { get; set; }

        public int LoadedProviderCategoryCount { get; set; }
    }
}
