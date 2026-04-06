namespace SV22T1020587.Models.Sales
{
    public class OrderViewInfo : Order
    {
        public string EmployeeName { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string CustomerContactName { get; set; } = "";
        public string CustomerEmail { get; set; } = "";
        public string CustomerPhone { get; set; } = "";
        public string CustomerAddress { get; set; } = "";
        public string ShipperName { get; set; } = "";
        public string ShipperPhone { get; set; } = "";

        // Status lấy từ lớp cha Order, ta chỉ tạo thêm mô tả bằng chữ:
        public string StatusDescription => Status switch
        {
            1 => "Chờ duyệt",
            2 => "Đã duyệt (Chờ giao hàng)",
            3 => "Đang giao hàng",
            4 => "Thành công",
            -1 => "Đã hủy",
            -2 => "Bị từ chối",
            _ => "Không xác định"
        };
    }
}