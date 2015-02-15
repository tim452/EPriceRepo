using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EPriceProviderServices.MerlionDataService;
using EPriceProviderServices.Models;
using EPriceProviderServices.OcsDataService;
using EPriceProviderServices.OldiDataService;
using EPriceRequestServiceDBEngine.Enums;
using EPriceRequestServiceDBEngine.Models;
using CatalogResult = EPriceProviderServices.MerlionDataService.CatalogResult;
using Currency = EPriceRequestServiceDBEngine.Enums.Currency;

namespace EPriceProviderServices.Helpers
{
    public static class Extension
    {
        public static ProviderServiceCategoryBase ConvertToDataModel(this CatalogResult instance)
        {
            var model = new ProviderServiceCategoryBase()
            {
                Id = instance.ID,
                IdType = IdValueType.String,
                IdParent = instance.ID_PARENT,
                IdParentType = IdValueType.String,
                Name = instance.Description,
                IdUn1tCategory = -1,
                IsLoad = false
            };
            return model;
        }

        public static ProviderServiceCategoryBase ConvertToDataModel(this Category instance)
        {
            var model = new ProviderServiceCategoryBase()
            {
                Id = instance.CategoryID,
                IdParent = instance.ParentCategoryID,
                IdType = IdValueType.String,
                IdParentType = IdValueType.String,
                Name = instance.CategoryName,
                IdUn1tCategory = -1,
                IsLoad = false
            };
            return model;
        }

        public static Un1tProductBase ConvertToDataModel(this ItemsResult instance)
        {
            var model = new Un1tProductBase()
            {
                Id = -1,
                Brand = instance.Brand,
                IdProvider = instance.No,
                IdProviderType = IdValueType.String,
                IdProviderCategory = instance.GroupCode3.Trim(),
                IdProviderCategoryType = IdValueType.String,
                IdCategory = -1,
                Name = instance.Name,
                PartNumber = instance.Vendor_part.Trim(),
                Provider = ProviderType.Merlion,
                Vendor = string.Empty
            };
            return model;
        }

