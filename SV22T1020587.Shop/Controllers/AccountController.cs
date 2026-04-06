using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020587.BusinessLayers;
using SV22T1020587.Models.DataDictionary;
using SV22T1020587.Models.Partner;
using SV22T1020587.Models.Sales;
using SV22T1020587.Models.Security;
using System;
using System.Security.Claims;

namespace SV22T1020587.Shop.Controllers
{
    public class AccountController : Controller
    {
        private const string SHOPPING_CART = "ShoppingCart";
        private const string AUTH_SCHEME = "CustomerAuth";
        private readonly ILogger<AccountController> _logger;

        public AccountController(ILogger<AccountController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// đăng ký tài khoản mới, chỉ cần thông tin cơ bản, không bắt buộc phải có địa chỉ, số điện thoại... 
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Register()
        {
            var provinces = await DictionaryDataService.ListProvincesAsync();
            ViewBag.Provinces = provinces ?? new List<Province>();
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(Customer model, string password)
        {
            ModelState.Remove(nameof(model.CustomerID));
            ModelState.Remove(nameof(model.IsLocked));

            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync() ?? new List<Province>();
                return View(model);
            }

            model.Email = model.Email?.Trim().ToLowerInvariant();

            bool ok = await PartnerDataService.IsValidCustomerEmailAsync(model.Email, 0);
            if (!ok)
            {
                ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng.");
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync() ?? new List<Province>();
                return View(model);
            }

            int newId = await PartnerDataService.AddCustomerAsync(model);
            if (newId <= 0)
            {
                ModelState.AddModelError("", "Không thể đăng ký tài khoản.");
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync() ?? new List<Province>();
                return View(model);
            }

            bool passSaved = await PartnerDataService.ChangeCustomerPasswordAsync(newId, password);
            if (!passSaved)
            {
                ModelState.AddModelError("", "Không thể lưu mật khẩu.");
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync() ?? new List<Province>();
                return View(model);
            }

            TempData["InfoMessage"] = "Đăng ký tài khoản thành công. Vui lòng đăng nhập.";
            return RedirectToAction("Login");
        }

        // GET: /Account/Login
        public IActionResult Login(string returnUrl = null)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool remember = false, string? returnUrl = null)
        {
            email = email?.Trim().ToLowerInvariant();

            try
            {
                _logger.LogInformation("Login attempt for {Email}", email ?? "<null>");

                var customer = await PartnerDataService.AuthorizeCustomerAsync(email, password);

                if (customer == null)
                {
                    ModelState.AddModelError("", "Thông tin đăng nhập không đúng. Vui lòng kiểm tra email và mật khẩu.");
                    _logger.LogWarning("Failed login for {Email}", email ?? "<null>");
                    return View();
                }

                if (customer.IsLocked == true)
                {
                    ModelState.AddModelError("", "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ quản trị viên!");
                    _logger.LogWarning("Locked account login attempt for {Email}", email);
                    return View();
                }

                var userAccount = new UserAccount
                {
                    UserId = customer.CustomerID.ToString(),
                    UserName = customer.Email ?? string.Empty,
                    DisplayName = !string.IsNullOrWhiteSpace(customer.CustomerName) ? customer.CustomerName : customer.ContactName ?? string.Empty,
                    Email = customer.Email ?? string.Empty,
                    Photo = ""
                };

                await SignInUserAsync(userAccount, remember);
                _logger.LogInformation("User {UserId} signed in. Persistent={Persistent}", userAccount.UserId, remember);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while authenticating {Email}", email ?? "<null>");
                ModelState.AddModelError("", "Lỗi khi xác thực. Vui lòng thử lại sau.");
                return View();
            }
        }

        // POST: /Account/Logout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            _logger.LogInformation("User {User} logging out", User?.Identity?.Name ?? "<anonymous>");
            await HttpContext.SignOutAsync(AUTH_SCHEME);
            HttpContext.Session.Remove(SHOPPING_CART);
            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/LogoutGet
        [HttpGet]
        public async Task<IActionResult> LogoutGet()
        {
            return await Logout();
        }

        private async Task SignInUserAsync(UserAccount user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId),
                new Claim(ClaimTypes.Name, user.DisplayName ?? user.UserName),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim(ClaimTypes.Role, "Customer")
            };

            var identity = new ClaimsIdentity(claims, AUTH_SCHEME);
            var principal = new ClaimsPrincipal(identity);

            var props = new AuthenticationProperties
            {
                IsPersistent = isPersistent,
            };

