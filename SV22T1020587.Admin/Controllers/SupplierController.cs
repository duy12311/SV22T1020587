using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SV22T1020587.BusinessLayers;
using SV22T1020587.Models.Common;
using SV22T1020587.Models.Partner;
using System;
using System.Threading.Tasks;

namespace SV22T1020587.Admin.Controllers
{
    [Authorize(Roles = "Staff, Admin")]
    public class SupplierController : Controller
    {
        private const int PAGE_SIZE = 10;
        private const string SEARCH_CONDITION = "SupplierSearchCondition";

        public IActionResult Index()
        {
            ViewBag.Title = "Quản lý Nhà cung cấp";

            ViewBag.Page = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_Page") ?? 1;
            ViewBag.SearchValue = HttpContext.Session.GetString($"{SEARCH_CONDITION}_Value") ?? "";

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Search(PaginationSearchInput condition)
        {
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_Page", condition.Page);
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_Value", condition.SearchValue ?? "");

            condition.PageSize = PAGE_SIZE;

            var data = await PartnerDataService.ListSuppliersAsync(condition);
            return PartialView("Search", data);
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhà cung cấp";
            var model = new Supplier() { SupplierID = 0 };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id = 0)
        {
            ViewBag.Title = "Cập nhật thông tin nhà cung cấp";
            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Save(Supplier data)
        {
            if (string.IsNullOrWhiteSpace(data.SupplierName))
                ModelState.AddModelError(nameof(data.SupplierName), "Vui lòng nhập tên nhà cung cấp");

            if (string.IsNullOrWhiteSpace(data.ContactName))
                ModelState.AddModelError(nameof(data.ContactName), "Vui lòng nhập tên giao dịch");

            if (string.IsNullOrWhiteSpace(data.Phone))
                ModelState.AddModelError(nameof(data.Phone), "Vui lòng nhập số điện thoại");

            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email");

            if (string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh/thành");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.SupplierID == 0 ? "Bổ sung nhà cung cấp" : "Cập nhật thông tin nhà cung cấp";
                return View("Edit", data);
            }

            try
            {
                if (data.SupplierID == 0)
                {
                    await PartnerDataService.AddSupplierAsync(data);
                    TempData["SuccessMessage"] = "Bổ sung nhà cung cấp thành công!";
                }
                else
                {
                    await PartnerDataService.UpdateSupplierAsync(data);
                    TempData["SuccessMessage"] = "Cập nhật thông tin nhà cung cấp thành công!";
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
                return View("Edit", data);
            }
        }

        public async Task<IActionResult> Delete(int id = 0)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteSupplierAsync(id);
                TempData["SuccessMessage"] = "Xóa nhà cung cấp thành công!";
                return RedirectToAction("Index");
            }

            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null) return RedirectToAction("Index");

            ViewBag.IsUsed = await PartnerDataService.IsUsedSupplierAsync(id);
            ViewBag.Title = "Xóa nhà cung cấp";
            return View(model);
        }
    }
}