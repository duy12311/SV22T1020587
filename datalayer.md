# IGeneicRepository:
Cung c?p các ch?c n?ng ?? làm vi?c v?i các b?ng trong DB
## - SupplierRepository
## - ShipperRepository
## - CategoryRepository


# IProductRepository:
## ProductRepository

# ICustomerRepository:
## - CustomerRepository

# IEmployeeRepository:
## - EmployeeRepository

# IOrderRepository:
## - OrderRepository

# IDataDictionaryRepository:
## - ProvinceRepository

# IUserAccountRepository:
## - EmployeeAccountRepository
## - CustomerAccountRepository


cho 1 csdl ???c cài ??t nh? sau:
-- 1. B?ng Provinces: L?u danh sách các t?nh/thành ph?
CREATE TABLE [dbo].[Provinces]
(
	[ProvinceName] [nvarchar](255) NOT NULL PRIMARY KEY
) 
GO
-- 2. B?ng Suppliers: L?u danh sách nhà cung c?p
CREATE TABLE [dbo].[Suppliers]
(
	[SupplierID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[SupplierName] [nvarchar](255) NOT NULL,
	[ContactName] [nvarchar](255) NOT NULL,
	[Province] [nvarchar](255) NULL,
	[Address] [nvarchar](255) NULL,
	[Phone] [nvarchar](255) NULL,
	[Email] [nvarchar](255) NULL
)
GO
-- 3. B?ng Customers: L?u danh sách khách hàng
CREATE TABLE [dbo].[Customers]
(
	[CustomerID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[CustomerName] [nvarchar](255) NOT NULL,
	[ContactName] [nvarchar](255) NOT NULL,
	[Province] [nvarchar](255) NULL,
	[Address] [nvarchar](255) NULL,
	[Phone] [nvarchar](255) NULL,
	[Email] [nvarchar](50) NULL,
	[Password] [nvarchar](50) NULL,
	[IsLocked] [bit] NULL
)
GO

-- 4. B?ng Employees: L?u d? li?u nhân viên
CREATE TABLE [dbo].[Employees]
(
	[EmployeeID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[FullName] [nvarchar](255) NOT NULL,
	[BirthDate] [date] NULL,
	[Address] [nvarchar](255) NULL,
	[Phone] [nvarchar](255) NULL,
	[Email] [nvarchar](50) NULL UNIQUE,
	[Password] [nvarchar](50) NULL,
	[Photo] [nvarchar](255) NULL,
	[IsWorking] [bit] NULL,
	[RoleNames] [nvarchar](500) NULL
)
GO

-- 5. B?ng Shippers: L?u d? li?u ng??i giao hàng
CREATE TABLE [dbo].[Shippers]
(
	[ShipperID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[ShipperName] [nvarchar](255) NOT NULL,
	[Phone] [nvarchar](255) NULL
)
GO

-- 6. B?ng Categories: L?u danh m?c lo?i hàng
CREATE TABLE [dbo].[Categories]
(
	[CategoryID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[CategoryName] [nvarchar](255) NOT NULL,
	[Description] [nvarchar](255) NULL
)
GO

-- 7. B?ng Products: L?u d? li?u m?t hàng
CREATE TABLE [dbo].[Products]
(
	[ProductID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[ProductName] [nvarchar](255) NOT NULL,
	[ProductDescription] [nvarchar](2000) NULL,
	[SupplierID] [int] NULL,
	[CategoryID] [int] NULL,
	[Unit] [nvarchar](255) NOT NULL,
	[Price] [money] NOT NULL,
	[Photo] [nvarchar](255) NULL,
	[IsSelling] [bit] NULL
)
GO

-- 8. B?ng ProductAttributes: L?u danh sách các thu?c tính c?a m?t hàng
CREATE TABLE [dbo].[ProductAttributes]
(
	[AttributeID] [bigint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[ProductID] [int] NOT NULL,
	[AttributeName] [nvarchar](255) NOT NULL,
	[AttributeValue] [nvarchar](500) NOT NULL,
	[DisplayOrder] [int] NOT NULL
)
GO

-- 9. B?ng ProductPhotos: L?u danh sách ?nh c?a m?t hàng
CREATE TABLE [dbo].[ProductPhotos]
(
	[PhotoID] [bigint] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[ProductID] [int] NOT NULL,
	[Photo] [nvarchar](255) NOT NULL,
	[Description] [nvarchar](255) NOT NULL,
	[DisplayOrder] [int] NOT NULL,
	[IsHidden] [bit] NOT NULL
)
GO

-- 10. B?ng OrderStatus: L?u d? li?u ??nh ngh?a các tr?ng thái c?a ??n hàng
CREATE TABLE [dbo].[OrderStatus]
(
	[Status] [int] NOT NULL PRIMARY KEY,
	[Description] [nvarchar](50) NOT NULL
)
GO

-- 11. B?ng Orders: L?u d? li?u ??n hangf
CREATE TABLE [dbo].[Orders]
(
	[OrderID] [int] IDENTITY(1,1) NOT NULL PRIMARY KEY,
	[CustomerID] [int] NULL,
	[OrderTime] [datetime] NOT NULL,
	[DeliveryProvince] [nvarchar](255) NULL,
	[DeliveryAddress] [nvarchar](255) NULL,
	[EmployeeID] [int] NULL,
	[AcceptTime] [datetime] NULL,
	[ShipperID] [int] NULL,
	[ShippedTime] [datetime] NULL,
	[FinishedTime] [datetime] NULL,
	[Status] [int] NOT NULL	
)
GO

-- 12. B?ng OrderDetails: L?u thông tin chi ti?t các m?t hàng ???c bán trong ??n hàng
CREATE TABLE [dbo].[OrderDetails]
(
	[OrderID] [int] NOT NULL,
	[ProductID] [int] NOT NULL,
	[Quantity] [int] NOT NULL,
	[SalePrice] [money] NOT NULL,
	PRIMARY KEY ([OrderID], [ProductID])
)
GO

-- Thi?t l?p m?i quan h? gi?a các b?ng
ALTER TABLE [dbo].[Suppliers]  
ADD FOREIGN KEY([Province])
	REFERENCES [dbo].[Provinces] ([ProvinceName])
GO

ALTER TABLE [dbo].[Customers]  
ADD	FOREIGN KEY([Province])
	REFERENCES [dbo].[Provinces] ([ProvinceName])
GO

ALTER TABLE [dbo].[Products]
ADD	FOREIGN KEY([CategoryID])
	REFERENCES [dbo].[Categories] ([CategoryID])
GO

ALTER TABLE [dbo].[Products]  
ADD	FOREIGN KEY([SupplierID])
	REFERENCES [dbo].[Suppliers] ([SupplierID])
GO

ALTER TABLE [dbo].[ProductAttributes] 
ADD	FOREIGN KEY([ProductID])
	REFERENCES [dbo].[Products] ([ProductID])
GO

ALTER TABLE [dbo].[ProductPhotos]
ADD	FOREIGN KEY([ProductID])
	REFERENCES [dbo].[Products] ([ProductID])
GO

ALTER TABLE [dbo].[Orders]  
ADD	FOREIGN KEY([CustomerID])
	REFERENCES [dbo].[Customers] ([CustomerID])
GO

ALTER TABLE [dbo].[Orders]  
ADD FOREIGN KEY([EmployeeID])
	REFERENCES [dbo].[Employees] ([EmployeeID])
GO

ALTER TABLE [dbo].[Orders]
ADD	FOREIGN KEY([ShipperID])
	REFERENCES [dbo].[Shippers] ([ShipperID])
GO

ALTER TABLE [dbo].[Orders]
ADD	FOREIGN KEY([Status])
	REFERENCES [dbo].[OrderStatus] ([Status])
GO

ALTER TABLE [dbo].[OrderDetails]  
ADD	FOREIGN KEY([OrderID])
	REFERENCES [dbo].[Orders] ([OrderID])
GO

ALTER TABLE [dbo].[OrderDetails]  
ADD FOREIGN KEY([ProductID])
	REFERENCES [dbo].[Products] ([ProductID])
GO


cho các l?p sau:
namespace SV22T1020218.Models.Common
{
    /// <summary>
    /// L?p dùng ?? bi?u di?n thông tin ??u vào c?a m?t truy v?n/tìm ki?m 
    /// d? li?u ??n gi?n d??i d?ng phân trang
    /// </summary>
    public class PaginationSearchInput
    {
        private const int MaxPageSize = 100; //Gi?i h?n t?i ?a 100 dòng m?i trang
        private int _page = 1;
        private int _pageSize = 20;
        private string _searchValue = "";
        
        /// <summary>
        /// Trang c?n ???c hi?n th? (b?t ??u t? 1)
        /// </summary>
        public int Page 
        { 
            get => _page;
            set => _page = value < 1 ? 1 : value;
        }
        /// <summary>
        /// S? dòng ???c hi?n th? trên m?i trang
        /// (0 có ngh?a là hi?n th? t?t c? các dòng trên m?t trang, t?c là không phân trang)
        /// </summary>
        public int PageSize 
        { 
            get => _pageSize; 
            set
            {
                if (value < 0)
                    _pageSize = 0;
                else if (value > MaxPageSize)
                    _pageSize = MaxPageSize;
                else
                    _pageSize = value;
            }
        }
        /// <summary>
        /// Giá tr? tìm ki?m (n?u có) ???c s? d?ng ?? l?c d? li?u 
        /// (N?u không có giá tr? tìm ki?m, thì ?? r?ng)
        /// </summary>
        public string SearchValue
        { 
            get => _searchValue; 
            set => _searchValue = value?.Trim() ?? ""; 
        }        
        /// <summary>
        /// S? dòng c?n b? qua (tính t? dòng ??u tiên c?a t?p d? li?u) 
        /// ?? l?y d? li?u cho trang hi?n t?i
        /// </summary>
        public int Offset => PageSize > 0 ? (Page - 1) * PageSize : 0;
    }
}

namespace SV22T1020218.Models.Common
{
    /// <summary>
    /// Ph?n t? trên thanh phân trang, có th? là m?t s? trang ho?c d?u "..." ?? phân cách các nhóm trang
    /// </summary>
    public class PageItem
    {
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="pageNumber">0 n?u là ph?n t? dùng ?? th? hi?n d?u "..." phân cách</param>
        /// <param name="isCurrent"></param>
        public PageItem(int pageNumber, bool isCurrent = false)
        {
            Page = pageNumber;
            IsCurrent = isCurrent;
        }
        /// <summary>
        /// S? trang (có giá tr? là 0 n?u là d?u "..." ?? phân cách các nhóm trang)
        /// </summary>
        public int Page { get; set; }
        /// <summary>
        /// Có ph?i là trang hi?n t?i hay không?
        /// </summary>
        public bool IsCurrent { get; set; }
        /// <summary>
        /// Có ph?i là v? trí hi?n th? d?u "..." ?? phân cách các nhóm trang hay không?
        /// </summary>
        public bool IsEllipsis => Page == 0;
    }
}


namespace SV22T1020218.Models.Common
{
    /// <summary>
    /// L?p dùng ?? bi?u di?n k?t qu? truy v?n/tìm ki?m d? li?u d??i d?ng phân trang
    /// </summary>
    /// <typeparam name="T">Ki?u c?a d? li?u truy v?n ???c</typeparam>
    public class PagedResult<T> where T : class
    {
        /// <summary>
        /// Trang ?ang ???c hi?n th?
        /// </summary>
        public int Page { get; set; }
        /// <summary>
        /// S? dòng ???c hi?n th? trên m?i trang (0 có ngh?a là hi?n th? t?t c? các dòng trên m?t trang/không phân trang)
        /// </summary>
        public int PageSize { get; set; }        
        /// <summary>
        /// T?ng s? dòng d? li?u ???c tìm th?y
        /// </summary>
        public int RowCount { get; set; }
        /// <summary>
        /// Danh sách các dòng d? li?u ???c hi?n th? trên trang hi?n t?i
        /// </summary>
        public List<T> DataItems { get; set; } = new List<T>();

        /// <summary>
        /// T?ng s? trang
        /// </summary>
        public int PageCount
        {
            get
            {
                if (PageSize == 0)
                    return 1;
                return (int)Math.Ceiling((decimal)RowCount / PageSize);
            }
        }
        /// <summary>
        /// Có trang tr??c không?
        /// </summary>
        public bool HasPreviousPage => Page > 1;
        /// <summary>
        /// Có trang sau không?
        /// </summary>
        public bool HasNextPage => Page < PageCount;             
        /// <summary>
        /// L?y danh sách các trang ???c hi?n th? trên thanh phân trang
        /// </summary>
        /// <param name="n">S? l??ng trang lân c?n trang hi?n t?i c?n ???c hi?n th?</param>
        /// <returns></returns>
        public List<PageItem> GetDisplayPages(int n = 5)
        {
            var result = new List<PageItem>();

            if (PageCount == 0)
                return result;

            n = n > 0 ? n : 5; //Giá tr? n không h?p l?, ??t l?i v? m?c ??nh            

            int currentPage = Page;
            if (currentPage < 1) 
                currentPage = 1;
            else if (currentPage > PageCount)
                currentPage = PageCount;

            int displayedPages = 2 * n + 1;     //S? l??ng trang t?i ?a hi?n th? trên thanh phân trang (bao g?m c? trang hi?n t?i)
            int startPage = currentPage - n;    //Trang b?t ??u hi?n th?
            int endPage = currentPage + n;      //Trang k?t thúc hi?n th?

            //N?u thi?u bên trái
            if (startPage < 1)
            {
                endPage += (1 - startPage);
                startPage = 1;
            }

            //N?u thi?u bên ph?i
            if (endPage > PageCount)
            {
                startPage -= (endPage - PageCount);
                endPage = PageCount;
            }

            //Gán l?i b?ng 1 n?u startPage b? âm sau khi tr?
            if (startPage < 1)
                startPage = 1;

            //??m b?o không v??t quá displayedPages
            if (endPage - startPage + 1 > displayedPages)
                endPage = startPage + displayedPages - 1;

            //Trang ??u
            if (startPage > 1)
            {
                result.Add(new PageItem(1, currentPage == 1));
                //Thêm d?u "..." ?? phân cách n?u có nhi?u trang ? gi?a
                if (startPage > 2)
                    result.Add(new PageItem(0));
            }

            //Trang hi?n t?i và các trang lân c?n
            for (int i = startPage; i <= endPage; i++)
            {
                result.Add(new PageItem(i, i == currentPage));
            }

            //Trang cu?i
            if (endPage < PageCount)
            {
                //Thêm d?u "..." ?? phân cách n?u có nhi?u trang ? gi?a
                if (endPage < PageCount - 1)
                    result.Add(new PageItem(0));
                result.Add(new PageItem(PageCount, currentPage == PageCount));
            }

            return result;
        }
    }
}

Cho interface nh? sau:
using SV22T1020218.Models.Common;

namespace SV22T1020218.DataLayers.Interfaces
{
    /// <summary>
    /// ??nh ngh?a các phép x? lý d? li?u ??n gi?n trên m?t
    /// ki?u d? li?u T nào ?ó (T là m?t Entity/DomainModel nào ?ó)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IGenericRepository<T> where T : class
    {
        /// <summary>
        /// Truy v?n, tìm ki?m d? li?u và tr? v? k?t qu? d??i d?ng ???c phân trang
        /// </summary>
        /// <param name="input">??u vào tìm ki?m, phân trang</param>
        /// <returns></returns>
        Task<PagedResult<T>> ListAsync(PaginationSearchInput input);
        /// <summary>
        /// L?y d? li?u c?a m?t b?n ghi có mã là id (tr? v? null n?u không có d? li?u)
        /// </summary>
        /// <param name="id">Mã c?a d? li?u c?n l?y</param>
        /// <returns></returns>
        Task<T?> GetAsync(int id);
        /// <summary>
        /// B? sung m?t b?n ghi vào b?ng trong CSDL
        /// </summary>
        /// <param name="data">D? li?u c?n b? sung</param>
        /// <returns>Mã c?a dòng d? li?u ???c b? sung (th??ng là IDENTITY)</returns>
        Task<int> AddAsync(T data);
        /// <summary>
        /// C?p nh?t m?t b?n ghi trong b?ng c?a CSDL
        /// </summary>
        /// <param name="data">D? li?u c?n c?p nh?t</param>
        /// <returns></returns>
        Task<bool> UpdateAsync(T data);
        /// <summary>
        /// Xóa b?n ghi có mã là id
        /// </summary>
        /// <param name="id">Mã c?a b?n ghi c?n xóa</param>
        /// <returns></returns>
        Task<bool> DeleteAsync(int id);
        /// <summary>
        /// Ki?m tra xem m?t b?n ghi có mã là id có d? li?u liên quan hay không?
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> IsUsed(int id);
    }
}

Vi?t l?p SupplierRepository cho entity Supplier sau, cài ??t interface trên.
namespace SV22T1020218.Models.Partner
{
    /// <summary>
    /// Nhà cung c?p
    /// </summary>
    public class Supplier
    {
        /// <summary>
        /// Mã nhà cung c?p
        /// </summary>
        public int SupplierID { get; set; }
        /// <summary>
        /// Tên nhà cung c?p
        /// </summary>
        public string SupplierName { get; set; } = string.Empty;
        /// <summary>
        /// Tên giao d?ch
        /// </summary>
        public string ContactName { get; set; } = string.Empty;
        /// <summary>
        /// T?nh thành
        /// </summary>
        public string? Province { get; set; }
        /// <summary>
        /// ??a ch?
        /// </summary>
        public string? Address { get; set; }
        /// <summary>
        /// ?i?n tho?i
        /// </summary>
        public string? Phone { get; set; }
        /// <summary>
        /// Email
        /// </summary>
        public string? Email { get; set; }
    }
}

Yêu c?u: 
- Contructer c?a l?p có tham s? ??u vào là connetionString.
- S? d?ng Dapper, Microsoft.Data.SqlClient ?? làm vi?c v?i CSDL SQL Server
- L?p thu?c namespace SV22T1020218.DataLayers.SQLServer
- vi?t ??y ?? summary cho l?p và hàm

