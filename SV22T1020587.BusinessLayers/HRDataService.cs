using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.DataLayers.SQLServer;
using SV22T1020587.Models.Common;
using SV22T1020587.Models.HR;

namespace SV22T1020587.BusinessLayers
{
    public static class HRDataService
    {
        private static readonly IEmployeeRepository employeeDB;

        static HRDataService()
        {
            // Đảm bảo ConnectionString đã được cấu hình trong lớp Configuration
            employeeDB = new EmployeeRepository(Configuration.ConnectionString);
        }

        /// <summary>
        /// Tìm kiếm và lấy danh sách nhân viên dưới dạng phân trang
        /// </summary>
        public static async Task<PagedResult<Employee>> ListEmployeesAsync(PaginationSearchInput input)
            => await employeeDB.ListAsync(input);

        /// <summary>
        /// Lấy thông tin chi tiết của một nhân viên dựa trên ID
        /// </summary>
        public static async Task<Employee?> GetEmployeeAsync(int id)
            => await employeeDB.GetAsync(id);

        /// <summary>
        /// Bổ sung một nhân viên mới
        /// </summary>
        public static async Task<int> AddEmployeeAsync(Employee data)
            => await employeeDB.AddAsync(data);

        /// <summary>
        /// Cập nhật thông tin của nhân viên
        /// </summary>
        public static async Task<bool> UpdateEmployeeAsync(Employee data)
            => await employeeDB.UpdateAsync(data);

        /// <summary>
        /// Xóa nhân viên nếu không có dữ liệu liên quan (không bị dùng ở bảng khác)
        /// </summary>
        public static async Task<bool> DeleteEmployeeAsync(int id)
        {
            if (await employeeDB.IsUsedAsync(id)) return false;
            return await employeeDB.DeleteAsync(id);
        }

        /// <summary>
        /// Kiểm tra Email có trùng với nhân viên khác hay không
        /// </summary>
        public static async Task<bool> ValidateEmployeeEmailAsync(string email, int id = 0)
            => await employeeDB.ValidateEmailAsync(email, id);

        /// <summary>
        /// Cập nhật danh sách quyền cho nhân viên
        /// Hàm này giúp thực hiện nhanh việc phân quyền mà không cần nạp lại toàn bộ thông tin nhân viên
        /// </summary>
        public static async Task<bool> UpdateRoleAsync(int employeeID, string roleNames)
        {
            // Lưu ý: Bạn cần đảm bảo trong IEmployeeRepository và EmployeeRepository 
            // đã định nghĩa phương thức UpdateRoleAsync(id, roles)
            return await employeeDB.UpdateRoleAsync(employeeID, roleNames);
        }
    }
}