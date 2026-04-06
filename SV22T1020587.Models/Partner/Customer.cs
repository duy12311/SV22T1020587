using System.ComponentModel.DataAnnotations;

namespace SV22T1020587.Models.Partner
{
    /// <summary>
    /// Khách hàng
    /// </summary>
    public class Customer
    {
        /// <summary>
        /// Mã khách hàng
        /// </summary>
        public int CustomerID { get; set; }

        /// <summary>
        /// Tên khách hàng
        /// </summary>
        [Required(ErrorMessage = "Vui lòng nhập tên khách hàng.")]
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// Tên giao dịch
        /// </summary>
        public string ContactName { get; set; } = string.Empty;

        /// <summary>
        /// Tỉnh/thành
        /// </summary>
        [Required(ErrorMessage = "Vui lòng chọn tỉnh/thành phố.")]
        public string? Province { get; set; }

        /// <summary>
        /// Địa chỉ
        /// </summary>
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ.")]
        public string? Address { get; set; }

        /// <summary>
        /// Điện thoại
        /// </summary>
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        public string? Phone { get; set; }

        /// <summary>
        /// Email
        /// </summary>
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ Email.")]
        [EmailAddress(ErrorMessage = "Địa chỉ Email không đúng định dạng.")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Khách hàng hiện có bị khóa hay không?
        /// </summary>
        public bool? IsLocked { get; set; }
    }
}