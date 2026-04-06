using SV22T1020587.Models.Common; // Thay đổi tùy theo namespace của bạn

namespace SV22T1020587.Models.Sales
{
    public class OrderSearchInput : PaginationSearchInput // Kế thừa class chứa Page, PageSize, SearchValue
    {
        // Thêm trạng thái đơn hàng (0 là tất cả)
        public int Status { get; set; } = 0;

        // Thay vì DateTime?, dùng string? để hứng định dạng dd/MM/yyyy từ Web
        public string? DateFrom { get; set; }
        public string? DateTo { get; set; }
    }
}