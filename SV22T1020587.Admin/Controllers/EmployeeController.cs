using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SV22T1020587.BusinessLayers;
using SV22T1020587.Models.Common;
using SV22T1020587.Models.HR;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace SV22T1020587.Admin.Controllers
{
    // CẬP NHẬT: Cho phép cả Staff và Admin quản lý nhân viên
    [Authorize(Roles = "Staff, Admin")]
    public class EmployeeController : Controller
    {
        private const int PAGE_SIZE = 10;
        private const string SEARCH_CONDITION = "EmployeeSearchCondition";

        public IActionResult Index()
        {
            ViewBag.Title = "Quản lý Nhân viên";

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

            var data = await HRDataService.ListEmployeesAsync(input);
            return PartialView(data);
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung Nhân viên";
            var model = new Employee()
            {
                EmployeeID = 0,
                BirthDate = new DateTime(1990, 1, 1),
                IsWorking = true,
                Photo = "nophoto.png"
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id = 0)
        {
            ViewBag.Title = "Cập nhật Nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(Employee data, string birthDateInput, IFormFile? uploadPhoto)
        {
            // 1. Xử lý ngày sinh
            if (!string.IsNullOrWhiteSpace(birthDateInput))
            {
                if (DateTime.TryParseExact(birthDateInput, "d/M/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                    data.BirthDate = parsedDate;
                else
                    ModelState.AddModelError(nameof(data.BirthDate), "Ngày sinh không đúng định dạng d/m/yyyy");
            }

            // 2. Kiểm tra dữ liệu bắt buộc
            if (string.IsNullOrWhiteSpace(data.FullName))
                ModelState.AddModelError(nameof(data.FullName), "Họ tên không được để trống");
            if (string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Email không được để trống");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung Nhân viên" : "Cập nhật Nhân viên";
                return View("Edit", data);
            }

            // 3. Xử lý upload ảnh
            if (uploadPhoto != null)
            {
                string fileName = $"{DateTime.Now.Ticks}_{uploadPhoto.FileName}";
                string folder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "employees");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                string filePath = Path.Combine(folder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadPhoto.CopyToAsync(stream);
                }
                data.Photo = fileName;
            }

            if (string.IsNullOrEmpty(data.Photo))
                data.Photo = "nophoto.png";

            // 4. Lưu CSDL
            if (data.EmployeeID == 0)
                await HRDataService.AddEmployeeAsync(data);
            else
                await HRDataService.UpdateEmployeeAsync(data);

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id = 0)
        {
            ViewBag.Title = "Xóa Nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            bool result = await HRDataService.DeleteEmployeeAsync(id);
            if (!result)
            {
                TempData["Error"] = "Không thể xóa nhân viên này vì có dữ liệu liên quan.";
                return RedirectToAction("Delete", new { id = id });
            }
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            ViewBag.Title = "Đổi mật khẩu nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePassword(int employeeID, string newPassword, string confirmPassword)
        {
            var employee = await HRDataService.GetEmployeeAsync(employeeID);
            if (employee == null) return RedirectToAction("Index");

            if (string.IsNullOrWhiteSpace(newPassword))
                ModelState.AddModelError("newPassword", "Vui lòng nhập mật khẩu mới");
            else if (newPassword != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");

            if (!ModelState.IsValid)
            {
                ViewBag.Title = "Đổi mật khẩu nhân viên";
                return View("ChangePassword", employee);
            }

            // Sử dụng Email làm username để đổi mật khẩu
            await SecurityDataService.ChangeEmployeePasswordAsync(employee.Email, newPassword);
            TempData["Message"] = $"Đã đổi mật khẩu cho nhân viên {employee.FullName}";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> ChangeRole(int id = 0)
        {
            ViewBag.Title = "Phân quyền nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null) return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRole(int employeeID, string[] roles)
        {
            // Chuyển mảng Roles thành chuỗi cách nhau bởi dấu phẩy
            string roleNames = (roles != null && roles.Length > 0) ? string.Join(",", roles) : "";

            var employee = await HRDataService.GetEmployeeAsync(employeeID);
            if (employee != null)
            {
                employee.RoleNames = roleNames;
                await HRDataService.UpdateEmployeeAsync(employee);
                TempData["Message"] = $"Cập nhật quyền cho nhân viên {employee.FullName} thành công.";
            }
            return RedirectToAction("Index");
        }
    }
}