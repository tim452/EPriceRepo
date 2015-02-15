using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EPriceRequestServiceDBEngine.Enums;
using EPriceRequestServiceDBEngine.Models;

namespace EPriceRequestServiceDBEngine
{
    public class DbEngine
    {
        private readonly string _connectionString;

        public DbEngine()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["EPriceConnectionString"].ConnectionString;
        }

        #region UN1TCategory

        public List<Un1tCategory> LoadAllUn1tCategories()
        {
            var list = new List<Un1tCategory>();
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "LoadAllUN1TCategory";
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        var category = new Un1tCategory()
                        {
                            Id = rd.GetInt32(0),
                            IdParent = rd.GetInt32(1),
                            Name = rd.GetString(2),
                            LoadedProviderCategoryCount = rd.GetInt32(3)
                        };
                        list.Add(category);
                    }
                }
            }
            return list;
        }

        public void SaveUn1tCategories(List<Un1tCategory> categories)
        {
            if (categories.Any())
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var cmd = conn.CreateCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "CreateUN1TCategory";
                    conn.Open();
                    foreach (var category in categories)
                    {
                        cmd.Parameters.Clear();
                        cmd.Parameters.AddWithValue("@id", category.Id);
                        cmd.Parameters.AddWithValue("@idParent", category.IdParent);
                        cmd.Parameters.AddWithValue("@name", category.Name);
                        cmd.ExecuteNonQuery();
                    }
                }
            }
        }

        public void CreateUn1tCategory(Un1tCategory category)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "CreateUN1TCategory";
                conn.Open();
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@id", category.Id);
                cmd.Parameters.AddWithValue("@idParent", category.IdParent);
                cmd.Parameters.AddWithValue("@name", category.Name);
                cmd.ExecuteNonQuery();
            }
        }

        public bool DeleteUn1tCategories(List<int> idCategories)
        {
            var result = true;
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "DeleteUN1TCategory";
                conn.Open();
                foreach (var id in idCategories)
                {
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@id", id);
                    if (result) result = cmd.ExecuteNonQuery() > 0;
                }
            }
            return result;
        }

        public bool DeleteUn1tCategory(int idCategory)
        {
            var result = false;
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "DeleteUN1TCategory";
                conn.Open();
                cmd.Parameters.AddWithValue("@id", idCategory);
                result = cmd.ExecuteNonQuery() > 0;
            }
            return result;
        }

        public void UpdateUn1tCategory(int idCategory, string newName)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "UpdateUN1TCategory";
                conn.Open();
                cmd.Parameters.Clear();
                cmd.Parameters.AddWithValue("@id", idCategory);
                cmd.Parameters.AddWithValue("@name", newName);
                cmd.ExecuteNonQuery();
            }
        }

        #endregion

        #region ProviderCategory

        public void SaveLoadCategoriesForProvider(List<ProviderCategoryBase> categoriesToLoad,
            ProviderType provider)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmdDelete = conn.CreateCommand();
                cmdDelete.CommandType = CommandType.StoredProcedure;
                cmdDelete.CommandText = "DeleteProvidersCategories";
                cmdDelete.Parameters.AddWithValue("@provider", provider);
                conn.Open();
                cmdDelete.ExecuteNonQuery();
                if (categoriesToLoad.Any())
                {
                    var cmdCreate = conn.CreateCommand();
                    cmdCreate.CommandType = CommandType.StoredProcedure;
                    cmdCreate.CommandText = "CreateProviderCategory";
                    foreach (var category in categoriesToLoad)
                    {
                        cmdCreate.Parameters.Clear();
                        cmdCreate.Parameters.AddWithValue("@id", category.Id);
                        cmdCreate.Parameters.AddWithValue("@idParent", category.IdParent);
                        cmdCreate.Parameters.AddWithValue("@idType", category.IdType);
                        cmdCreate.Parameters.AddWithValue("@idParentType", category.IdParentType);
                        cmdCreate.Parameters.AddWithValue("@provider", provider);
                        cmdCreate.Parameters.AddWithValue("@idUN1TCategory", category.IdUn1tCategory);
                        cmdCreate.ExecuteNonQuery();
                    }
                }
            }
        }

        public List<ProviderCategoryBase> LoadLoadCategoriesForProvider(ProviderType provider)
        {
            var list = new List<ProviderCategoryBase>();
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "LoadProviderCategories";
                cmd.Parameters.AddWithValue("@provider", provider);
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        var category = new ProviderCategoryBase()
                        {
                            Id = rd.GetString(0),
                            IdType = (IdValueType)rd.GetInt32(1),
                            IdParent = rd.GetString(2),
                            IdParentType = (IdValueType)rd.GetInt32(3),
                            IdUn1tCategory = rd.GetInt32(5)
                        };
                        list.Add(category);
                    }
                }
            }
            return list;
        } 

        #endregion

        #region Product

        public List<Un1tProductBase> SaveProducts(List<Un1tProductBase> products, ProviderType provider, Action updateUI = null)
        {
            var newProducts = new List<Un1tProductBase>();
            if (products.Any())
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var cmdProduct = conn.CreateCommand();
                    cmdProduct.CommandType = CommandType.StoredProcedure;
                    cmdProduct.CommandText = "CreateProduct";
                    var cmdStock = conn.CreateCommand();
                    cmdStock.CommandType = CommandType.StoredProcedure;
                    cmdStock.CommandText = "SaveStock";
                    var cmdDeleteStocks = conn.CreateCommand();
                    cmdDeleteStocks.CommandType = CommandType.StoredProcedure;
                    cmdDeleteStocks.CommandText = "DeleteStocksForProvider";
                    cmdDeleteStocks.Parameters.AddWithValue("@provider", provider);
                    conn.Open();
                    cmdDeleteStocks.ExecuteNonQuery();
                    foreach (var product in products)
                    {
                        if (!string.IsNullOrEmpty(product.PartNumber))
                        {
                            cmdProduct.Parameters.Clear();
                            cmdProduct.Parameters.AddWithValue("@idCategory", product.IdCategory);
                            cmdProduct.Parameters.AddWithValue("@name", product.Name.Trim());
                            if (!string.IsNullOrEmpty(product.Vendor))
                            {
                                cmdProduct.Parameters.AddWithValue("@vendor", product.Vendor);
                            }
                            else
                            {
                                cmdProduct.Parameters.AddWithValue("@vendor", DBNull.Value);
                            }
                            if (!string.IsNullOrEmpty(product.Brand))
                            {
                                cmdProduct.Parameters.AddWithValue("@brand", product.Brand);
                            }
                            else
                            {
                                cmdProduct.Parameters.AddWithValue("@brand", DBNull.Value);
                            }
                            cmdProduct.Parameters.AddWithValue("@partNumber", product.PartNumber.Trim());
                            cmdProduct.Parameters.AddWithValue("@recordDate", product.Date);
                            cmdProduct.Parameters.AddWithValue("@provider", provider);
                            var isNew = (int) cmdProduct.ExecuteScalar() == 1;
                            if (isNew)
                            {
                                newProducts.Add(product);
                            }
                            if (product.Stocks != null && product.Stocks.Any())
                            {
                                foreach (var stock in product.Stocks)
                                {
                                    cmdStock.Parameters.Clear();
                                    cmdStock.Parameters.AddWithValue("@partNumber", product.PartNumber.Trim());
                                    cmdStock.Parameters.AddWithValue("@currency", stock.Currency);
                                    cmdStock.Parameters.AddWithValue("@location", stock.Location);
                                    cmdStock.Parameters.AddWithValue("@provider", provider);
                                    cmdStock.Parameters.AddWithValue("@value", stock.Value);
                                    cmdStock.Parameters.AddWithValue("@price", (decimal) stock.Price);
                                    cmdStock.Parameters.AddWithValue("@recordDate", stock.Date);
                                    cmdStock.ExecuteNonQuery();
                                }
                            }
                        }
                        if (updateUI != null) updateUI();
                    }
                }
            }
            return newProducts;
        }

        public List<Product> GetProductsForCategory(int categoryId)
        {
            var list = new List<Product>();
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "LoadProductsForCategory";
                cmd.Parameters.AddWithValue("@idCategory", categoryId);
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        var product = new Product()
                        {
                            Stocks = new List<ProductStock>(),
                            Id = rd.GetInt32(0),
                            Name = rd.GetString(2),
                            PartNumber = rd.GetString(5),
                            Provider = (ProviderType)rd.GetInt32(6),
                            Date = rd.GetDateTime(7)
                        };
                        list.Add(product);
                    }
                }
                if (list.Any())
                {
                    cmd.CommandText = "LoadStocksForProducts";
                    cmd.Parameters.Clear();
                    cmd.Parameters.AddWithValue("@categoryId", categoryId);
                    using (var rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            var id = rd.GetInt32(0);
                            var stock = new ProductStock()
                            {
                                Currency = (Currency) rd.GetInt32(1),
                                Location = (Location) rd.GetInt32(2),
                                Value = rd.GetInt32(3),
                                Price = (float) rd.GetDecimal(4),
                                Provider = (ProviderType) rd.GetInt32(5),
                                Date = rd.GetDateTime(6)
                            };
                            var product = list.FirstOrDefault(x => x.Id == id);
                            if (product != null)
                            {
                                product.Stocks.Add(stock);
                            }
                        }
                    }
                }
            }
            if (list.Any())
            {
                var providers = new[] { ProviderType.Merlion, ProviderType.Treolan, ProviderType.OCS, ProviderType.OLDI };
                var locations = new[] { Location.Region, Location.Moscow, Location.SanktPeterburg };
                var currencies = new[] { Currency.USD, Currency.RUB };
                foreach (var product in list)
                {
                    var stocks = product.Stocks;
                    var minUsdPrice = -1f;
                    var minRubPrice = -1f;
                    var minStockUsdValue = -1;
                    var minStockRubValue = -1;
                    var usdPrices = stocks.Where(x => x.Currency == Currency.USD).ToList();
                    if (usdPrices.Any())
                    {
                        minUsdPrice = usdPrices.Min(x => x.Price);
                        minStockUsdValue = usdPrices.Where(x => x.Price.Equals(minUsdPrice)).Min(x => x.Value);
                    }
                    var rubPrices = stocks.Where(x => x.Currency == Currency.RUB).ToList();
                    if (rubPrices.Any())
                    {
                        minRubPrice = rubPrices.Min(x => x.Price);
                        minStockRubValue = rubPrices.Where(x => x.Price.Equals(minRubPrice)).Min(x => x.Value);
                    }
                    product.MinPriceRub = minRubPrice;
                    product.MinPriceUsd = minUsdPrice;
                    product.MinStockUsdValue = minStockUsdValue;
                    product.MinStockRubValue = minStockRubValue;
                    var allStocks = new List<ProductStock>();
                    foreach (var providerType in providers)
                    {
                        foreach (var location in locations)
                        {
                            foreach (var currency in currencies)
                            {
                                var stock = new ProductStock()
                                {
                                    Currency = currency,
                                    Location = location,
                                    Provider = providerType,
                                    Price = -1,
                                    Value = -1
                                };
                                var searchStocks =
                                        stocks.Where(
                                            x => x.Currency == currency && x.Location == location && x.Provider == providerType).ToList();
                                if (searchStocks.Any())
                                {
                                    stock.Price = searchStocks.Min(x => x.Price);
                                    stock.Value = searchStocks.Min(x => x.Value);
                                }
                                allStocks.Add(stock);
                            }
                        }
                    }
                    product.Stocks.Clear();
                    product.Stocks.AddRange(allStocks);
                }
            }
            return list;
        }

        #endregion

        #region Product property

        public void SaveProductsProperties(List<Un1tProductBase> products, Action<int> updateUI = null)
        {
            if (products.Any())
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    var cmd = conn.CreateCommand();
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "SaveProductProperty";
                    conn.Open();
                    foreach (var product in products.Where(x => !string.IsNullOrEmpty(x.PartNumber) && x.Properties.Any()).ToList())
                    {
                        foreach (var property in product.Properties)
                        {
                            cmd.Parameters.Clear();
                            cmd.Parameters.AddWithValue("@partNumber", product.PartNumber.Trim());
                            cmd.Parameters.AddWithValue("@provider", product.Provider);
                            cmd.Parameters.AddWithValue("@idPropertyProvider", property.IdPropertyProvider.Trim());
                            cmd.Parameters.AddWithValue("@idPropertyProviderType", property.IdProvertyPropertyType);
                            cmd.Parameters.AddWithValue("@value", property.Value.Trim());
                            cmd.Parameters.AddWithValue("@name", property.Name.Trim());
                            cmd.ExecuteNonQuery();
                        }
                        if (updateUI != null) updateUI(product.Properties.Count());
                    }
                }
            }
        }

        public List<PropertyProductBase> LoadProperties(int idProduct)
        {
            var list = new List<PropertyProductBase>();
            using (var conn = new SqlConnection(_connectionString))
            {
                var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "LoadProperiesForProduct";
                cmd.Parameters.AddWithValue("@idProduct", idProduct);
                conn.Open();
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        var property = new PropertyProductBase()
                        {
                            IdProduct = rd.GetInt32(0),
                            Provider = (ProviderType)rd.GetInt32(1),
                            IdPropertyProvider = rd.GetString(2),
                            IdProductProviderType = (IdValueType)rd.GetInt32(3),
                            Name = rd.GetString(4),
                            Value = rd.GetString(5)
                        };
                        list.Add(property);
                    }
                }
            }
            return list;   
        }

        #endregion
    }
}