            if (isPersistent)
            {
                props.ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30);
            }

            await HttpContext.SignInAsync(AUTH_SCHEME, principal, props);
        }

        // GET: /Account/Profile
        public async Task<IActionResult> Profile()
        {
            // Kiểm tra an toàn tuyệt đối, tránh lỗi bool?
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return RedirectToAction("Login");

            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int customerId))
                return RedirectToAction("Login");

            var customer = await PartnerDataService.GetCustomerAsync(customerId);
            if (customer == null)
                return RedirectToAction("Index", "Home");

            var orders = await SalesDataService.ListOrdersForCustomerAsync(customerId);
            ViewBag.Orders = orders ?? new List<OrderViewInfo>();

            var provinces = await DictionaryDataService.ListProvincesAsync();
            ViewBag.Provinces = provinces ?? new List<Province>();

            return View("~/Views/Profile/Index.cshtml", customer);
        }

        // POST: /Account/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(Customer model)
        {
            // Kiểm tra an toàn tuyệt đối, tránh lỗi bool?
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return RedirectToAction("Login");

            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int customerId))
                return RedirectToAction("Login");

            model.CustomerID = model.CustomerID == 0 ? customerId : model.CustomerID;
            if (model.CustomerID != customerId)
            {
                TempData["ErrorMessage"] = "Không được phép cập nhật tài khoản khác.";
                return RedirectToAction("Profile");
            }

            model.Email = model.Email?.Trim().ToLowerInvariant();

            if (!ModelState.IsValid)
            {
                ViewBag.Provinces = await DictionaryDataService.ListProvincesAsync() ?? new List<Province>();
                return View("~/Views/Profile/Index.cshtml", model);
            }

            bool okEmail = await PartnerDataService.IsValidCustomerEmailAsync(model.Email, model.CustomerID);
            if (!okEmail)
            {
                ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng.");
                return View("~/Views/Profile/Index.cshtml", model);
            }

            bool ok = await PartnerDataService.UpdateCustomerAsync(model);
            if (ok)
            {
                TempData["InfoMessage"] = "Cập nhật thông tin thành công.";

                var updatedCustomer = await PartnerDataService.GetCustomerAsync(customerId);
                if (updatedCustomer != null)
                {
                    var user = new UserAccount
                    {
                        UserId = updatedCustomer.CustomerID.ToString(),
                        UserName = updatedCustomer.Email ?? string.Empty,
                        DisplayName = !string.IsNullOrWhiteSpace(updatedCustomer.CustomerName) ? updatedCustomer.CustomerName : updatedCustomer.ContactName ?? string.Empty,
                        Email = updatedCustomer.Email ?? string.Empty,
                        Photo = ""
                    };

                    await SignInUserAsync(user, false);
                }
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể cập nhật thông tin.";
            }

            return RedirectToAction("Profile");
        }

        // GET: /Account/ChangePassword
        public IActionResult ChangePassword() => View("~/Views/Profile/ChangePassword.cshtml");

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
        {
            // Kiểm tra an toàn tuyệt đối, tránh lỗi bool?
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return RedirectToAction("Login");

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới cần tối thiểu 6 ký tự.";
                return Redirect("/Profile#changePassword");
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới và xác nhận không khớp.";
                return Redirect("/Profile#changePassword");
            }

            var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            email = email.Trim().ToLowerInvariant();

            var authorized = await PartnerDataService.AuthorizeCustomerAsync(email, currentPassword) != null;
            if (!authorized)
            {
                TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng.";
                return Redirect("/Profile#changePassword");
            }

            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out int customerId))
            {
                TempData["ErrorMessage"] = "Không xác định được tài khoản.";
                return Redirect("/Profile#changePassword");
            }

            bool changed = await PartnerDataService.ChangeCustomerPasswordAsync(customerId, newPassword);
            if (changed) TempData["InfoMessage"] = "Đổi mật khẩu thành công.";
            else TempData["ErrorMessage"] = "Không thể đổi mật khẩu.";

            return Redirect("/Profile#changePassword");
        }

        // Debug endpoint for authorizing credentials
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> DebugAuthorize([FromForm] string email, [FromForm] string password)
        {
            email = email?.Trim().ToLowerInvariant();

            try
            {
                var customer = await PartnerDataService.AuthorizeCustomerAsync(email, password);
                if (customer == null)
                {
                    _logger.LogInformation("DebugAuthorize: credentials rejected for {Email}", email ?? "<null>");
                    return BadRequest(new { ok = false, message = "unauthorized" });
                }

                if (customer.IsLocked == true)
                {
                    _logger.LogInformation("DebugAuthorize: account locked for {Email}", email);
                    return BadRequest(new { ok = false, message = "account_locked" });
                }

                _logger.LogInformation("DebugAuthorize: authorized user {UserId} for {Email}", customer.CustomerID, email);

                string displayName = !string.IsNullOrWhiteSpace(customer.CustomerName) ? customer.CustomerName : customer.ContactName ?? string.Empty;

                return Ok(new
                {
                    ok = true,
                    userId = customer.CustomerID.ToString(),
                    email = customer.Email,
                    displayName = displayName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DebugAuthorize error for {Email}", email ?? "<null>");
                return StatusCode(500, new { ok = false, message = "error", detail = ex.Message });
            }
        }
    }
}