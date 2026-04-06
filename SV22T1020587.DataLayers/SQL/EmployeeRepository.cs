using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.Models.Common;
using SV22T1020587.Models.HR;
using System.Data;

namespace SV22T1020587.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho nhân viên (Employee) trên CSDL SQL Server
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        public EmployeeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<int> AddAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Employees (FullName, BirthDate, Address, Phone, Email, Photo, IsWorking, RoleNames)
                VALUES (@FullName, @BirthDate, @Address, @Phone, @Email, @Photo, @IsWorking, @RoleNames);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                string sql = "DELETE FROM Employees WHERE EmployeeID = @EmployeeID";
                int rowsAffected = await connection.ExecuteAsync(sql, new { EmployeeID = id });
                return rowsAffected > 0;
            }
            catch (SqlException)
            {
                // Bắt lỗi nếu nhân viên này đang dính khóa ngoại (đã có đơn hàng)
                // Trả về false để Controller nhận biết và báo lỗi ra màn hình thay vì sập web
                return false;
            }
        }

        public async Task<Employee?> GetAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            // TỐI ƯU: Chỉ định rõ các cột thay vì SELECT *
            string sql = "SELECT EmployeeID, FullName, BirthDate, Address, Phone, Email, Photo, IsWorking, RoleNames FROM Employees WHERE EmployeeID = @EmployeeID";
            return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { EmployeeID = id });
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            // Ép kiểu (CAST) tường minh về BIT để Dapper <bool> có thể map chính xác
            string sql = @"SELECT CAST(CASE WHEN EXISTS(SELECT 1 FROM Orders WHERE EmployeeID = @EmployeeID) THEN 1 ELSE 0 END AS BIT)";
            return await connection.ExecuteScalarAsync<bool>(sql, new { EmployeeID = id });
        }

        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);

            // TỐI ƯU: Xây dựng câu lệnh WHERE động để tránh Table Scan
            var parameters = new DynamicParameters();
            string whereCondition = "";

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                whereCondition = "WHERE (FullName LIKE @SearchValue) OR (Phone LIKE @SearchValue) OR (Email LIKE @SearchValue)";
                parameters.Add("SearchValue", $"%{input.SearchValue}%");
            }

            string sqlCount = $"SELECT COUNT(1) FROM Employees {whereCondition}";
            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, parameters);

            var result = new PagedResult<Employee>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = new List<Employee>()
            };

            if (rowCount == 0) return result;

            string sqlData;

            if (input.PageSize == 0)
            {
                // TỐI ƯU: Liệt kê rõ các cột thay vì SELECT *
                sqlData = $@"
                    SELECT EmployeeID, FullName, BirthDate, Address, Phone, Email, Photo, IsWorking, RoleNames 
                    FROM Employees 
                    {whereCondition}
                    ORDER BY FullName";
            }
            else
            {
                // TỐI ƯU: Liệt kê rõ các cột thay vì SELECT *
                sqlData = $@"
                    SELECT EmployeeID, FullName, BirthDate, Address, Phone, Email, Photo, IsWorking, RoleNames 
                    FROM Employees 
                    {whereCondition}
                    ORDER BY FullName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

                parameters.Add("Offset", input.Offset);
                parameters.Add("PageSize", input.PageSize);
            }

            var data = await connection.QueryAsync<Employee>(sqlData, parameters);
            result.DataItems = data.ToList();

            return result;
        }

        public async Task<bool> UpdateAsync(Employee data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Employees
                SET FullName = @FullName,
                    BirthDate = @BirthDate,
                    Address = @Address,
                    Phone = @Phone,
                    Email = @Email,
                    Photo = @Photo,
                    IsWorking = @IsWorking,
                    RoleNames = @RoleNames
                WHERE EmployeeID = @EmployeeID";
            int rowsAffected = await connection.ExecuteAsync(sql, data);
            return rowsAffected > 0;
        }

        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"SELECT COUNT(*) FROM Employees WHERE Email = @Email AND EmployeeID <> @id";
            int count = await connection.ExecuteScalarAsync<int>(sql, new { Email = email, id = id });
            return count == 0;
        }

        /// <summary>
        /// CẬP NHẬT RIÊNG DANH SÁCH QUYỀN
        /// </summary>
        public async Task<bool> UpdateRoleAsync(int employeeID, string roleNames)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"UPDATE Employees 
                           SET RoleNames = @RoleNames 
                           WHERE EmployeeID = @EmployeeID";

            int rowsAffected = await connection.ExecuteAsync(sql, new
            {
                EmployeeID = employeeID,
                RoleNames = roleNames ?? "" // Đề phòng null
            });

            return rowsAffected > 0;
        }
    }
}