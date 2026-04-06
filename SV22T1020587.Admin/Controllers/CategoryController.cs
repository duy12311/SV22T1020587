using Microsoft.AspNetCore.Authorization; // 1. Bắt buộc thêm thư viện này
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SV22T1020587.BusinessLayers;
using SV22T1020587.Models.Catalog;
using SV22T1020587.Models.Common;
using System.Threading.Tasks;

namespace SV22T1020587.Admin.Controllers
{
    // 2. CHẶN TRUY CẬP: Loại hàng đi liền với Sản phẩm nên cấp quyền cho "Kho" và "Admin"
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
        public async Task<IActionResult> Save(Category data)
        {
            if (string.IsNullOrWhiteSpace(data.CategoryName))
                ModelState.AddModelError(nameof(data.CategoryName), "Tên loại hàng không được để trống");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.CategoryID == 0 ? "Bổ sung Loại hàng" : "Cập nhật Loại hàng";
                return View("Edit", data);
            }

            if (data.CategoryID == 0)
                await CatalogDataService.AddCategoryAsync(data);
            else
                await CatalogDataService.UpdateCategoryAsync(data);

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id = 0)
        {
            ViewBag.Title = "Xóa Loại hàng";
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            bool result = await CatalogDataService.DeleteCategoryAsync(id);
            if (!result)
            {
                ModelState.AddModelError("", "Không thể xóa loại hàng này vì đang có sản phẩm liên quan (ràng buộc dữ liệu)!");
                var model = await CatalogDataService.GetCategoryAsync(id);
                return View("Delete", model);
            }
            return RedirectToAction("Index");
        }
    }
}