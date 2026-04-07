using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SV22T1020587.BusinessLayers;
using SV22T1020587.Models.Common;
using SV22T1020587.Models.Partner;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SV22T1020587.Admin.Controllers
{
    [Authorize(Roles = "Staff, Admin")]
    public class CustomerController : Controller
    {
        private const int PAGESIZE = 10;
        private const string SEARCH_CONDITION = "CustomerSearchCondition";

        // ==========================================
        // 1. TÌM KIẾM VÀ HIỂN THỊ
        // ==========================================
        public IActionResult Index()
        {
            ViewBag.Title = "Quản lý Khách hàng";
            var input = new PaginationSearchInput()
            {
                Page = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_Page") ?? 1,
                PageSize = PAGESIZE,
                SearchValue = HttpContext.Session.GetString($"{SEARCH_CONDITION}_Value") ?? ""
            };
            return View(input);
        }

        [HttpPost]
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_Page", input.Page);
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_Value", input.SearchValue ?? "");

            var result = await PartnerDataService.ListCustomersAsync(input);
            return PartialView("Search", result);
        }

        // ==========================================
        // 2. THÊM VÀ SỬA KHÁCH HÀNG
        // ==========================================
        public async Task<IActionResult> Create()
        {
            ViewBag.Title = "Bổ sung khách hàng";
            ViewBag.Provinces = await SelectListHelper.Provinces();

            var model = new Customer()
            {
                CustomerID = 0,
                IsLocked = false
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id = 0)
        {
            ViewBag.Title = "Cập nhật thông tin khách hàng";
            ViewBag.Provinces = await SelectListHelper.Provinces();

            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Save(Customer data)
        {
            if (!string.IsNullOrWhiteSpace(data.Email))
            {
                bool isEmailValid = await PartnerDataService.IsValidCustomerEmailAsync(data.Email, data.CustomerID);
                if (!isEmailValid)
                {
                    ModelState.AddModelError(nameof(data.Email), "Địa chỉ Email này đã được sử dụng. Vui lòng nhập Email khác.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng" : "Cập nhật thông tin khách hàng";
                ViewBag.Provinces = await SelectListHelper.Provinces();
                return View("Edit", data);
            }

            if (data.CustomerID == 0)
            {
                await PartnerDataService.AddCustomerAsync(data);
                HttpContext.Session.SetString("SuccessMessage", "Bổ sung khách hàng thành công!");
            }
            else
            {
                await PartnerDataService.UpdateCustomerAsync(data);
                HttpContext.Session.SetString("SuccessMessage", "Cập nhật thông tin khách hàng thành công!");
            }
            return RedirectToAction("Index");
        }

        // ==========================================
        // 3. XÓA KHÁCH HÀNG
        // ==========================================
        public async Task<IActionResult> Delete(int id = 0)
        {
            ViewBag.Title = "Xóa khách hàng";

            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            bool result = await PartnerDataService.DeleteCustomerAsync(id);

            if (!result)
            {
                ModelState.AddModelError("", "Không thể xóa khách hàng này vì đã phát sinh dữ liệu giao dịch (đơn hàng).");
                var model = await PartnerDataService.GetCustomerAsync(id);
                return View("Delete", model);
            }

            HttpContext.Session.SetString("SuccessMessage", "Xóa khách hàng thành công!");
            return RedirectToAction("Index");
        }

        // ==========================================
        // 4. ĐỔI MẬT KHẨU
        // ==========================================
        public async Task<IActionResult> ChangePassword(int id = 0)
        {
            ViewBag.Title = "Mật khẩu khách hàng";

            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SavePassword(int customerID, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError("newPassword", "Vui lòng nhập mật khẩu mới.");
            }
            else if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp.");
            }

            if (!ModelState.IsValid)
            {
                var model = await PartnerDataService.GetCustomerAsync(customerID);
                return View("ChangePassword", model);
            }

            bool result = await PartnerDataService.ChangeCustomerPasswordAsync(customerID, newPassword);

            if (!result)
            {
                ModelState.AddModelError("", "Đổi mật khẩu thất bại. Vui lòng thử lại sau.");
                var model = await PartnerDataService.GetCustomerAsync(customerID);
                return View("ChangePassword", model);
            }

            HttpContext.Session.SetString("SuccessMessage", "Đổi mật khẩu thành công!");
            return RedirectToAction("Index");
        }
    }
}