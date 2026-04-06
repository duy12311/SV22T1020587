using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.DataLayers.SQLServer;
using SV22T1020587.Models.Security;

namespace SV22T1020587.BusinessLayers
{
    /// <summary>
    /// Các dịch vụ liên quan đến bảo mật và tài khoản (Đăng nhập, Đổi mật khẩu)
    /// </summary>
    public static class SecurityDataService
    {
        private static readonly IUserAccountRepository employeeAccountDB;
        private static readonly IUserAccountRepository customerAccountDB;

        static SecurityDataService()
        {
            // Khởi tạo các repository kết nối với SQL Server
            employeeAccountDB = new EmployeeAccountRepository(Configuration.ConnectionString);
            customerAccountDB = new CustomerAccountRepository(Configuration.ConnectionString);
        }

        #region Tài khoản Nhân viên (Dùng cho trang Admin)

        /// <summary>
        /// Xác thực đăng nhập cho nhân viên
        /// </summary>
        public static async Task<UserAccount?> AuthorizeEmployeeAsync(string userName, string password)
            => await employeeAccountDB.Authorize(userName, password);

        /// <summary>
        /// Đổi mật khẩu cho nhân viên
        /// </summary>
        public static async Task<bool> ChangeEmployeePasswordAsync(string userName, string newPassword)
            => await employeeAccountDB.ChangePassword(userName, newPassword);

        #endregion

        #region Tài khoản Khách hàng (Dùng cho trang Shop/Frontend)

        /// <summary>
        /// Xác thực đăng nhập cho khách hàng
        /// </summary>
        public static async Task<UserAccount?> AuthorizeCustomerAsync(string userName, string password)
            => await customerAccountDB.Authorize(userName, password);

        /// <summary>
        /// Đổi mật khẩu cho khách hàng
        /// </summary>
        public static async Task<bool> ChangeCustomerPasswordAsync(string userName, string newPassword)
            => await customerAccountDB.ChangePassword(userName, newPassword);

        #endregion
    }
}