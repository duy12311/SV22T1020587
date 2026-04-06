using SV22T1020587.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020587.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu trên Customer
    /// </summary>
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        /// <summary>
        /// Kiểm tra xem một địa chỉ email có hợp lệ (không bị trùng) hay không?
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: Kiểm tra email của khách hàng mới.
        /// Nếu id <> 0: Kiểm tra email đối với khách hàng đã tồn tại
        /// </param>
        Task<bool> IsValidEmailAsync(string email, int id = 0);

        /// <summary>
        /// Thay đổi mật khẩu của khách hàng
        /// </summary>
        /// <param name="customerID">Mã khách hàng cần đổi mật khẩu</param>
        /// <param name="password">Mật khẩu mới (Nên được mã hóa trước khi truyền vào)</param>
        /// <returns>True nếu cập nhật thành công, False nếu thất bại</returns>
        Task<bool> ChangePasswordAsync(int customerID, string password);

        /// <summary>
        /// Xác thực khách hàng dựa trên email và mật khẩu.
        /// Trả về Customer nếu hợp lệ, null nếu không hợp lệ.
        /// Lưu ý: mật khẩu nên được mã hóa/so sánh an toàn trong implementation.
        /// </summary>
        Task<Customer?> AuthorizeAsync(string email, string password);
    }
}