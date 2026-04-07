using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using SV22T1020587.BusinessLayers;
using SV22T1020587.Models.Catalog;
using SV22T1020587.Models.Common;
using SV22T1020587.Models.Partner;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SV22T1020587.Admin.Controllers
{
    [Authorize(Roles = "Kho, Admin")]
    public class ProductController : Controller
    {
        private const int PAGESIZE = 20;
        private const string SEARCH_CONDITION = "ProductSearchCondition";

        // Gán hằng số để tránh gõ sai tên TempData
        private const string SUCCESS_MSG = "SuccessMessage";
        private const string ERROR_MSG = "ErrorMessage";

        public async Task<IActionResult> Index()
        {
            ViewBag.Title = "Quản lý Sản phẩm";
            ViewBag.Page = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_Page") ?? 1;
            ViewBag.SearchValue = HttpContext.Session.GetString($"{SEARCH_CONDITION}_Value") ?? "";
            ViewBag.CategoryID = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_CategoryID") ?? 0;
            ViewBag.SupplierID = HttpContext.Session.GetInt32($"{SEARCH_CONDITION}_SupplierID") ?? 0;

            string minPriceStr = HttpContext.Session.GetString($"{SEARCH_CONDITION}_MinPrice");
            ViewBag.MinPrice = string.IsNullOrEmpty(minPriceStr) ? "" : minPriceStr;

            string maxPriceStr = HttpContext.Session.GetString($"{SEARCH_CONDITION}_MaxPrice");
            ViewBag.MaxPrice = string.IsNullOrEmpty(maxPriceStr) ? "" : maxPriceStr;

            var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 0 });
            var supplierResult = await DictionaryDataService.ListOfSuppliers(new PaginationSearchInput { Page = 1, PageSize = 0 });

            ViewBag.Categories = categoryResult?.DataItems;
            ViewBag.Suppliers = supplierResult?.DataItems;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Search(int page = 1, string searchValue = "", int categoryID = 0, int supplierID = 0, string minPrice = "", string maxPrice = "")
        {
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_Page", page);
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_Value", searchValue ?? "");
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_CategoryID", categoryID);
            HttpContext.Session.SetInt32($"{SEARCH_CONDITION}_SupplierID", supplierID);
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_MinPrice", minPrice ?? "");
            HttpContext.Session.SetString($"{SEARCH_CONDITION}_MaxPrice", maxPrice ?? "");

            decimal min = 0, max = 0;
            if (!string.IsNullOrWhiteSpace(minPrice)) decimal.TryParse(minPrice.Replace(",", ""), out min);
            if (!string.IsNullOrWhiteSpace(maxPrice)) decimal.TryParse(maxPrice.Replace(",", ""), out max);

            var input = new ProductSearchInput()
            {
                Page = page,
                PageSize = PAGESIZE,
                SearchValue = searchValue ?? "",
                CategoryID = categoryID,
                SupplierID = supplierID,
                MinPrice = min,
                MaxPrice = max
            };

            var data = await CatalogDataService.ListProductsAsync(input);
            return PartialView("Search", data);
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.Title = "Bổ sung mặt hàng";
            var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 0 });
            var supplierResult = await DictionaryDataService.ListOfSuppliers(new PaginationSearchInput { Page = 1, PageSize = 0 });
            ViewBag.Categories = categoryResult?.DataItems;
            ViewBag.Suppliers = supplierResult?.DataItems;

            var product = new Product() { ProductID = 0, IsSelling = true };
            return View("Edit", product);
        }

        public async Task<IActionResult> Edit(int id = 0)
        {
            ViewBag.Title = "Cập nhật mặt hàng";
            var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 0 });
            var supplierResult = await DictionaryDataService.ListOfSuppliers(new PaginationSearchInput { Page = 1, PageSize = 0 });
            ViewBag.Categories = categoryResult?.DataItems;
            ViewBag.Suppliers = supplierResult?.DataItems;

            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");

            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Save(Product data, IFormFile? uploadPhoto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(data.ProductName))
                    ModelState.AddModelError(nameof(data.ProductName), "Tên mặt hàng không được để trống");

                if (!ModelState.IsValid)
                {
                    var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 0 });
                    var supplierResult = await DictionaryDataService.ListOfSuppliers(new PaginationSearchInput { Page = 1, PageSize = 0 });
                    ViewBag.Categories = categoryResult?.DataItems;
                    ViewBag.Suppliers = supplierResult?.DataItems;
                    return View("Edit", data);
                }

                if (uploadPhoto != null)
                {
                    string fileName = $"{DateTime.Now.Ticks}_{uploadPhoto.FileName}";
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }

                if (data.ProductID == 0)
                {
                    long id = await CatalogDataService.AddProductAsync(data);
                    if (id <= 0) throw new Exception("Tên mặt hàng bị trùng lặp");

                    TempData[SUCCESS_MSG] = "Bổ sung mặt hàng thành công";
                    return RedirectToAction("Edit", new { id = id });
                }
                else
                {
                    bool result = await CatalogDataService.UpdateProductAsync(data);
                    if (!result) throw new Exception("Cập nhật không thành công (có thể tên mặt hàng bị trùng)");

                    TempData[SUCCESS_MSG] = "Cập nhật mặt hàng thành công";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData[ERROR_MSG] = ex.Message;
                var categoryResult = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 0 });
                var supplierResult = await DictionaryDataService.ListOfSuppliers(new PaginationSearchInput { Page = 1, PageSize = 0 });
                ViewBag.Categories = categoryResult?.DataItems;
                ViewBag.Suppliers = supplierResult?.DataItems;
                return View("Edit", data);
            }
        }

        public async Task<IActionResult> Delete(int id = 0)
        {
            if (Request.Method == "POST")
            {
                await CatalogDataService.DeleteProductAsync(id);
                TempData[SUCCESS_MSG] = "Đã xóa mặt hàng thành công";
                return RedirectToAction("Index");
            }
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");
            return View(product);
        }

        // ==========================================
        // QUẢN LÝ THƯ VIỆN ẢNH
        // ==========================================
        public async Task<IActionResult> Photo(int id = 0, string method = "", long photoId = 0)
        {
            switch (method)
            {
                case "add":
                    return View("EditPhoto", new ProductPhoto() { ProductID = id });
                case "edit":
                    var photo = await CatalogDataService.GetPhotoAsync(photoId);
                    return photo == null ? RedirectToAction("Edit", new { id = id }) : View("EditPhoto", photo);
                case "delete":
                    await CatalogDataService.DeletePhotoAsync(photoId);
                    TempData[SUCCESS_MSG] = "Đã xóa ảnh thành công";
                    return RedirectToAction("Edit", new { id = id });
                default:
                    return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SavePhoto(ProductPhoto data, IFormFile? uploadPhoto)
        {
            try
            {
                if (uploadPhoto != null)
                {
                    string fileName = $"{DateTime.Now.Ticks}_{uploadPhoto.FileName}";
                    string filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "products", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create)) { await uploadPhoto.CopyToAsync(stream); }
                    data.Photo = fileName;
                }

                if (data.PhotoID == 0) await CatalogDataService.AddPhotoAsync(data);
                else await CatalogDataService.UpdatePhotoAsync(data);

                TempData[SUCCESS_MSG] = "Đã lưu ảnh mặt hàng";
                return RedirectToAction("Edit", new { id = data.ProductID });
            }
            catch (Exception ex)
            {
                TempData[ERROR_MSG] = "Lỗi khi lưu ảnh: " + ex.Message;
                return RedirectToAction("Edit", new { id = data.ProductID });
            }
        }

        // ==========================================
        // QUẢN LÝ THUỘC TÍNH
        // ==========================================
        public async Task<IActionResult> Attribute(int id = 0, string method = "", long attributeId = 0)
        {
            switch (method)
            {
                case "add":
                    return View("EditAttribute", new ProductAttribute() { ProductID = id });
                case "edit":
                    var attr = await CatalogDataService.GetAttributeAsync(attributeId);
                    return attr == null ? RedirectToAction("Edit", new { id = id }) : View("EditAttribute", attr);
                case "delete":
                    await CatalogDataService.DeleteAttributeAsync(attributeId);
                    TempData[SUCCESS_MSG] = "Đã xóa thuộc tính thành công";
                    return RedirectToAction("Edit", new { id = id });
                default:
                    return RedirectToAction("Index");
            }
        }

        [HttpPost]
        public async Task<IActionResult> SaveAttribute(ProductAttribute data)
        {
            try
            {
                if (data.AttributeID == 0) await CatalogDataService.AddAttributeAsync(data);
                else await CatalogDataService.UpdateAttributeAsync(data);

                TempData[SUCCESS_MSG] = "Đã lưu thuộc tính mặt hàng";
                return RedirectToAction("Edit", new { id = data.ProductID });
            }
            catch (Exception ex)
            {
                TempData[ERROR_MSG] = "Lỗi khi lưu thuộc tính: " + ex.Message;
                return RedirectToAction("Edit", new { id = data.ProductID });
            }
        }
    }
}