using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.Models.Sales;

namespace SV22T1020587.DataLayers.SQL
{
    /// <summary>
    /// Legacy wrapper to avoid duplicate type conflicts.
    /// Use the implementation in SV22T1020587.DataLayers.SQLServer.CustomerCartRepository.
    /// This class simply delegates to the SQLServer implementation.
    /// </summary>
    [Obsolete("CustomerCartRepository moved to SV22T1020587.DataLayers.SQLServer.CustomerCartRepository. Use that type instead.")]
    public class CustomerCartRepositoryLegacy : ICustomerCartRepository
    {
        private readonly SV22T1020587.DataLayers.SQLServer.CustomerCartRepository _impl;

        public CustomerCartRepositoryLegacy(string connectionString)
        {
            _impl = new SV22T1020587.DataLayers.SQLServer.CustomerCartRepository(connectionString);
        }

        public Task<List<CartItem>> GetCartAsync(int customerId) => _impl.GetCartAsync(customerId);

        public Task SaveCartAsync(int customerId, List<CartItem> cart) => _impl.SaveCartAsync(customerId, cart);

        public Task ClearCartAsync(int customerId) => _impl.ClearCartAsync(customerId);
    }
}