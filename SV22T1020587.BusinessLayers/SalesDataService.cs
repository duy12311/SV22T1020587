using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.DataLayers.SQLServer;
using SV22T1020587.Models.Common;
using SV22T1020587.Models.Partner;
using SV22T1020587.Models.Sales;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace SV22T1020587.BusinessLayers
{
    public static class SalesDataService
    {
        private static readonly IOrderRepository orderDB;

        static SalesDataService()
        {
            orderDB = new OrderRepository(Configuration.ConnectionString);
        }

        public static int InitOrder(int employeeID, int? customerID, string orderType, string deliveryProvince, string deliveryAddress, List<CartItem> cart, string? customerName = null, string? customerPhone = null, string? paymentMethod = null)
        {
            return InitOrderAsync(employeeID, customerID, orderType, deliveryProvince, deliveryAddress, cart, customerName, customerPhone, paymentMethod).GetAwaiter().GetResult();
        }

        public static async Task<int> InitOrderAsync(int employeeID, int? customerID, string orderType, string deliveryProvince, string deliveryAddress, List<CartItem> cart, string? customerName = null, string? customerPhone = null, string? paymentMethod = null)
        {
            if (cart == null || cart.Count == 0)
                return 0;

            // Đảm bảo ID không hợp lệ thì gán bằng null
            if (customerID <= 0)
            {
                customerID = null;
            }

            // Convert giỏ hàng sang chi tiết đơn hàng
            List<OrderDetail> details = cart.Select(item => new OrderDetail
            {
                ProductID = item.ProductID,
                Quantity = item.Quantity,
                SalePrice = item.SalePrice
            }).ToList();

            // Lưu thẳng vào đơn hàng, KHÔNG gọi PartnerDataService.AddCustomerAsync nữa
            return await Task.Run(() => orderDB.InitOrder(employeeID, customerID, orderType, deliveryProvince, deliveryAddress, details, customerName, customerPhone, paymentMethod));
        }

        #region Nghiệp vụ liên quan đến Đơn hàng (Orders)

        public static async Task<PagedResult<OrderViewInfo>> ListOrdersAsync(OrderSearchInput input) => await orderDB.ListAsync(input);

        public static async Task<List<OrderViewInfo>> ListOrdersForCustomerAsync(int customerID) => await orderDB.ListByCustomerAsync(customerID);

        public static async Task<OrderViewInfo?> GetOrderAsync(int id) => await orderDB.GetAsync(id);

        public static async Task<int> AddOrderAsync(Order data) => await orderDB.AddAsync(data);

        public static async Task<bool> UpdateOrderAsync(Order data) => await orderDB.UpdateAsync(data);

        public static bool DeleteOrder(int id) => orderDB.DeleteAsync(id).Result;

        #endregion

        #region Nghiệp vụ liên quan đến Chi tiết Đơn hàng (OrderDetails)

        public static async Task<List<OrderDetailViewInfo>> ListOrderDetailsAsync(int orderId) => await orderDB.ListDetailsAsync(orderId);

        public static async Task<OrderDetailViewInfo?> GetOrderDetailAsync(int orderId, int productId) => await orderDB.GetDetailAsync(orderId, productId);

        public static async Task<bool> AddOrderDetailAsync(OrderDetail data) => await orderDB.AddDetailAsync(data);

        public static async Task<bool> UpdateOrderDetailAsync(OrderDetail data) => await orderDB.UpdateDetailAsync(data);

        public static bool DeleteOrderDetail(int orderId, int productId) => orderDB.DeleteDetailAsync(orderId, productId).Result;

        public static OrderDetailViewInfo? GetOrderDetail(int orderId, int productId)
        {
            return orderDB.GetDetailAsync(orderId, productId).Result;
        }

        public static bool SaveOrderDetail(int orderID, int productID, int quantity, decimal salePrice)
        {
            var data = new OrderDetail
            {
                OrderID = orderID,
                ProductID = productID,
                Quantity = quantity,
                SalePrice = salePrice
            };
            return orderDB.UpdateDetailAsync(data).Result;
        }

        #endregion

        #region Nghiệp vụ xử lý trạng thái Đơn hàng

        public static bool AcceptOrder(int orderID, int employeeID) => orderDB.Accept(orderID, employeeID);

        public static bool ShipOrder(int orderID, int shipperID) => orderDB.Ship(orderID, shipperID);

        public static bool FinishOrder(int orderID) => orderDB.Finish(orderID);

        public static bool CancelOrder(int orderID) => orderDB.Cancel(orderID);

        public static bool RejectOrder(int orderID) => orderDB.Reject(orderID);

        public static bool IssueOrderInvoice(int orderID) => orderDB.Issue(orderID);

        #endregion
    }
}