using Microsoft.AspNetCore.Mvc;
using SV22T1020587.BusinessLayers;
using SV22T1020587.Models.Catalog;
using SV22T1020587.Models.Common;

namespace SV22T1020587.Shop.Controllers
{
    public class ProductController : Controller
    {
        private const int PAGE_SIZE = 12;

        // Index accepts search input via query string and returns paged result to the view
        public async Task<IActionResult> Index(ProductSearchInput? input)
        {
            var categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 0 });
            ViewBag.Categories = categories.DataItems;

            var searchInput = input ?? new ProductSearchInput
            {
                Page = 1,
                PageSize = PAGE_SIZE,
                CategoryID = 0,
                SupplierID = 0,
                MinPrice = 0,
                MaxPrice = 0,
                SearchValue = ""
            };

            searchInput.PageSize = searchInput.PageSize == 0 ? PAGE_SIZE : searchInput.PageSize;

            var result = await CatalogDataService.ListProductsAsync(searchInput);
            ViewBag.RowCount = result.RowCount;
            ViewBag.CurrentPage = searchInput.Page;
            ViewBag.SearchInput = searchInput; // pass back for view to prefill form and build paging links

            return View(result);
        }

        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            input.PageSize = input.PageSize == 0 ? PAGE_SIZE : input.PageSize;
            var result = await CatalogDataService.ListProductsAsync(input);
            ViewBag.RowCount = result.RowCount;
            ViewBag.CurrentPage = input.Page;
            ViewBag.SearchInput = input;
            return View("Index", result);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null) return RedirectToAction("Index");
            ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);
            return View(product);
        }
    }
}