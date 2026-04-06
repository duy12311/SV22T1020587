using SV22T1020587.Models.HR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SV22T1020587.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu trên Employee
    /// </summary>
    public interface IEmployeeRepository : IGenericRepository<Employee>
    {
        /// <summary>
        /// Kiểm tra xem email của nhân viên có hợp lệ không (tránh trùng lặp)
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: Kiểm tra email của nhân viên mới
        /// Nếu id <> 0: Kiểm tra email của nhân viên có mã là id
        /// </param>
        /// <returns>True nếu email hợp lệ (chưa tồn tại), False nếu đã tồn tại</returns>
        Task<bool> ValidateEmailAsync(string email, int id = 0);

        /// <summary>
        /// Cập nhật danh sách quyền cho nhân viên
        /// </summary>
        /// <param name="employeeID">Mã nhân viên</param>
        /// <param name="roleNames">Chuỗi các quyền cách nhau bởi dấu phẩy (vd: "admin,ManageOrders")</param>
        /// <returns>True nếu cập nhật thành công</returns>
        Task<bool> UpdateRoleAsync(int employeeID, string roleNames);
    }
}