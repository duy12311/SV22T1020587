using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.Models.Common;
using SV22T1020587.Models.Partner;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020587.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho nhà cung cấp (Supplier) trên CSDL SQL Server
    /// </summary>
    public class SupplierRepository : IGenericRepository<Supplier>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo lớp repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối đến CSDL SQL Server</param>
        public SupplierRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Bổ sung một nhà cung cấp mới vào cơ sở dữ liệu
        /// </summary>
        public async Task<int> AddAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                INSERT INTO Suppliers (SupplierName, ContactName, Province, Address, Phone, Email)
                VALUES (@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Xóa nhà cung cấp dựa vào mã nhà cung cấp
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = "DELETE FROM Suppliers WHERE SupplierID = @SupplierID";

            int rowsAffected = await connection.ExecuteAsync(sql, new { SupplierID = id });
            return rowsAffected > 0;
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhà cung cấp
        /// </summary>
        public async Task<Supplier?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            // TỐI ƯU: Liệt kê rõ các cột thay vì SELECT *
            string sql = @"
                SELECT SupplierID, SupplierName, ContactName, Province, Address, Phone, Email 
                FROM Suppliers 
                WHERE SupplierID = @SupplierID";

            return await connection.QueryFirstOrDefaultAsync<Supplier>(sql, new { SupplierID = id });
        }

        /// <summary>
        /// Kiểm tra xem nhà cung cấp có dữ liệu liên quan (trong bảng Products) hay không
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                SELECT CASE 
                    WHEN EXISTS(SELECT 1 FROM Products WHERE SupplierID = @SupplierID) THEN 1 
                    ELSE 0 
                END";

            return await connection.ExecuteScalarAsync<bool>(sql, new { SupplierID = id });
        }

        /// <summary>
        /// Truy vấn, tìm kiếm và phân trang danh sách nhà cung cấp
        /// </summary>
        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            // TỐI ƯU: Xây dựng truy vấn động để tránh bị Table Scan
            var parameters = new DynamicParameters();
            string whereCondition = "";

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                whereCondition = @"WHERE (SupplierName LIKE @SearchValue) 
                                      OR (ContactName LIKE @SearchValue) 
                                      OR (Phone LIKE @SearchValue)";
                parameters.Add("SearchValue", $"%{input.SearchValue}%");
            }

            string sqlCount = $"SELECT COUNT(1) FROM Suppliers {whereCondition}";

            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, parameters);

            var result = new PagedResult<Supplier>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = new List<Supplier>()
            };

            if (rowCount == 0) return result;

            string sqlData;

            if (input.PageSize == 0)
            {
                // TỐI ƯU: Liệt kê rõ các cột thay vì SELECT *
                sqlData = $@"
                    SELECT SupplierID, SupplierName, ContactName, Province, Address, Phone, Email 
                    FROM Suppliers 
                    {whereCondition}
                    ORDER BY SupplierName";
            }
            else
            {
                // TỐI ƯU: Liệt kê rõ các cột thay vì SELECT *
                sqlData = $@"
                    SELECT SupplierID, SupplierName, ContactName, Province, Address, Phone, Email 
                    FROM Suppliers 
                    {whereCondition}
                    ORDER BY SupplierName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                parameters.Add("Offset", input.Offset);
                parameters.Add("PageSize", input.PageSize);
            }

            var data = await connection.QueryAsync<Supplier>(sqlData, parameters);
            result.DataItems = data.ToList();

            return result;
        }

        /// <summary>
        /// Cập nhật thông tin của một nhà cung cấp
        /// </summary>
        public async Task<bool> UpdateAsync(Supplier data)
        {
            using var connection = new SqlConnection(_connectionString);

            string sql = @"
                UPDATE Suppliers
                SET SupplierName = @SupplierName,
                    ContactName = @ContactName,
                    Province = @Province,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email
                WHERE SupplierID = @SupplierID";

            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }
    }
}