        public static Un1tProductBase ConvertToDataModel(this OcsDataService.Product instance, decimal currencyRate)
        {
            var model = new Un1tProductBase()
            {
                Id = -1,
                Brand = string.Empty,
                IdProvider = instance.ItemID,
                IdProviderType = IdValueType.String,
                IdProviderCategory = instance.CategoryID.Trim(),
                IdProviderCategoryType = IdValueType.String,
                IdCategory = -1,
                Name = instance.ItemName,
                PartNumber = instance.PartNumber.Trim(),
                Provider = ProviderType.OCS,
                Vendor = instance.Producer
            };
            if (instance.Locations != null && instance.Locations.Any())
            {
                if (instance.Currency == "RUR")
                {
                    var location = instance.Locations.FirstOrDefault(x => x.Location == "ЦО (Москва)");
                    if (location != null)
                    {
                        var stock = new StockBase()
                        {
                            Currency = Currency.RUB,
                            Location = Location.Moscow,
                            Price = (float) (instance.Price.HasValue ? instance.Price.Value : 0),
                            Value = location.Quantity
                        };
                        model.Stocks.Add(stock);
                        stock = new StockBase()
                        {
                            Currency = Currency.USD,
                            Location = Location.Moscow,
                            Price = (float)((instance.Price.HasValue ? instance.Price.Value : 0)/currencyRate),
                            Value = location.Quantity
                        };
                        model.Stocks.Add(stock);
                    }
                    location = instance.Locations.FirstOrDefault(x => x.Location == "ЦО (СПб)");
                    if (location != null)
                    {
                        var stock = new StockBase()
                        {
                            Currency = Currency.RUB,
                            Location = Location.SanktPeterburg,
                            Price = (float)(instance.Price.HasValue ? instance.Price.Value : 0),
                            Value = location.Quantity
                        };
                        model.Stocks.Add(stock);
                        stock = new StockBase()
                        {
                            Currency = Currency.USD,
                            Location = Location.SanktPeterburg,
                            Price = (float)((instance.Price.HasValue ? instance.Price.Value : 0) / currencyRate),
                            Value = location.Quantity
                        };
                        model.Stocks.Add(stock);
                    }
                    location = instance.Locations.FirstOrDefault(x => x.Location == "Екатеринбург");
                    if (location != null)
                    {
                        var stock = new StockBase()
                        {
                            Currency = Currency.RUB,
                            Location = Location.Region,
                            Price = (float)(instance.Price.HasValue ? instance.Price.Value : 0),
                            Value = location.Quantity
                        };
                        model.Stocks.Add(stock);
                        stock = new StockBase()
                        {
                            Currency = Currency.USD,
                            Location = Location.Region,
                            Price = (float)((instance.Price.HasValue ? instance.Price.Value : 0) / currencyRate),
                            Value = location.Quantity
                        };
                        model.Stocks.Add(stock);
                    }
                }
                if (instance.Currency == "USD")
                {
                    var location = instance.Locations.FirstOrDefault(x => x.Location == "ЦО (Москва)");
                    if (location != null)
                    {
                        var stock = new StockBase()
                        {
                            Currency = Currency.USD,
                            Location = Location.Moscow,
                            Price = (float)(instance.Price.HasValue ? instance.Price.Value : 0),
                            Value = location.Quantity
                        };
                        model.Stocks.Add(stock);
                        stock = new StockBase()
                        {
                            Currency = Currency.RUB,
                            Location = Location.Moscow,
                            Price = (float)((instance.Price.HasValue ? instance.Price.Value : 0) * currencyRate),
                            Value = location.Quantity
                        };
                        model.Stocks.Add(stock);
                    }
                    location = instance.Locations.FirstOrDefault(x => x.Location == "ЦО (СПб)");
                    if (location != null)
                    {
                        var stock = new StockBase()
                        {
                            Currency = Currency.USD,
                            Location = Location.SanktPeterburg,
                            Price = (float)(instance.Price.HasValue ? instance.Price.Value : 0),
                            Value = location.Quantity
                        };
                        model.Stocks.Add(stock);
                        stock = new StockBase()
                        {
                            Currency = Currency.RUB,
                            Location = Location.SanktPeterburg,
                            Price = (float)((instance.Price.HasValue ? instance.Price.Value : 0) * currencyRate),
                            Value = location.Quantity
                        };
                        model.Stocks.Add(stock);
                    }
                    location = instance.Locations.FirstOrDefault(x => x.Location == "Екатеринбург");
                    if (location != null)
                    {
                        var stock = new StockBase()
                        {
                            Currency = Currency.USD,
                            Location = Location.Region,
                            Price = (float)(instance.Price.HasValue ? instance.Price.Value : 0),
                            Value = location.Quantity
                        };
                        model.Stocks.Add(stock);
                        stock = new StockBase()
                        {
                            Currency = Currency.RUB,
                            Location = Location.Region,
                            Price = (float)((instance.Price.HasValue ? instance.Price.Value : 0) * currencyRate),
                            Value = location.Quantity
                        };
                        model.Stocks.Add(stock);
                    }
                }
            }
            return model;
        }

        public static Un1tProductBase ConvertToProductDataModel(this Item instance)
        {
            var model = new Un1tProductBase()
            {
                Id = -1,
                Brand = string.Empty,
                IdProvider = instance.Code,
                IdProviderType = IdValueType.String,
                IdProviderCategory = (instance.Parent != null && instance.Parent.IsGroup.HasValue && instance.Parent.IsGroup.Value) ? instance.Parent.Code.Trim() : string.Empty,
                IdProviderCategoryType = IdValueType.String,
                IdCategory = -1,
                Name = instance.Name,
                PartNumber = !string.IsNullOrEmpty(instance.VendorCode) ? instance.VendorCode.Trim() : string.Empty,
                Provider = ProviderType.OLDI,
                Vendor = instance.Vendor
            };
            return model;
        }

