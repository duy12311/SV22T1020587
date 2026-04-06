using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020587.Admin;
using SV22T1020587.BusinessLayers;
using SV22T1020587.Models.Catalog;
using SV22T1020587.Models.Common;
using SV22T1020587.Models.Order;
using SV22T1020587.Models.Sales;
using SV22T1020587.Admin.AppCodes;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;

namespace SV22T1020587.Web.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private const int PAGE_SIZE = 20;
        private const string SHOPPING_CART = "ShoppingCart";
        private const int DEFAULT_CUSTOMER_ID = 1;

        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>("OrderSearch");
            if (input == null)
            {
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = PAGE_SIZE,
                    SearchValue = "",
                    Status = 0,
                    DateFrom = DateTime.Today.AddMonths(-1).ToString("dd/MM/yyyy"),
                    DateTo = DateTime.Today.ToString("dd/MM/yyyy")
                };
            }
            return View(input);
        }

        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            var data = await SalesDataService.ListOrdersAsync(input);
            ApplicationContext.SetSessionData("OrderSearch", input);
            return View(data);
        }

        public async Task<IActionResult> Create()
        {
            var customerInput = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = -1,
                SearchValue = ""
            };

            var customersResult = await PartnerDataService.ListCustomersAsync(customerInput);
            ViewBag.Customers = customersResult.DataItems;
            ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync();

            var cart = GetCart();
            return View(cart);
        }

        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ViewBag.RowCount = result.RowCount;
            ViewBag.CurrentPage = input.Page;
            ApplicationContext.SetSessionData("ProductSearchForOrder", input);
            return View(result);
        }

        public IActionResult ShowShoppingCart()
        {
            return PartialView(GetCart());
        }

        [HttpPost]
        public async Task<IActionResult> Init(int? customerID, string orderType, string customerName, string customerPhone, string deliveryProvince, string deliveryAddress)
        {
            var cart = GetCart();
            if (cart.Count == 0)
                return Json(new { success = false, message = "Giỏ hàng đang trống" });

            int employeeID = GetCurrentEmployeeID();
            if (employeeID <= 0)
                return Json(new { success = false, message = "Không lấy được thông tin nhân viên đăng nhập." });

            if (!customerID.HasValue || customerID.Value <= 0)
            {
                if (string.IsNullOrWhiteSpace(customerName))
                    return Json(new { success = false, message = "Vui lòng chọn hoặc nhập tên khách hàng!" });

                customerID = null;
            }

            if (orderType == "delivery")
            {
                if (string.IsNullOrWhiteSpace(deliveryProvince) || string.IsNullOrWhiteSpace(deliveryAddress))
                    return Json(new { success = false, message = "Vui lòng nhập đầy đủ tỉnh/thành và địa chỉ giao hàng" });
            }
            else
            {
                deliveryProvince = "";
                deliveryAddress = "";
            }

            try
            {
                int orderID = await SalesDataService.InitOrderAsync(employeeID, customerID, orderType, deliveryProvince, deliveryAddress, cart, customerName, customerPhone);
                if (orderID > 0)
                {
                    ApplicationContext.SetSessionData(SHOPPING_CART, new List<CartItem>());
                    return Json(new { success = true, orderID = orderID });
                }
                return Json(new { success = false, message = "Không thể lập đơn hàng" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi Database: " + ex.Message });
            }
        }

        [HttpPost]
        public IActionResult AddToCart(CartItem item)
        {
            if (item.SalePrice <= 0 || item.Quantity <= 0)
                return Json("Giá bán và số lượng không hợp lệ");

            var cart = GetCart();
            var existsItem = cart.FirstOrDefault(m => m.ProductID == item.ProductID);
            if (existsItem == null) cart.Add(item);
            else
            {
                existsItem.Quantity += item.Quantity;
                existsItem.SalePrice = item.SalePrice;
            }
            ApplicationContext.SetSessionData(SHOPPING_CART, cart);
            return Json("");
        }

        [HttpPost]
        public IActionResult UpdateCart(int productID, int quantity)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(x => x.ProductID == productID);
            if (item != null)
            {
                if (quantity <= 0) cart.Remove(item);
                else item.Quantity = quantity;
            }
            ApplicationContext.SetSessionData(SHOPPING_CART, cart);
            return Json("");
        }

        public IActionResult RemoveFromCart(int id)
        {
            var cart = GetCart();
            int index = cart.FindIndex(m => m.ProductID == id);
            if (index >= 0) cart.RemoveAt(index);
            ApplicationContext.SetSessionData(SHOPPING_CART, cart);
            return Json("");
        }

        public IActionResult ClearCart()
        {
            ApplicationContext.SetSessionData(SHOPPING_CART, new List<CartItem>());
            return Json("");
        }

        public async Task<IActionResult> Detail(int id)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return RedirectToAction("Index");
            ViewBag.Details = await SalesDataService.ListOrderDetailsAsync(id);
            return View(order);
        }

        // GET: /Order/EditDetail/{orderId}?productId=123
        public async Task<IActionResult> EditDetail(int id, int productId)
        {
            var detail = await SalesDataService.GetOrderDetailAsync(id, productId);
            if (detail == null) return Content("<div class='alert alert-warning'>Không tìm thấy chi tiết này.</div>", "text/html");
            return PartialView("EditDetail", detail);
        }

        // POST: /Order/UpdateDetail  -> update quantity only
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateDetail(int orderID, int productID, int quantity)
        {
            if (quantity <= 0)
            {
                return Content("<div class='alert alert-danger'>Số lượng không hợp lệ.</div>", "text/html");
            }

            // Read existing detail to preserve salePrice
            var existing = await SalesDataService.GetOrderDetailAsync(orderID, productID);
            if (existing == null)
            {
                return Content("<div class='alert alert-danger'>Không tìm thấy chi tiết đơn hàng.</div>", "text/html");
            }

            var data = new OrderDetail
            {
                OrderID = orderID,
                ProductID = productID,
                Quantity = quantity,
                SalePrice = existing.SalePrice
            };

            bool ok = await SalesDataService.UpdateOrderDetailAsync(data);
            if (ok)
            {
                string script = $"<script>if(window.parent){{ window.parent.$('#modal-container').modal('hide'); window.parent.location.href = '/Order/Detail/{orderID}'; }}else{{ window.location.href = '/Order/Detail/{orderID}'; }}</script>";
                return Content(script, "text/html");
            }

            return Content("<div class='alert alert-danger'>Không thể cập nhật chi tiết đơn hàng.</div>", "text/html");
        }

        public async Task<IActionResult> Accept(int id)
        {
            int employeeID = GetCurrentEmployeeID();
            if (employeeID <= 0) return RedirectToAction("Login", "Account");

            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return RedirectToAction("Index");

            SalesDataService.AcceptOrder(id, employeeID);

            if (order.OrderType?.ToLower() == "instore")
            {
                return RedirectToAction("Create", "Invoice", new { id });
            }

            return RedirectToAction("Shipping", new { id });
        }

        public IActionResult Finish(int id)
        {
            SalesDataService.FinishOrder(id);
            return RedirectToAction("Detail", new { id });
        }

        public IActionResult Cancel(int id)
        {
            SalesDataService.CancelOrder(id);
            return RedirectToAction("Detail", new { id });
        }

        public IActionResult Reject(int id)
        {
            SalesDataService.RejectOrder(id);
            return RedirectToAction("Detail", new { id });
        }

        public IActionResult Delete(int id)
        {
            SalesDataService.DeleteOrder(id);
            return RedirectToAction("Index");
        }

        private List<CartItem> GetCart() => ApplicationContext.GetSessionData<List<CartItem>>(SHOPPING_CART) ?? new List<CartItem>();

        private int GetCurrentEmployeeID()
        {
            var userData = User.GetUserData();
            if (userData != null && int.TryParse(userData.UserId, out int id)) return id;
            return 0;
        }

        // GET: Order/Shipping/1031
        public async Task<IActionResult> Shipping(int id)
        {
            var input = new PaginationSearchInput()
            {
                Page = 1,
                PageSize = 100,
                SearchValue = ""
            };
            var result = await PartnerDataService.ListShippersAsync(input);
            ViewBag.Shippers = result.DataItems;
            return PartialView(id);
        }

        // POST: Order/Shipping
        [HttpPost]
        public IActionResult Shipping(int id, int shipperID)
        {
            if (shipperID <= 0) return RedirectToAction("Detail", new { id });
            SalesDataService.ShipOrder(id, shipperID);
            return RedirectToAction("Detail", new { id });
        }
    }
}