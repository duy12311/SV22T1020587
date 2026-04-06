using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.Models.Catalog;
using SV22T1020587.Models.Common;
using System.Data;

namespace SV22T1020587.DataLayers.SQLServer
{
    public class CategoryRepository : IGenericRepository<Category>
    {
        private readonly string _connectionString;

        public CategoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddAsync(Category data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Categories (CategoryName, Description)
                VALUES (@CategoryName, @Description);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Categories WHERE CategoryID = @CategoryID";
            int rowsAffected = await connection.ExecuteAsync(sql, new { CategoryID = id });
            return rowsAffected > 0;
        }

        public async Task<Category?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT CategoryID, CategoryName, Description FROM Categories WHERE CategoryID = @CategoryID";
            return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { CategoryID = id });
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT CASE 
                    WHEN EXISTS(SELECT 1 FROM Products WHERE CategoryID = @CategoryID) THEN 1 
                    ELSE 0 
                END";
            return await connection.ExecuteScalarAsync<bool>(sql, new { CategoryID = id });
        }

        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();

            string whereCondition = "";
            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                whereCondition = "WHERE (CategoryName LIKE @SearchValue) OR (Description LIKE @SearchValue)";
                parameters.Add("SearchValue", $"%{input.SearchValue}%");
            }

            string sqlCount = $"SELECT COUNT(1) FROM Categories {whereCondition}";
            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, parameters);

            var result = new PagedResult<Category>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = new List<Category>()
            };

            if (rowCount == 0) return result;

            string sqlData;
            if (input.PageSize == 0)
            {
                sqlData = $@"
                    SELECT CategoryID, CategoryName, Description 
                    FROM Categories {whereCondition} 
                    ORDER BY CategoryName";
            }
            else
            {
                sqlData = $@"
                    SELECT CategoryID, CategoryName, Description 
                    FROM Categories {whereCondition} 
                    ORDER BY CategoryName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                parameters.Add("Offset", input.Offset);
                parameters.Add("PageSize", input.PageSize);
            }

            var data = await connection.QueryAsync<Category>(sqlData, parameters);
            result.DataItems = data.ToList();

            return result;
        }

        public async Task<bool> UpdateAsync(Category data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Categories
                SET CategoryName = @CategoryName,
                    Description = @Description
                WHERE CategoryID = @CategoryID";
            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }
    }
}