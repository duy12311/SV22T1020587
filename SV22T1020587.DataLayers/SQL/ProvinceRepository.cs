using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.Models.DataDictionary;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SV22T1020587.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt phép xử lý dữ liệu cho Tỉnh/Thành trên CSDL SQL Server
    /// </summary>
    public class ProvinceRepository : IDataDictionaryRepository<Province>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo lớp repository với chuỗi kết nối
        /// </summary>
        public ProvinceRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Lấy danh sách tất cả các tỉnh/thành
        /// </summary>
        /// <returns>Danh sách các đối tượng Province</returns>
        public async Task<List<Province>> ListAsync()
        {
            using var connection = new SqlConnection(_connectionString);

            // TỐI ƯU: Truy vấn đã được tối ưu do chỉ SELECT đúng cột cần thiết (ProvinceName)
            string sql = "SELECT ProvinceName FROM Provinces ORDER BY ProvinceName";

            var data = await connection.QueryAsync<Province>(sql);
            return data.ToList();
        }
    }
}