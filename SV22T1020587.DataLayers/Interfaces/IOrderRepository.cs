using SV22T1020587.Models.Common;
using SV22T1020587.Models.Sales;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SV22T1020587.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các chức năng xử lý dữ liệu cho đơn hàng
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>
        /// Khởi tạo một đơn hàng mới (bao gồm thông tin đơn hàng và danh sách chi tiết)
        /// customerID có thể là NULL => không cần FK với bảng Customers khi không lưu khách mới
        /// </summary>
        // Đã sửa: int employeeID -> int? employeeID
        int InitOrder(int? employeeID, int? customerID, string orderType, string deliveryProvince, string deliveryAddress, List<OrderDetail> details, string? customerName = null, string? customerPhone = null, string? paymentMethod = null);

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input);

        /// <summary>
        /// Lấy danh sách đơn hàng của 1 khách hàng (không phân trang)
        /// </summary>
        Task<List<OrderViewInfo>> ListByCustomerAsync(int customerID);

        /// <summary>
        /// Lấy thông tin 1 đơn hàng
        /// </summary>
        Task<OrderViewInfo?> GetAsync(int orderID);

        /// <summary>
        /// Bổ sung đơn hàng (thông tin chung)
        /// </summary>
        Task<int> AddAsync(Order data);

        /// <summary>
        /// Cập nhật thông tin đơn hàng
        /// </summary>
        Task<bool> UpdateAsync(Order data);

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        Task<bool> DeleteAsync(int orderID);

        /// <summary>
        /// Lấy danh sách mặt hàng (chi tiết) trong đơn hàng
        /// </summary>
        Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID);

        /// <summary>
        /// Lấy thông tin chi tiết của một mặt hàng cụ thể trong đơn hàng
        /// </summary>
        Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID);

        /// <summary>
        /// Bổ sung mặt hàng vào đơn hàng
        /// </summary>
        Task<bool> AddDetailAsync(OrderDetail data);

        /// <summary>
        /// Cập nhật số lượng và giá bán của một mặt hàng trong đơn hàng
        /// </summary>
        Task<bool> UpdateDetailAsync(OrderDetail data);

        /// <summary>
        /// Xóa một mặt hàng khỏi đơn hàng
        /// </summary>
        Task<bool> DeleteDetailAsync(int orderID, int productID);

        // ==========================================
        // CÁC HÀM XỬ LÝ TRẠNG THÁI ĐƠN HÀNG
        // ==========================================

        /// <summary>
        /// Duyệt đơn hàng (chuyển trạng thái = 2) và ghi nhận mã nhân viên duyệt
        /// </summary>
        bool Accept(int orderID, int employeeID);

        /// <summary>
        /// Chuyển sang đang giao hàng (chuyển trạng thái = 3)
        /// </summary>
        bool Ship(int orderID, int shipperID);

        /// <summary>
        /// Xác nhận hoàn tất đơn hàng (chuyển trạng thái = 4)
        /// </summary>
        bool Finish(int orderID);

        /// <summary>
        /// Đánh dấu đã lập hóa đơn (chuyển trạng thái = 5)
        /// </summary>
        bool IssueInvoice(int orderID);

        /// <summary>
        /// Hủy đơn hàng (chuyển trạng thái = -1)
        /// </summary>
        bool Cancel(int orderID);

        /// <summary>
        /// Từ chối đơn hàng (chuyển trạng thái = -2)
        /// </summary>
        bool Reject(int orderID);

        bool Issue(int orderID);
    }
}