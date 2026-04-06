using Microsoft.AspNetCore.Mvc;
using SV22T1020587.Shop.Models;
using SV22T1020587.Models.Catalog;
using SV22T1020587.BusinessLayers;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace SV22T1020587.Shop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            // load featured products from database
            List<Product> featured = await CatalogDataService.GetFeaturedProductsAsync(6);
            return View(featured);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
