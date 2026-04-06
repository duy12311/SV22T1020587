using Microsoft.Data.SqlClient;
using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.Models.Security;
using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.Models.Security;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using System;
using SV22T1020587.Models.Common;
using SV22T1020587.Models.Partner;
using System.Collections.Generic;
using System.Linq;

namespace SV22T1020587.DataLayers.SQLServer
{
    /// <summary>
    /// Xử lý dữ liệu liên quan đến tài khoản của khách hàng trên SQL Server
    /// </summary>
    public class CustomerAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;
        private const int Pbkdf2Iter = 10000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        public CustomerAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<UserAccount?> Authorize(string userName, string password)
        {
            // Basic input guard
            if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(password))
                return null;

            using var connection = new SqlConnection(_connectionString);

            // Normalize email for lookup to avoid case/whitespace issues
            var userNameNormalized = userName.Trim().ToLowerInvariant();

            string sql = @"
                SELECT 
                    CAST(CustomerID AS VARCHAR) AS UserId,
                    Email AS UserName,
                    CustomerName AS DisplayName,
                    Email AS Email,
                    '' AS Photo,
                    'Customer' AS RoleNames,
                    Password
                FROM Customers 
                WHERE LOWER(Email) = @userNameNormalized AND IsLocked = 0";

            var row = await connection.QueryFirstOrDefaultAsync<AuthRow>(sql, new { userNameNormalized });
            if (row == null) return null;

            var stored = row.Password ?? string.Empty;

            bool verified = false;
            try
            {
                verified = VerifyPassword(password, stored);
            }
            catch
            {
                // verification failure (malformed stored hash) -> treat as unauthorized
                verified = false;
            }

            if (!verified) return null;

            return new UserAccount
            {
                UserId = row.UserId,
                UserName = row.UserName,
                DisplayName = row.DisplayName,
                Email = row.Email,
                Photo = row.Photo,
                RoleNames = row.RoleNames
            };
        }

        public async Task<bool> ChangePassword(string userName, string password)
        {
            using var connection = new SqlConnection(_connectionString);

            // Hash password using PBKDF2 and store formatted string
            string hashed = HashPasswordPbkdf2(password);

            string sql = @"
                UPDATE Customers 
                SET Password = @password 
                WHERE LOWER(Email) = @userNameNormalized";

            var userNameNormalized = userName?.Trim().ToLowerInvariant() ?? string.Empty;

            int rowsAffected = await connection.ExecuteAsync(sql, new { userNameNormalized, password = hashed });
            return rowsAffected > 0;
        }

        private static bool VerifyPassword(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored))
                return false;

            if (stored.StartsWith("pbkdf2$"))
            {
                // Format: pbkdf2$iterations$base64salt$base64hash
                var parts = stored.Split('$');
                if (parts.Length == 4 && int.TryParse(parts[1], out int iter))
                {
                    var salt = Convert.FromBase64String(parts[2]);
                    var hash = Convert.FromBase64String(parts[3]);
                    using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iter, HashAlgorithmName.SHA256);
                    var computed = pbkdf2.GetBytes(hash.Length);
                    return CryptographicOperations.FixedTimeEquals(computed, hash);
                }
                return false;
            }

            // legacy MD5 (32 hex chars)
            if (stored.Length == 32 && IsHexString(stored))
            {
                var md5 = ComputeMd5Hex(password);
                return string.Equals(md5, stored, System.StringComparison.OrdinalIgnoreCase);
            }

            // fallback - plain text compare (try to avoid)
            return string.Equals(password, stored, System.StringComparison.Ordinal);
        }

        private static string HashPasswordPbkdf2(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Pbkdf2Iter, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(HashSize);
            return $"pbkdf2${Pbkdf2Iter}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        private static string ComputeMd5Hex(string input)
        {
            using var md5 = MD5.Create();
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            foreach (var b in hashBytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static bool IsHexString(string s)
        {
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                bool ok = (c >= '0' && c <= '9') ||
                          (c >= 'a' && c <= 'f') ||
                          (c >= 'A' && c <= 'F');
                if (!ok) return false;
            }
            return true;
        }

        // Helper class to read password column with Dapper
        private class AuthRow
        {
            public string UserId { get; set; } = "";
            public string UserName { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public string Email { get; set; } = "";
            public string Photo { get; set; } = "";
            public string RoleNames { get; set; } = "";
            public string? Password { get; set; }
        }
    }
}