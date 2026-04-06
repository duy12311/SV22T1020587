using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.Models.Sales;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace SV22T1020587.DataLayers.SQLServer
{
    /// <summary>
    /// Simple JSON-backed per-customer cart repository.
    /// Requires table: CustomerCarts(CustomerID int PRIMARY KEY, CartJson nvarchar(max))
    /// </summary>
    public class CustomerCartRepository : ICustomerCartRepository
    {
        private readonly string _connectionString;

        public CustomerCartRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<List<CartItem>> GetCartAsync(int customerId)
        {
            using var conn = new SqlConnection(_connectionString);
            string sql = "SELECT CartJson FROM CustomerCarts WHERE CustomerID = @CustomerID";
            var json = await conn.QueryFirstOrDefaultAsync<string?>(sql, new { CustomerID = customerId });
            if (string.IsNullOrWhiteSpace(json)) return new List<CartItem>();
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                var items = JsonSerializer.Deserialize<List<CartItem>>(json, options);
                return items ?? new List<CartItem>();
            }
            catch
            {
                return new List<CartItem>();
            }
        }

        public async Task SaveCartAsync(int customerId, List<CartItem> cart)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
            var json = JsonSerializer.Serialize(cart ?? new List<CartItem>(), options);
            using var conn = new SqlConnection(_connectionString);
            string sql = @"
IF EXISTS(SELECT 1 FROM CustomerCarts WHERE CustomerID = @CustomerID)
    UPDATE CustomerCarts SET CartJson = @CartJson WHERE CUSTOMERID = @CustomerID;
ELSE
    INSERT INTO CustomerCarts(CustomerID, CartJson) VALUES(@CustomerID, @CartJson);";
            await conn.ExecuteAsync(sql, new { CustomerID = customerId, CartJson = json });
        }

        public async Task ClearCartAsync(int customerId)
        {
            using var conn = new SqlConnection(_connectionString);
            string sql = "DELETE FROM CustomerCarts WHERE CustomerID = @CustomerID";
            await conn.ExecuteAsync(sql, new { CustomerID = customerId });
        }
    }
}