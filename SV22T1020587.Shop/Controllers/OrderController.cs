using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020587.BusinessLayers;
using SV22T1020587.Models.Order;
using SV22T1020587.Models.Sales;
using System.Security.Claims;
using SV22T1020587.Shop.AppCodes;
using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.DataLayers.SQLServer;

namespace SV22T1020587.Shop.Controllers
{
    [Authorize] // Yêu cầu đăng nhập để xem lịch sử đơn hàng
    public class OrderController : Controller
    {
        private const string SHOPPING_CART = "ShoppingCart";
        private const string CHECKOUT_CART = "CheckoutCart";

        // Khai báo Repo để đọc giỏ hàng từ Database nếu người dùng đã đăng nhập
        private readonly ICustomerCartRepository _cartRepo;

        public OrderController()
        {
            _cartRepo = new CustomerCartRepository(Configuration.ConnectionString);
        }

        // Lấy giỏ hàng từ Session (cho khách chưa đăng nhập)
        private List<CartItem> GetSessionCart() => ApplicationContext.GetSessionData<List<CartItem>>(SHOPPING_CART) ?? new List<CartItem>();

        // --- HÀM TRỢ GIÚP: Load giỏ hàng chuẩn ---
        private async Task<List<CartItem>> LoadCartForCurrentUserAsync()
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out int customerId))
                {
                    return await _cartRepo.GetCartAsync(customerId) ?? new List<CartItem>();
                }
            }
            return GetSessionCart();
        }

        // --- HÀM TRỢ GIÚP: Xóa các mặt hàng ĐÃ MUA khỏi giỏ ---
        private async Task RemovePurchasedItemsAsync(List<CartItem> purchasedItems)
        {
            var currentCart = await LoadCartForCurrentUserAsync();
            // Chỉ xóa những món nằm trong hóa đơn vừa thanh toán
            currentCart.RemoveAll(x => purchasedItems.Any(p => p.ProductID == x.ProductID));

            if (User.Identity?.IsAuthenticated ?? false)
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out int customerId))
                {
                    await _cartRepo.SaveCartAsync(customerId, currentCart);
                }
            }
            else
            {
                ApplicationContext.SetSessionData(SHOPPING_CART, currentCart);
            }
        }

        // GET: /Order/Checkout 
        [AllowAnonymous]
        public async Task<IActionResult> Checkout([FromQuery] int[]? selectedProductID)
        {
            List<CartItem> cart = await LoadCartForCurrentUserAsync();

            if (selectedProductID != null && selectedProductID.Length > 0)
            {
                var filtered = cart.Where(i => selectedProductID.Contains(i.ProductID)).ToList();
                if (filtered == null || filtered.Count == 0)
                    return RedirectToAction("Index", "Cart");

                // Lưu các món đã chọn vào Session tạm để chuẩn bị Init Order
                ApplicationContext.SetSessionData(CHECKOUT_CART, filtered);

                // Trả về Ok() vì request này được gọi ngầm từ JS để set Session
                return Ok();
            }

            // Xóa session checkout nếu truy cập không có tham số
            ApplicationContext.SetSessionData(CHECKOUT_CART, null as List<CartItem>);

            if (cart == null || cart.Count == 0)
            {
                return RedirectToAction("Index", "Cart");
            }

            return View(cart);
        }

        // POST: /Order/Init (Xử lý đặt hàng qua AJAX)
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Init(OrderInfo customerData)
        {
            // Ưu tiên giỏ hàng Checkout (những món đã tích chọn)
            var checkoutCart = ApplicationContext.GetSessionData<List<CartItem>>(CHECKOUT_CART);
            var cart = (checkoutCart != null && checkoutCart.Count > 0) ? checkoutCart : await LoadCartForCurrentUserAsync();

            if (cart == null || cart.Count == 0)
            {
                return BadRequest("Giỏ hàng của bạn đang trống hoặc phiên làm việc đã hết hạn. Vui lòng tải lại trang.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest("Vui lòng điền đầy đủ thông tin giao hàng.");
            }

            int? customerID = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out int cid)) customerID = cid;
            }

            int orderId = SalesDataService.InitOrder(0, customerID, "delivery", customerData.DeliveryProvince ?? "", customerData.DeliveryAddress ?? "", cart, customerData.CustomerName, customerData.Phone, customerData.PaymentMethod);

            if (orderId > 0)
            {
                // Cập nhật thông tin khách hàng nếu có thay đổi
                if (customerID.HasValue)
                {
                    try
                    {
                        var customer = await PartnerDataService.GetCustomerAsync(customerID.Value);
                        if (customer != null)
                        {
                            if (!string.IsNullOrWhiteSpace(customerData.CustomerName)) customer.CustomerName = customerData.CustomerName;
                            if (!string.IsNullOrWhiteSpace(customerData.Phone)) customer.Phone = customerData.Phone;
                            if (!string.IsNullOrWhiteSpace(customerData.DeliveryProvince)) customer.Province = customerData.DeliveryProvince;
                            if (!string.IsNullOrWhiteSpace(customerData.DeliveryAddress)) customer.Address = customerData.DeliveryAddress;

                            await PartnerDataService.UpdateCustomerAsync(customer);
                        }
                    }
                    catch { }
                }

                // CHỈ xóa những món đã đặt thành công khỏi giỏ hàng chính
                await RemovePurchasedItemsAsync(cart);
                ApplicationContext.SetSessionData(CHECKOUT_CART, null as List<CartItem>);

                // Trả về redirect cho client tự chuyển hướng sang trang Success
                return RedirectToAction("Success");
            }

            // Trả về BadRequest để JS Modal bắt được và in ra dòng chữ đỏ
            return BadRequest("Không thể lưu đơn hàng vào lúc này. Vui lòng thử lại sau.");
        }

        // GET: /Order/Success (Trang thông báo đặt hàng thành công)
        [AllowAnonymous]
        public IActionResult Success()
        {
            return View();
        }

        // GET: /Order/MyOrders - danh sách đơn hàng
        public async Task<IActionResult> MyOrders()
        {
            if (!User.Identity?.IsAuthenticated ?? true) return RedirectToAction("Login", "Account");
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int customerID)) return RedirectToAction("Login", "Account");

            var orders = await SalesDataService.ListOrdersForCustomerAsync(customerID);
            return View("~/Views/Order/MyOrders.cshtml", orders);
        }

        // GET: /Order/Detail/123 - xem chi tiết đơn hàng
        public async Task<IActionResult> Detail(int id)
        {
            if (!User.Identity?.IsAuthenticated ?? true) return RedirectToAction("Login", "Account");
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int customerID)) return RedirectToAction("Login", "Account");

            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return RedirectToAction("MyOrders");

            if (order.CustomerID != customerID)
            {
                return Forbid();
            }

            ViewBag.Details = await SalesDataService.ListOrderDetailsAsync(id);
            return View("~/Views/Order/Details.cshtml", order);
        }
    }
}