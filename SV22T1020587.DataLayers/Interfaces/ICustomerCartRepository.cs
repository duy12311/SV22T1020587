using SV22T1020587.Models.Sales;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SV22T1020587.DataLayers.Interfaces
{
    /// <summary>
    /// Persist shopping cart per customer.
    /// </summary>
    public interface ICustomerCartRepository
    {
        Task<List<CartItem>> GetCartAsync(int customerId);
        Task SaveCartAsync(int customerId, List<CartItem> cart);
        Task ClearCartAsync(int customerId);
    }
}