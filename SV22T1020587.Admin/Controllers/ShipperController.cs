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
    public class ShipperController : Controller
    {
        private const int PAGE_SIZE = 10;
        private const string SEARCH_CONDITION = "ShipperSearchCondition";

        public IActionResult Index()
        {
            ViewBag.Title = "Quản lý Người giao hàng";

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

            var data = await PartnerDataService.ListShippersAsync(condition);
            return PartialView("Search", data);
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung người giao hàng";
            var model = new Shipper() { ShipperID = 0 };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id = 0)
        {
            ViewBag.Title = "Cập nhật thông tin người giao hàng";
            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Shipper data)
        {
            // Kiểm tra dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(data.ShipperName))
                ModelState.AddModelError(nameof(data.ShipperName), "Tên người giao hàng không được để trống");

            if (string.IsNullOrWhiteSpace(data.Phone))
                ModelState.AddModelError(nameof(data.Phone), "Số điện thoại không được để trống");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.ShipperID == 0 ? "Bổ sung người giao hàng" : "Cập nhật thông tin người giao hàng";
                return View("Edit", data);
            }

            try
            {
                if (data.ShipperID == 0)
                {
                    int id = await PartnerDataService.AddShipperAsync(data);
                    TempData["Message"] = "Đã thêm mới người giao hàng thành công!";
                    return RedirectToAction("Edit", new { id = id });
                }
                else
                {
                    await PartnerDataService.UpdateShipperAsync(data);
                    TempData["Message"] = "Đã cập nhật thông tin người giao hàng thành công!";
                    return RedirectToAction("Edit", new { id = data.ShipperID });
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", "Có lỗi xảy ra: " + ex.Message);
                return View("Edit", data);
            }
        }

        public async Task<IActionResult> Delete(int id = 0)
        {
            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null) return RedirectToAction("Index");

            if (Request.Method == "POST")
            {
                bool result = await PartnerDataService.DeleteShipperAsync(id);
                if (result)
                {
                    TempData["Message"] = "Đã xóa người giao hàng thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = "Không thể xóa người giao hàng này vì đang có dữ liệu liên quan.";
                    return RedirectToAction("Delete", new { id = id });
                }
            }

            ViewBag.IsUsed = await PartnerDataService.IsUsedShipperAsync(id);
            ViewBag.Title = "Xóa người giao hàng";
            return View(model);
        }
    }
}