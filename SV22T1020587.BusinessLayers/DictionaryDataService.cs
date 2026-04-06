using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.DataLayers.SQLServer;
using SV22T1020587.Models.Catalog;
using SV22T1020587.Models.Common;
using SV22T1020587.Models.DataDictionary;
using SV22T1020587.Models.HR;
using SV22T1020587.Models.Partner;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SV22T1020587.BusinessLayers
{
    /// <summary>
    /// Đã sửa tên thành DictionaryDataService (Chuẩn chính tả)
    /// </summary>
    public static class DictionaryDataService
    {
        private static readonly IDataDictionaryRepository<Province> provinceDB;
        private static readonly IGenericRepository<Category> categoryDB;
        private static readonly IGenericRepository<Supplier> supplierDB;
        private static readonly IGenericRepository<Shipper> shipperDB;
        private static readonly ICustomerRepository customerDB;
        private static readonly IEmployeeRepository employeeDB;

        static DictionaryDataService()
        {
            string connectionString = Configuration.ConnectionString;
            provinceDB = new ProvinceRepository(connectionString);
            categoryDB = new CategoryRepository(connectionString);
            supplierDB = new SupplierRepository(connectionString);
            shipperDB = new ShipperRepository(connectionString);
            customerDB = new CustomerRepository(connectionString);
            employeeDB = new EmployeeRepository(connectionString);
        }

        #region --- Xử lý cho Tỉnh/Thành (Province) ---
        // Sửa tên hàm cho khớp với SelectListHelper
        public static async Task<List<Province>> ListProvincesAsync() => await provinceDB.ListAsync();
        #endregion

        #region --- Xử lý cho Loại hàng (Category) ---
        public static async Task<PagedResult<Category>> ListCategoriesAsync(PaginationSearchInput input) => await categoryDB.ListAsync(input);
        public static async Task<Category?> GetCategoryAsync(int id) => await categoryDB.GetAsync(id);
        // ... (Bạn có thể đổi tên các hàm bên dưới thêm đuôi Async cho đồng bộ nhé)
        #endregion

        #region --- Xử lý cho Nhà cung cấp (Supplier) ---
        public static async Task<PagedResult<Supplier>> ListOfSuppliers(PaginationSearchInput input) => await supplierDB.ListAsync(input);
        public static async Task<Supplier?> GetSupplier(int id) => await supplierDB.GetAsync(id);
        public static async Task<int> AddSupplier(Supplier data) => await supplierDB.AddAsync(data);
        public static async Task<bool> UpdateSupplier(Supplier data) => await supplierDB.UpdateAsync(data);
        public static async Task<bool> DeleteSupplier(int id) => await supplierDB.DeleteAsync(id);
        public static async Task<bool> InUsedSupplier(int id) => await supplierDB.IsUsedAsync(id);
        #endregion

        #region --- Xử lý cho Người giao hàng (Shipper) ---
        public static async Task<PagedResult<Shipper>> ListOfShippers(PaginationSearchInput input) => await shipperDB.ListAsync(input);
        public static async Task<Shipper?> GetShipper(int id) => await shipperDB.GetAsync(id);
        public static async Task<int> AddShipper(Shipper data) => await shipperDB.AddAsync(data);
        public static async Task<bool> UpdateShipper(Shipper data) => await shipperDB.UpdateAsync(data);
        public static async Task<bool> DeleteShipper(int id) => await shipperDB.DeleteAsync(id);
        public static async Task<bool> InUsedShipper(int id) => await shipperDB.IsUsedAsync(id);
        #endregion

        #region --- Xử lý cho Khách hàng (Customer) ---
        public static async Task<PagedResult<Customer>> ListOfCustomers(PaginationSearchInput input) => await customerDB.ListAsync(input);
        public static async Task<Customer?> GetCustomer(int id) => await customerDB.GetAsync(id);
        public static async Task<int> AddCustomer(Customer data) => await customerDB.AddAsync(data);
        public static async Task<bool> UpdateCustomer(Customer data) => await customerDB.UpdateAsync(data);
        public static async Task<bool> DeleteCustomer(int id) => await customerDB.DeleteAsync(id);
        public static async Task<bool> InUsedCustomer(int id) => await customerDB.IsUsedAsync(id);
        public static async Task<bool> IsValidCustomerEmail(string email, int id = 0) => await customerDB.IsValidEmailAsync(email, id);
        #endregion

        #region --- Xử lý cho Nhân viên (Employee) ---
        public static async Task<PagedResult<Employee>> ListOfEmployees(PaginationSearchInput input) => await employeeDB.ListAsync(input);
        public static async Task<Employee?> GetEmployee(int id) => await employeeDB.GetAsync(id);
        public static async Task<int> AddEmployee(Employee data) => await employeeDB.AddAsync(data);
        public static async Task<bool> UpdateEmployee(Employee data) => await employeeDB.UpdateAsync(data);
        public static async Task<bool> DeleteEmployee(int id) => await employeeDB.DeleteAsync(id);
        public static async Task<bool> InUsedEmployee(int id) => await employeeDB.IsUsedAsync(id);
        public static async Task<bool> ValidateEmployeeEmail(string email, int id = 0) => await employeeDB.ValidateEmailAsync(email, id);
        #endregion
    }
}