        public static List<StockServiceBase> ConvertToStockDataModel(this Item instance, List<OldiDataService.Currency> currencies)
        {
            var list = new List<StockServiceBase>();
            var stocks = instance.StockItem;
            var prices = instance.PriceItem;
            if (stocks != null && stocks.Any() && prices != null && prices.Any())
            {
                for (var i = 0; i < stocks.Count(); i++)
                {
                    var stock = stocks[i];
                    var price = prices[i];
                    var rubPrice = -1f;
                    var usdPrice = -1f;
                    var oldiCurrency = currencies.FirstOrDefault(x => x.Code == price.PriceType.Currency.Code);
                    if (oldiCurrency != null)
                    {
                        var priceValue = (float)(price.Value.HasValue ? price.Value.Value : 0);
                        var usdCurrency = currencies.FirstOrDefault(x => x.Name.Trim().ToLower() == "usd");
                        var eurCurrency = currencies.FirstOrDefault(x => x.Name.Trim().ToLower() == "eur");
                        switch (oldiCurrency.Name.Trim().ToLower())
                        {
                            case "eur":
                                if (eurCurrency != null && eurCurrency.Rate.HasValue)
                                {
                                    rubPrice = priceValue * (float)eurCurrency.Rate.Value;
                                    if (usdCurrency != null && usdCurrency.Rate.HasValue)
                                    {
                                        usdPrice = rubPrice / (float)usdCurrency.Rate.Value;
                                    }    
                                }
                                break;
                            case "usd":
                                usdPrice = (float)priceValue;
                                if (usdCurrency != null && usdCurrency.Rate.HasValue)
                                {
                                    rubPrice = priceValue * (float)usdCurrency.Rate.Value;
                                }
                                break;
                            case "руб.":
                                rubPrice = priceValue;
                                if (usdCurrency != null && usdCurrency.Rate.HasValue)
                                {
                                    usdPrice = rubPrice / (float)usdCurrency.Rate.Value;
                                } 
                                break;
                        }
                    }
                    if ((int)rubPrice == -1)
                    {
                        rubPrice = (float) (price.Value.HasValue ? price.Value.Value : 0);
                    }
                    if (stock.Stock.Name.Trim() == "00070")
                    {
                        var model = new StockServiceBase()
                        {
                            IdProductProvider = instance.Code,
                            IdProductValueType = IdValueType.String,
                            Stock = new StockBase()
                            {
                                Location = Location.Moscow,
                                Currency = Currency.RUB,
                                Price = rubPrice,
                                Value = stock.Available
                            }
                        };
                        list.Add(model);
                        if ((int) usdPrice != -1)
                        {
                            model = new StockServiceBase()
                            {
                                IdProductProvider = instance.Code,
                                IdProductValueType = IdValueType.String,
                                Stock = new StockBase()
                                {
                                    Location = Location.Moscow,
                                    Currency = Currency.USD,
                                    Price = usdPrice,
                                    Value = stock.Available
                                }
                            };
                            list.Add(model);
                        }
                    }
                }
            }
            return list;
        }

        public static List<StockServiceBase> ConvertToDataModel(this ItemsAvailResult instance, float rateRub)
        {
            var list = new List<StockServiceBase>();
            var moskowUsd = new StockServiceBase()
            {
                Stock = new StockBase()
                {
                    Location = Location.Moscow,
                    Currency = Currency.USD,
                    Price = (instance.PriceClient_MSK??0),
                    Value = instance.AvailableClient_MSK??0
                },
                IdProductProvider = instance.No,
                IdProductValueType = IdValueType.String
            };
            var regionUsd = new StockServiceBase()
            {
                Stock = new StockBase()
                {
                    Location = Location.Region,
                    Currency = Currency.USD,
                    Price = (instance.PriceClient_RG ?? 0),
                    Value = instance.AvailableClient_RG ?? 0
                },
                IdProductProvider = instance.No,
                IdProductValueType = IdValueType.String
            };
            var spbUsd = new StockServiceBase()
            {
                Stock = new StockBase()
                {
                    Location = Location.SanktPeterburg,
                    Currency = Currency.USD,
                    Price = (instance.PriceClient_RG ?? 0),
                    Value = instance.AvailableClient_RG ?? 0
                },
                IdProductProvider = instance.No,
                IdProductValueType = IdValueType.String
            };
            list.AddRange(new[] {moskowUsd, regionUsd, spbUsd});
            var rubModels = new List<StockServiceBase>();
            if ((int) rateRub != -1)
            {
                foreach (var serviceBase in list)
                {
                    var rubModel = new StockServiceBase()
                    {
                        Stock = new StockBase()
                        {
                            Currency = Currency.RUB,
                            Location = serviceBase.Stock.Location,
                            Price = serviceBase.Stock.Price*rateRub,
                            Value = serviceBase.Stock.Value
                        },
                        IdProductProvider = instance.No,
                        IdProductValueType = IdValueType.String
                    };
                    rubModels.Add(rubModel);
                }
            }
            if (rubModels.Any()) list.AddRange(rubModels);
            return list;
        }

        public static PropertyProductBase ConvertToDataModel(this ItemsPropertiesResult instance)
        {
            var model = new PropertyProductBase()
            {
                IdProduct = -1,
                IdProductProvider = instance.No,
                IdProductProviderType = IdValueType.String,
                IdPropertyProvider = (instance.PropertyID.HasValue)?instance.PropertyID.Value.ToString("G"):"-1",
                IdProvertyPropertyType = IdValueType.Integer,
                Name = instance.PropertyName,
                Provider = ProviderType.Merlion,
                Value = instance.Value
            };
            return model;
        }
    }
}
