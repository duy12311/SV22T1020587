using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SV22T1020587.BusinessLayers;
using SV22T1020587.Models.Catalog;
using SV22T1020587.Models.Common;
using System;
using System.Threading.Tasks;

namespace SV22T1020587.Admin.Controllers
{
    [Authorize(Roles = "Kho, Admin")]
    public class CategoryController : Controller
    {
        private const int PAGE_SIZE = 10;
        private const string SEARCH_CONDITION = "CategorySearchCondition";

        public IActionResult Index()
        {
            ViewBag.Title = "Quản lý Loại hàng";

            var input = new PaginationSearchInput()
            {
                Page = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_Page") ?? 1,
                PageSize = PAGE_SIZE,
                SearchValue = HttpContext.Session.GetString($"{SEARCH_CONDITION}_Value") ?? ""
            };

            return View(input);
        }

        [HttpPost]
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_Page", input.Page);
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_Value", input.SearchValue ?? "");

            var data = await CatalogDataService.ListCategoriesAsync(input);

            // Bắt buộc dùng PartialView để không bị lồng giao diện Menu/Header
            return PartialView(data);
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung Loại hàng";
            var model = new Category() { CategoryID = 0 };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id = 0)
        {
            ViewBag.Title = "Cập nhật Loại hàng";
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Bảo mật Form
        public async Task<IActionResult> Save(Category data)
        {
            if (string.IsNullOrWhiteSpace(data.CategoryName))
                ModelState.AddModelError(nameof(data.CategoryName), "Tên loại hàng không được để trống");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.CategoryID == 0 ? "Bổ sung Loại hàng" : "Cập nhật Loại hàng";
                return View("Edit", data);
            }

            try
            {
                if (data.CategoryID == 0)
                {
                    await CatalogDataService.AddCategoryAsync(data);
                    TempData["Message"] = "Đã bổ sung loại hàng mới thành công!";
                }
                else
                {
                    await CatalogDataService.UpdateCategoryAsync(data);
                    TempData["Message"] = "Cập nhật thông tin loại hàng thành công!";
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Bắt lỗi CSDL (ví dụ nếu có unique key trên Tên loại hàng)
                if (ex.GetBaseException().Message.Contains("UNIQUE KEY"))
                {
                    ModelState.AddModelError(nameof(data.CategoryName), "Tên loại hàng này đã tồn tại. Vui lòng nhập tên khác!");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi hệ thống khi lưu dữ liệu. Vui lòng thử lại!");
                }

                ViewBag.Title = data.CategoryID == 0 ? "Bổ sung Loại hàng" : "Cập nhật Loại hàng";
                return View("Edit", data);
            }
        }

        public async Task<IActionResult> Delete(int id = 0)
        {
            ViewBag.Title = "Xóa Loại hàng";
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken] // Bảo mật Form
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            bool result = await CatalogDataService.DeleteCategoryAsync(id);
            if (!result)
            {
                // Nếu không xóa được do có ràng buộc dữ liệu
                TempData["Error"] = "Không thể xóa loại hàng này vì đang có sản phẩm liên quan (ràng buộc dữ liệu)!";
                return RedirectToAction("Delete", new { id = id });
            }

            TempData["Message"] = "Đã xóa loại hàng thành công.";
            return RedirectToAction("Index");
        }
    }
}