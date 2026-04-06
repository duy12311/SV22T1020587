using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.Models.Common;
using SV22T1020587.Models.Sales;
using System;
using System.Data;
using System.Globalization;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SV22T1020587.DataLayers.SQLServer
{
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        public OrderRepository(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        }

        #region Orders

        public int InitOrder(int? employeeID, int? customerID, string orderType, string deliveryProvince, string deliveryAddress, List<OrderDetail> details, string? customerName = null, string? customerPhone = null, string? paymentMethod = null)
        {
            int orderId = 0;

            if (employeeID <= 0)
            {
                employeeID = null;
            }

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string sqlOrder = @"
                            INSERT INTO Orders (CustomerID, CustomerName, CustomerPhone, OrderType, OrderTime, DeliveryProvince, DeliveryAddress, EmployeeID, Status, PaymentMethod)
                            VALUES (@CustomerID, @CustomerName, @CustomerPhone, @OrderType, GETDATE(), @DeliveryProvince, @DeliveryAddress, @EmployeeID, 1, @PaymentMethod);
                            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                        orderId = connection.ExecuteScalar<int>(sqlOrder, new
                        {
                            CustomerID = customerID,
                            CustomerName = customerName,
                            CustomerPhone = customerPhone,
                            OrderType = orderType,
                            DeliveryProvince = deliveryProvince,
                            DeliveryAddress = deliveryAddress,
                            EmployeeID = employeeID,
                            PaymentMethod = paymentMethod
                        }, transaction);

                        if (details != null && details.Count > 0)
                        {
                            string sqlDetail = @"
                                INSERT INTO OrderDetails (OrderID, ProductID, Quantity, SalePrice)
                                VALUES (@OrderID, @ProductID, @Quantity, @SalePrice)";

                            var detailParams = details.Select(item => new
                            {
                                OrderID = orderId,
                                ProductID = item.ProductID,
                                Quantity = item.Quantity,
                                SalePrice = item.SalePrice
                            });

                            connection.Execute(sqlDetail, detailParams, transaction);
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        orderId = 0;
                        throw new Exception("Lỗi khởi tạo đơn hàng tại Repository: " + ex.Message, ex);
                    }
                }
            }
            return orderId;
        }

        public async Task<int> AddAsync(Order data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                INSERT INTO Orders (CustomerID, OrderType, OrderTime, DeliveryProvince, DeliveryAddress, EmployeeID, Status, PaymentMethod)
                VALUES (@CustomerID, @OrderType, @OrderTime, @DeliveryProvince, @DeliveryAddress, @EmployeeID, @Status, @PaymentMethod);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";
            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> DeleteAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();
            try
            {
                var parameters = new DynamicParameters();
                parameters.Add("OrderID", orderID);

                // Delete details first
                string sqlDeleteDetails = "DELETE FROM OrderDetails WHERE OrderID = @OrderID";
                await connection.ExecuteAsync(sqlDeleteDetails, parameters, transaction);

                // Delete order
                string sqlDeleteOrder = "DELETE FROM Orders WHERE OrderID = @OrderID";
                int rows = await connection.ExecuteAsync(sqlDeleteOrder, parameters, transaction);

                transaction.Commit();
                return rows > 0;
            }
            catch
            {
                try { transaction.Rollback(); } catch { /* ignore */ }
                throw;
            }
        }

        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT o.OrderID, o.CustomerID, o.OrderType, o.OrderTime, o.DeliveryProvince, o.DeliveryAddress, 
                       o.EmployeeID, o.AcceptTime, o.ShipperID, o.ShippedTime, o.FinishedTime, o.Status, o.PaymentMethod,
                       ISNULL(o.CustomerName, c.CustomerName) AS CustomerName, 
                       c.ContactName as CustomerContactName, 
                       c.Address as CustomerAddress, 
                       ISNULL(o.CustomerPhone, c.Phone) as CustomerPhone, 
                       c.Email as CustomerEmail,
                       e.FullName as EmployeeName,
                       s.ShipperName, s.Phone as ShipperPhone
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                WHERE o.OrderID = @OrderID";
            return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { OrderID = orderID });
        }

        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            string where = "WHERE 1=1";

            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                // Thay đổi tìm kiếm để hỗ trợ tìm cả tên khách vãng lai
                where += " AND (c.CustomerName LIKE @SearchValue OR o.CustomerName LIKE @SearchValue OR s.ShipperName LIKE @SearchValue)";
                parameters.Add("SearchValue", $"%{input.SearchValue}%");
            }

            if (input.Status != 0)
            {
                where += " AND (o.Status = @Status)";
                parameters.Add("Status", input.Status);
            }

            if (!string.IsNullOrWhiteSpace(input.DateFrom) &&
                DateTime.TryParseExact(input.DateFrom, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dFrom))
            {
                where += " AND (o.OrderTime >= @DateFrom)";
                parameters.Add("DateFrom", dFrom);
            }

            if (!string.IsNullOrWhiteSpace(input.DateTo) &&
                DateTime.TryParseExact(input.DateTo, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dTo))
            {
                dTo = dTo.AddDays(1).AddTicks(-1);
                where += " AND (o.OrderTime <= @DateTo)";
                parameters.Add("DateTo", dTo);
            }

            string sqlCount = $@"
                SELECT COUNT(1)
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                {where}";
            int rowCount = await connection.ExecuteScalarAsync<int>(sqlCount, parameters);

            var result = new PagedResult<OrderViewInfo>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = rowCount,
                DataItems = new List<OrderViewInfo>()
            };

            if (rowCount == 0) return result;

            string sql = $@"
                SELECT o.OrderID, o.CustomerID, o.OrderType, o.OrderTime, o.DeliveryProvince, o.DeliveryAddress,
                       o.EmployeeID, o.AcceptTime, o.ShipperID, o.ShippedTime, o.FinishedTime, o.Status, o.PaymentMethod,
                       ISNULL(o.CustomerName, c.CustomerName) AS CustomerName, 
                       e.FullName as EmployeeName, s.ShipperName
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                {where}
                ORDER BY o.OrderID DESC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            parameters.Add("Offset", input.Offset);
            parameters.Add("PageSize", input.PageSize);

            var data = await connection.QueryAsync<OrderViewInfo>(sql, parameters);
            result.DataItems = data.ToList();
            return result;
        }

        public async Task<List<OrderViewInfo>> ListByCustomerAsync(int customerID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT o.OrderID, o.CustomerID, o.OrderType, o.OrderTime, o.DeliveryProvince, o.DeliveryAddress, 
                       o.EmployeeID, o.AcceptTime, o.ShipperID, o.ShippedTime, o.FinishedTime, o.Status, o.PaymentMethod, 
                       ISNULL(o.CustomerName, c.CustomerName) AS CustomerName, 
                       e.FullName as EmployeeName, s.ShipperName
                FROM Orders o
                LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                WHERE o.CustomerID = @CustomerID
                ORDER BY o.OrderID DESC";
            var data = await connection.QueryAsync<OrderViewInfo>(sql, new { CustomerID = customerID });
            return data.ToList();
        }

        public async Task<bool> UpdateAsync(Order data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE Orders
                SET CustomerID = @CustomerID,
                    OrderType = @OrderType,
                    DeliveryProvince = @DeliveryProvince,
                    DeliveryAddress = @DeliveryAddress,
                    EmployeeID = @EmployeeID,
                    AcceptTime = @AcceptTime,
                    ShipperID = @ShipperID,
                    ShippedTime = @ShippedTime,
                    FinishedTime = @FinishedTime,
                    Status = @Status,
                    PaymentMethod = @PaymentMethod
                WHERE OrderID = @OrderID";
            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        #endregion

        #region OrderDetails

        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                IF NOT EXISTS (SELECT 1 FROM OrderDetails WHERE OrderID = @ORDERID AND ProductID = @PRODUCTID)
                    INSERT INTO OrderDetails (OrderID, ProductID, Quantity, SalePrice)
                    VALUES (@ORDERID, @PRODUCTID, @Quantity, @SalePrice)
                ELSE
                    UPDATE OrderDetails 
                    SET Quantity = Quantity + @Quantity, SalePrice = @SalePrice
                    WHERE OrderID = @ORDERID AND ProductID = @PRODUCTID";
            int rows = await connection.ExecuteAsync(sql, data);
            return rows > 0;
        }

        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT od.OrderID, od.ProductID, od.Quantity, od.SalePrice, 
                       p.ProductName, p.Unit, p.Photo, (od.Quantity * od.SalePrice) as TotalPrice
                FROM OrderDetails od
                JOIN Products p ON od.ProductID = p.ProductID
                WHERE od.OrderID = @ORDERID";
            var data = await connection.QueryAsync<OrderDetailViewInfo>(sql, new { ORDERID = orderID });
            return data.ToList();
        }

        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = "DELETE FROM OrderDetails WHERE OrderID = @OrderID AND ProductID = @ProductID";
            int rows = await connection.ExecuteAsync(sql, new { OrderID = orderID, ProductID = productID });
            return rows > 0;
        }

        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                SELECT od.OrderID, od.ProductID, od.Quantity, od.SalePrice, 
                       p.ProductName, p.Unit, p.Photo
                FROM OrderDetails od
                JOIN Products p ON od.ProductID = p.ProductID
                WHERE od.OrderID = @ORDERID AND od.ProductID = @PRODUCTID";
            return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(sql, new { ORDERID = orderID, PRODUCTID = productID });
        }

        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"
                UPDATE OrderDetails
                SET Quantity = @Quantity,
                    SalePrice = @SalePrice
                WHERE OrderID = @ORDERID AND ProductID = @PRODUCTID";
            int rows = await connection.ExecuteAsync(sql, new { ORDERID = data.OrderID, PRODUCTID = data.ProductID, data.Quantity, data.SalePrice });
            return rows > 0;
        }

        #endregion

        #region Status actions

        public bool Accept(int orderID, int employeeID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"UPDATE Orders SET Status = 2, EmployeeID = @EmployeeID, AcceptTime = GETDATE() WHERE OrderID = @ORDERID AND Status = 1";
            return connection.Execute(sql, new { ORDERID = orderID, EmployeeID = employeeID }) > 0;
        }

        public bool Ship(int orderID, int shipperID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"UPDATE Orders SET Status = 3, ShipperID = @ShipperID, ShippedTime = GETDATE() WHERE OrderID = @ORDERID AND Status = 2";
            return connection.Execute(sql, new { ORDERID = orderID, ShipperID = shipperID }) > 0;
        }

        public bool Finish(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"UPDATE Orders SET Status = 4, FinishedTime = GETDATE() WHERE OrderID = @ORDERID AND Status = 3";
            return connection.Execute(sql, new { ORDERID = orderID }) > 0;
        }

        public bool IssueInvoice(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"UPDATE Orders SET Status = 4 WHERE OrderID = @ORDERID AND Status >= 1";
            return connection.Execute(sql, new { ORDERID = orderID }) > 0;
        }

        public bool Issue(int orderID) => IssueInvoice(orderID);

        public bool Cancel(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"UPDATE Orders SET Status = -1 WHERE OrderID = @ORDERID AND Status IN (1,2)";
            return connection.Execute(sql, new { ORDERID = orderID }) > 0;
        }

        public bool Reject(int orderID)
        {
            using var connection = new SqlConnection(_connectionString);
            string sql = @"UPDATE Orders SET Status = -2 WHERE OrderID = @ORDERID AND Status IN (1,2)";
            return connection.Execute(sql, new { ORDERID = orderID }) > 0;
        }

        #endregion
    }
}