using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.Models.Common;
using SV22T1020587.Models.Partner;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020587.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho khách hàng (Customer) trên CSDL SQL Server
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);
            data.Email = data.Email?.Trim().ToLowerInvariant();

            string sql = @"
                INSERT INTO Customers (CustomerName, ContactName, Province, Address, Phone, Email, IsLocked)
                VALUES (@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @IsLocked);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Customers WHERE CustomerID = @CustomerID";
            return await connection.ExecuteAsync(sql, new { CustomerID = id }) > 0;
        }

        public async Task<Customer?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "SELECT CustomerID, CustomerName, ContactName, Province, Address, Phone, Email, IsLocked FROM Customers WHERE CustomerID = @CustomerID";
            return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { CustomerID = id });
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT CASE 
                    WHEN EXISTS(SELECT 1 FROM Orders WHERE CustomerID = @CustomerID) THEN 1 
                    ELSE 0 
                END";
            return await connection.ExecuteScalarAsync<bool>(sql, new { CustomerID = id });
        }

        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            string whereCondition = "";

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                whereCondition = @"WHERE (CustomerName LIKE @SearchValue) 
                                      OR (ContactName LIKE @SearchValue) 
                                      OR (Phone LIKE @SearchValue) 
                                      OR (Email LIKE @SearchValue)";
                parameters.Add("SearchValue", $"%{input.SearchValue}%");
            }

            string sqlCount = $"SELECT COUNT(1) FROM Customers {whereCondition}";
            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, parameters);

            var result = new PagedResult<Customer>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = new List<Customer>()
            };

            if (rowCount == 0) return result;

            string sqlData;

            if (input.PageSize == 0)
            {
                sqlData = $@"
                    SELECT CustomerID, CustomerName, ContactName, Province, Address, Phone, Email, IsLocked 
                    FROM Customers 
                    {whereCondition}
                    ORDER BY CustomerName";
                var data = await connection.QueryAsync<Customer>(sqlData, parameters);
                result.DataItems = data?.ToList() ?? new List<Customer>();
            }
            else
            {
                sqlData = $@"
                    SELECT CustomerID, CustomerName, ContactName, Province, Address, Phone, Email, IsLocked 
                    FROM Customers 
                    {whereCondition}
                    ORDER BY CustomerName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
                parameters.Add("Offset", input.Offset);
                parameters.Add("PageSize", input.PageSize);
                var data = await connection.QueryAsync<Customer>(sqlData, parameters);
                result.DataItems = data?.ToList() ?? new List<Customer>();
            }

            return result;
        }

        public async Task<bool> UpdateAsync(Customer data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Customers
                SET CustomerName = @CustomerName,
                    ContactName = @ContactName,
                    Province = @Province,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    IsLocked = @IsLocked
                WHERE CustomerID = @CustomerID";
            return await connection.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> IsValidEmailAsync(string email, int id = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT COUNT(*) 
                FROM Customers 
                WHERE Email = @Email AND CustomerID <> @id";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, id = id });
            return count == 0;
        }

        public async Task<bool> ChangePasswordAsync(int customerID, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Customers 
                SET Password = @Password 
                WHERE CustomerID = @CustomerID";
            return await connection.ExecuteAsync(sql, new { CustomerID = customerID, Password = password }) > 0;
        }

        public async Task<Customer?> AuthorizeAsync(string email, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            email = email?.Trim().ToLowerInvariant();

            // Đã bỏ IsLocked = 0 để lấy được tài khoản lên kiểm tra
            string sql = @"
                SELECT CustomerID, CustomerName, ContactName, Province, Address, Phone, Email, IsLocked
                FROM Customers
                WHERE Email = @Email AND Password = @Password";

            return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { Email = email, Password = password });
        }
    }
}