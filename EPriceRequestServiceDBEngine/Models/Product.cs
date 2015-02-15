using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPriceRequestServiceDBEngine.Enums;

namespace EPriceRequestServiceDBEngine.Models
{
    [Serializable]
    public class Product
    {
        public bool IsNotOne { get; set; }
        public int Id { get; set; }

        public string Name { get; set; }

        public string PartNumber { get; set; }

        public float MinPriceUsd { get; set; }

        public float MinPriceRub { get; set; }

        public int MinStockRubValue { get; set; }

        public int MinStockUsdValue { get; set; }

        public ProviderType Provider { get; set; }

        public DateTime Date { get; set; }

        public List<ProductStock> Stocks { get; set; } 
    }
}
