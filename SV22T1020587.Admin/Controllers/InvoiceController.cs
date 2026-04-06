using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020587.BusinessLayers;
using SV22T1020587.Models.Sales;
using System.Threading.Tasks;

namespace SV22T1020587.Admin.Controllers
{
    [Authorize(Roles = "Sale, Admin")]
    public class InvoiceController : Controller
    {
        // GET: /Invoice/Create/{id}
        public async Task<IActionResult> Create(int id = 0)
        {
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null) return RedirectToAction("Index", "Order");

            var details = await SalesDataService.ListOrderDetailsAsync(id);
            ViewBag.OrderDetails = details;

            return View(order);
        }

        // POST: /Invoice/Issue
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Issue(int orderID)
        {
            // Gọi hàm đã bổ sung ở bước 1
            bool issued = SalesDataService.IssueOrderInvoice(orderID);

            if (issued)
                TempData["InfoMessage"] = $"Hóa đơn đã được lập cho đơn hàng #{orderID}.";
            else
                TempData["ErrorMessage"] = $"Không thể lập hóa đơn cho đơn hàng #{orderID}.";

            // Redirect back to admin order detail (action name is 'Detail')
            return RedirectToAction("Detail", "Order", new { id = orderID });
        }
    }
}