using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using SV22T1020587.BusinessLayers;
using SV22T1020587.Models.Security;

namespace SV22T1020587.Admin.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string returnUrl = "")
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string returnUrl = "")
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ Email và Mật khẩu!");
                return View();
            }

            var userAccount = await SecurityDataService.AuthorizeEmployeeAsync(email, password);

            if (userAccount != null)
            {
                // 1. Khởi tạo danh sách Claims cơ bản (ĐÃ FIX LỖI TẠI ĐÂY)
                var claims = new List<Claim>()
                {
                    // Thêm claim "UserId" để hàm User.GetUserData() lấy đúng ID số của nhân viên
                    new Claim("UserId", userAccount.UserId), 
                    
                    // Gán ClaimTypes.Name là Email để User.Identity.Name lấy đúng tài khoản đổi mật khẩu
                    new Claim(ClaimTypes.Name, userAccount.Email ?? ""),

                    new Claim(ClaimTypes.GivenName, userAccount.DisplayName),
                    new Claim(ClaimTypes.Email, userAccount.Email ?? ""),
                    new Claim("Photo", userAccount.Photo ?? "nophoto.png")
                };

                // 2. PHÂN QUYỀN ĐỘNG: Đảm bảo quyền được nạp chính xác vào Identity
                if (!string.IsNullOrEmpty(userAccount.RoleNames))
                {
                    // Tách chuỗi quyền theo dấu phẩy
                    var roles = userAccount.RoleNames.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var role in roles)
                    {
                        // Trim khoảng trắng thừa để tránh lỗi so sánh chuỗi
                        claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
                    }
                }

                var identity = new ClaimsIdentity(claims, "AdminWebAuth");
                var principal = new ClaimsPrincipal(identity);

                // 3. Đăng nhập và tạo Cookie
                await HttpContext.SignInAsync("AdminWebAuth", principal);

                // 4. Điều hướng sau khi đăng nhập thành công
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                // --- TỰ ĐỘNG ĐIỀU HƯỚNG DỰA TRÊN QUYỀN ---
                string userRoles = userAccount.RoleNames ?? "";

                // Ưu tiên 1: Admin cho về Trang chủ (Dashboard tổng)
                if (userRoles.Contains("Admin"))
                {
                    return RedirectToAction("Index", "Home");
                }

                // Ưu tiên 2: Quyền Sale -> Điều hướng sang Order
                if (userRoles.Contains("Sale"))
                {
                    return RedirectToAction("Index", "Order");
                }

                // Ưu tiên 3: Quyền Kho -> Điều hướng sang Product
                if (userRoles.Contains("Kho"))
                {
                    return RedirectToAction("Index", "Product");
                }

                // Ưu tiên 4: Quyền Staff -> Điều hướng sang Customer
                if (userRoles.Contains("Staff"))
                {
                    return RedirectToAction("Index", "Customer");
                }

                // Mặc định an toàn
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("Error", "Tài khoản hoặc mật khẩu không chính xác!");
            return View();
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("AdminWebAuth");
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            ViewBag.Title = "Đổi mật khẩu";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ các trường mật khẩu.");
                return View();
            }

            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp.");
                return View();
            }

            string userName = User.Identity?.Name ?? "";

            // Xác thực mật khẩu cũ trước khi cho phép đổi
            var checkOldPass = await SecurityDataService.AuthorizeEmployeeAsync(userName, oldPassword);
            if (checkOldPass == null)
            {
                ModelState.AddModelError("oldPassword", "Mật khẩu cũ không chính xác.");
                return View();
            }

            bool result = await SecurityDataService.ChangeEmployeePasswordAsync(userName, newPassword);

            if (result)
            {
                TempData["Message"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError("Error", "Đổi mật khẩu thất bại. Vui lòng thử lại sau.");
            return View();
        }

        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            ViewBag.Title = "Từ chối truy cập";
            return View();
        }
    }
}