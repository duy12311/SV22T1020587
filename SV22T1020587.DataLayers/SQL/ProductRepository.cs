using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.Models.Catalog;
using SV22T1020587.Models.Common;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SV22T1020587.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho mặt hàng (Product), thuộc tính và ảnh của mặt hàng
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Products

        public async Task<int> AddAsync(Product data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Products (ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling)
                VALUES (@ProductName, @ProductDescription, @SupplierID, @CategoryID, @Unit, @Price, @Photo, @IsSelling);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> DeleteAsync(int productID)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var tran = connection.BeginTransaction();
            try
            {
                const string sqlCheckOrders = @"SELECT CASE WHEN EXISTS(SELECT 1 FROM OrderDetails WHERE ProductID = @ProductID) THEN 1 ELSE 0 END";
                var usedInOrders = await connection.ExecuteScalarAsync<int>(sqlCheckOrders, new { ProductID = productID }, tran);
                if (usedInOrders > 0)
                {
                    tran.Rollback();
                    return false;
                }

                const string sqlDeletePhotos = "DELETE FROM ProductPhotos WHERE ProductID = @ProductID";
                await connection.ExecuteAsync(sqlDeletePhotos, new { ProductID = productID }, tran);

                const string sqlDeleteAttributes = "DELETE FROM ProductAttributes WHERE ProductID = @ProductID";
                await connection.ExecuteAsync(sqlDeleteAttributes, new { ProductID = productID }, tran);

                const string sqlDeleteProduct = "DELETE FROM Products WHERE ProductID = @ProductID";
                int rowsAffected = await connection.ExecuteAsync(sqlDeleteProduct, new { ProductID = productID }, tran);

                tran.Commit();
                return rowsAffected > 0;
            }
            catch
            {
                try { tran.Rollback(); } catch { }
                throw;
            }
            finally
            {
                await connection.CloseAsync();
            }
        }

        public async Task<Product?> GetAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT ProductID, ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling 
                FROM Products 
                WHERE ProductID = @ProductID";
            return await connection.QueryFirstOrDefaultAsync<Product?>(sql, new { ProductID = productID });
        }

        private sealed class ProductWithTotal
        {
            public int ProductID { get; set; }
            public string ProductName { get; set; } = "";
            public int SupplierID { get; set; }
            public int CategoryID { get; set; }
            public string Unit { get; set; } = "";
            public decimal Price { get; set; }
            public string Photo { get; set; } = "";
            public bool IsSelling { get; set; }
            public int TotalRows { get; set; }
        }

        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            var result = new PagedResult<Product>();
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            string whereCondition = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                whereCondition += " AND (ProductName LIKE @SearchValue)";
                parameters.Add("SearchValue", $"%{input.SearchValue}%");
            }

            if (input.CategoryID > 0)
            {
                whereCondition += " AND (CategoryID = @CategoryID)";
                parameters.Add("CategoryID", input.CategoryID);
            }

            if (input.SupplierID > 0)
            {
                whereCondition += " AND (SupplierID = @SupplierID)";
                parameters.Add("SupplierID", input.SupplierID);
            }

            if (input.MinPrice > 0)
            {
                whereCondition += " AND (Price >= @MinPrice)";
                parameters.Add("MinPrice", input.MinPrice);
            }
            if (input.MaxPrice > 0)
            {
                whereCondition += " AND (Price <= @MaxPrice)";
                parameters.Add("MaxPrice", input.MaxPrice);
            }

            result.Page = input.Page;
            result.PageSize = input.PageSize;

            if (input.PageSize == 0)
            {
                string sqlAll = $@"
                    SELECT ProductID, ProductName, SupplierID, CategoryID, Unit, Price, Photo, IsSelling
                    FROM Products
                    {whereCondition}
                    ORDER BY ProductName";

                var all = await connection.QueryAsync<Product>(sqlAll, parameters);
                var listAll = all.ToList();
                result.RowCount = listAll.Count;
                result.DataItems = listAll;
                return result;
            }

            string sqlPaged = $@"
                SELECT ProductID, ProductName, SupplierID, CategoryID, Unit, Price, Photo, IsSelling,
                       COUNT(1) OVER() AS TotalRows
                FROM Products
                {whereCondition}
                ORDER BY ProductName
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            parameters.Add("Offset", input.Offset);
            parameters.Add("PageSize", input.PageSize);

            var paged = await connection.QueryAsync<ProductWithTotal>(sqlPaged, parameters);
            var pagedList = paged.ToList();

            if (pagedList.Count == 0)
            {
                result.RowCount = 0;
                result.DataItems = new List<Product>();
                return result;
            }

            result.RowCount = pagedList[0].TotalRows;
            result.DataItems = pagedList.Select(x => new Product
            {
                ProductID = x.ProductID,
                ProductName = x.ProductName,
                ProductDescription = string.Empty,
                SupplierID = x.SupplierID,
                CategoryID = x.CategoryID,
                Unit = x.Unit,
                Price = x.Price,
                Photo = x.Photo,
                IsSelling = x.IsSelling
            }).ToList();

            return result;
        }

        public async Task<bool> UpdateAsync(Product data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Products
                SET ProductName = @ProductName,
                    ProductDescription = @ProductDescription,
                    SupplierID = @SupplierID,
                    CategoryID = @CategoryID,
                    Unit = @Unit,
                    Price = @Price,
                    Photo = @Photo,
                    IsSelling = @IsSelling
                WHERE ProductID = @ProductID";
            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }

        public async Task<bool> IsUsedAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"SELECT CASE WHEN EXISTS(SELECT 1 FROM OrderDetails WHERE ProductID = @ProductID) THEN 1 ELSE 0 END";
            return await connection.ExecuteScalarAsync<bool>(sql, new { ProductID = productID });
        }

        #endregion

        #region ProductAttributes

        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO ProductAttributes (ProductID, AttributeName, AttributeValue, DisplayOrder)
                VALUES (@ProductID, @AttributeName, @AttributeValue, @DisplayOrder);
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM ProductAttributes WHERE AttributeID = @AttributeID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { AttributeID = attributeID });
            return rowsAffected > 0;
        }

        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT AttributeID, ProductID, AttributeName, AttributeValue, DisplayOrder 
                FROM ProductAttributes 
                WHERE AttributeID = @AttributeID";
            return await connection.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { AttributeID = attributeID });
        }

        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT AttributeID, ProductID, AttributeName, AttributeValue, DisplayOrder 
                FROM ProductAttributes 
                WHERE PRODUCTID = @ProductID 
                ORDER BY DisplayOrder";
            var data = await connection.QueryAsync<ProductAttribute>(sql, new { ProductID = productID });
            return data.ToList();
        }

        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE ProductAttributes
                SET ProductID = @ProductID,
                    AttributeName = @AttributeName,
                    AttributeValue = @AttributeValue,
                    DisplayOrder = @DisplayOrder
                WHERE AttributeID = @AttributeID";
            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }

        #endregion

        #region ProductPhotos

        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO ProductPhotos (ProductID, Photo, Description, DisplayOrder, IsHidden)
                VALUES (@ProductID, @Photo, @Description, @DisplayOrder, @IsHidden);
                SELECT CAST(SCOPE_IDENTITY() AS BIGINT);";
            return await connection.ExecuteScalarAsync<long>(sql, data);
        }

        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM ProductPhotos WHERE PhotoID = @PhotoID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { PhotoID = photoID });
            return rowsAffected > 0;
        }

        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT PhotoID, ProductID, Photo, Description, DisplayOrder, IsHidden 
                FROM ProductPhotos 
                WHERE PhotoID = @PhotoID";
            return await connection.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { PhotoID = photoID });
        }

        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT PhotoID, ProductID, Photo, Description, DisplayOrder, IsHidden 
                FROM ProductPhotos 
                WHERE ProductID = @ProductID 
                ORDER BY DisplayOrder";
            var data = await connection.QueryAsync<ProductPhoto>(sql, new { ProductID = productID });
            return data.ToList();
        }

        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE ProductPhotos
                SET ProductID = @ProductID,
                    Photo = @Photo,
                    Description = @Description,
                    DisplayOrder = @DisplayOrder,
                    IsHidden = @IsHidden
                WHERE PhotoID = @PhotoID";
            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }

        #endregion

        #region Featured

        public async Task<List<Product>> ListFeaturedAsync(int count = 6)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT TOP (@Count) ProductID, ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling
                FROM Products
                WHERE IsSelling = 1
                ORDER BY ProductID DESC";
            var data = await connection.QueryAsync<Product>(sql, new { Count = count });
            return data.ToList();
        }

        #endregion
    }
}