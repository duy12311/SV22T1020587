using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace SV22T1020587.Shop.AppCodes
{
    /// <summary>
    /// Lớp cung cấp các tiện ích liên quan đến ngữ cảnh của ứng dụng web (Shop)
    /// </summary>
    public static class ApplicationContext
    {
        private static IHttpContextAccessor? _httpContextAccessor;
        private static IWebHostEnvironment? _webHostEnvironment;
        private static IConfiguration? _configuration;

        /// <summary>
        /// Gọi hàm này trong Program.cs
        /// </summary>
        public static void Configure(
            IHttpContextAccessor httpContextAccessor,
            IWebHostEnvironment webHostEnvironment,
            IConfiguration configuration)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _webHostEnvironment = webHostEnvironment ?? throw new ArgumentNullException(nameof(webHostEnvironment));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <summary>
        /// HttpContext hiện tại
        /// </summary>
        public static HttpContext? HttpContext => _httpContextAccessor?.HttpContext;

        /// <summary>
        /// Environment của Web
        /// </summary>
        public static IWebHostEnvironment? WebHostEnvironment => _webHostEnvironment;

        /// <summary>
        /// Configuration
        /// </summary>
        public static IConfiguration? Configuration => _configuration;

        /// <summary>
        /// URL gốc của website
        /// </summary>
        public static string BaseURL =>
            $"{HttpContext?.Request.Scheme}://{HttpContext?.Request.Host}/";

        /// <summary>
        /// Đường dẫn vật lý tới thư mục wwwroot
        /// </summary>
        public static string WWWRootPath =>
            _webHostEnvironment?.WebRootPath ?? "";

        /// <summary>
        /// Đường dẫn vật lý tới thư mục gốc ứng dụng
        /// </summary>
        public static string ApplicationRootPath =>
            _webHostEnvironment?.ContentRootPath ?? "";

        /// <summary>
        /// Ghi dữ liệu vào Session
        /// </summary>
        public static void SetSessionData(string key, object value)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };
                string sValue = JsonSerializer.Serialize(value, options);
                if (!string.IsNullOrEmpty(sValue))
                {
                    _httpContextAccessor?
                        .HttpContext?
                        .Session
                        .SetString(key, sValue);
                }
            }
            catch
            {
           
            }
        }

        /// <summary>
        /// Đọc dữ liệu từ Session
        /// </summary>
        public static T? GetSessionData<T>(string key) where T : class
        {
            try
            {
                string sValue =
                    _httpContextAccessor?
                    .HttpContext?
                    .Session
                    .GetString(key) ?? "";

                if (!string.IsNullOrEmpty(sValue))
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<T>(sValue, options);
                }
            }
            catch
            {
            }
            return null;
        }

        /// <summary>
        /// Lấy giá trị cấu hình trong appsettings.json
        /// </summary>
        public static string GetConfigValue(string name)
        {
            return _configuration?[name] ?? "";
        }

        /// <summary>
        /// Lấy section cấu hình
        /// </summary>
        public static T GetConfigSection<T>(string name) where T : new()
        {
            var value = new T();
            _configuration?.GetSection(name).Bind(value);
            return value;
        }
    }
}