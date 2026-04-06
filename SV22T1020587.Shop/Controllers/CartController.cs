using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020587.BusinessLayers;
using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.DataLayers.SQLServer;
using SV22T1020587.Models.Sales;
using SV22T1020587.Shop.AppCodes;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SV22T1020587.Shop.Controllers
{
    [Authorize] // Require authentication to access all cart endpoints
    public class CartController : Controller
    {
        private const string SHOPPING_CART = "ShoppingCart";
        private readonly ICustomerCartRepository _cartRepo;

        public CartController()
        {
            _cartRepo = new CustomerCartRepository(Configuration.ConnectionString);
        }

        private List<CartItem> GetSessionCart() => ApplicationContext.GetSessionData<List<CartItem>>(SHOPPING_CART) ?? new List<CartItem>();
        private void SetSessionCart(List<CartItem> cart) => ApplicationContext.SetSessionData(SHOPPING_CART, cart);

        // --- HÀM TRỢ GIÚP DÙNG CHUNG ---
        private async Task<List<CartItem>> LoadCartForCurrentUserAsync()
        {
            var sessionCart = GetSessionCart();

            if (User.Identity?.IsAuthenticated ?? false)
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out int customerId))
                {
                    var dbCart = await _cartRepo.GetCartAsync(customerId) ?? new List<CartItem>();

                    if (sessionCart.Any())
                    {
                        foreach (var s in sessionCart)
                        {
                            var existing = dbCart.FirstOrDefault(x => x.ProductID == s.ProductID);
                            if (existing != null) existing.Quantity += s.Quantity;
                            else dbCart.Add(s);
                        }
                        await _cartRepo.SaveCartAsync(customerId, dbCart);
                        SetSessionCart(new List<CartItem>()); // Xóa session
                        return dbCart;
                    }
                    return dbCart;
                }
            }
            return sessionCart;
        }

        private async Task SaveCartForCurrentUserAsync(List<CartItem> cart)
        {
            if (User.Identity?.IsAuthenticated ?? false)
            {
                var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(idClaim, out int customerId))
                    await _cartRepo.SaveCartAsync(customerId, cart);
            }
            else
            {
                SetSessionCart(cart);
            }
        }

        // ================= ENDPOINTS =================

        // GET: /Cart
        public async Task<IActionResult> Index()
        {
            var cart = await LoadCartForCurrentUserAsync();

            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(idClaim, out int customerId))
            {
                var customer = await BusinessLayers.PartnerDataService.GetCustomerAsync(customerId);
                ViewBag.CheckoutCustomer = customer;
            }

            return View(cart);
        }

        // GET: /Cart/Count (Dùng để update Badge số lượng)
        [HttpGet]
        public async Task<IActionResult> Count()
        {
            var cart = await LoadCartForCurrentUserAsync();
            // Đếm tổng số lượng tất cả sản phẩm
            return Json(new { count = cart.Sum(i => i.Quantity) });
        }

        // POST: /Cart/Add (Dùng ở trang danh sách sản phẩm)
        [HttpPost]
        public async Task<IActionResult> Add(CartItem item)
        {
            if (item == null || item.Quantity <= 0) return BadRequest("Dữ liệu không hợp lệ");

            var cart = await LoadCartForCurrentUserAsync();
            var exists = cart.FirstOrDefault(x => x.ProductID == item.ProductID);

            if (exists == null) cart.Add(item);
            else exists.Quantity += item.Quantity;

            await SaveCartForCurrentUserAsync(cart);

            // Trả về tổng số lượng
            return Json(new { success = true, count = cart.Sum(i => i.Quantity) });
        }

        // POST: /Cart/Update (Gọi từ nút +/- hoặc ô input trong giỏ hàng)
        [HttpPost]
        public async Task<IActionResult> Update(int productID, int quantity)
        {
            var cart = await LoadCartForCurrentUserAsync();
            var item = cart.FirstOrDefault(x => x.ProductID == productID);

            if (item != null)
            {
                if (quantity <= 0) cart.Remove(item);
                else item.Quantity = quantity;

                await SaveCartForCurrentUserAsync(cart);
            }

            return Json(new
            {
                success = true,
                count = cart.Sum(i => i.Quantity) // Trả về tổng số lượng
            });
        }

        // POST: /Cart/Remove (Gọi từ nút Xóa 1 sản phẩm)
        [HttpPost]
        public async Task<IActionResult> Remove(int productID)
        {
            var cart = await LoadCartForCurrentUserAsync();
            cart.RemoveAll(x => x.ProductID == productID);

            await SaveCartForCurrentUserAsync(cart);

            // Trả về tổng số lượng
            return Json(new { success = true, count = cart.Sum(i => i.Quantity) });
        }

        // POST: /Cart/Clear (Gọi từ nút Xóa toàn bộ giỏ hàng)
        [HttpPost]
        public async Task<IActionResult> Clear()
        {
            // Truyền vào mảng rỗng để xóa sạch
            await SaveCartForCurrentUserAsync(new List<CartItem>());
            return Json(new { success = true });
        }
    }
}