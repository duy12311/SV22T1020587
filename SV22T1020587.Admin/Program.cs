using SV22T1020587.Admin.AppCodes;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Globalization;
using Microsoft.AspNetCore.Localization;

var builder = WebApplication.CreateBuilder(args);

// =======================
// Lấy chuỗi kết nối
// =======================
// Phải lấy chuỗi kết nối ở đây để truyền vào các Repository trước khi Build()
string connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB")
    ?? throw new InvalidOperationException("ConnectionString 'LiteCommerceDB' not found.");

// =======================
// Add services
// =======================
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllersWithViews()
    .AddMvcOptions(option =>
    {
        option.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });

// 📌 BỔ SUNG Ở ĐÂY: Đăng ký IOrderRepository vào hệ thống Dependency Injection
builder.Services.AddScoped<SV22T1020587.DataLayers.Interfaces.IOrderRepository>(provider =>
    new SV22T1020587.DataLayers.SQLServer.OrderRepository(connectionString));

// 1. Cấu hình Authentication
builder.Services.AddAuthentication("AdminWebAuth")
    .AddCookie("AdminWebAuth", option =>
    {
        option.Cookie.Name = "LiteCommerce.Admin.Auth";
        option.LoginPath = "/Account/Login";
        option.LogoutPath = "/Account/Logout";
        option.AccessDeniedPath = "/Account/AccessDenied";
        option.ExpireTimeSpan = TimeSpan.FromDays(7);
        option.SlidingExpiration = true;
        option.Cookie.HttpOnly = true;
    });

// 2. Cấu hình Session
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(option =>
{
    option.IdleTimeout = TimeSpan.FromMinutes(60);
    option.Cookie.HttpOnly = true;
    option.Cookie.IsEssential = true;
});

var app = builder.Build();

// =======================
// Configure pipeline (Thứ tự cực kỳ quan trọng)
// =======================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Chỉ dùng HttpsRedirection khi KHÔNG phải môi trường Development 
// hoặc khi bạn đã cấu hình Port HTTPS rõ ràng để tránh lỗi Warn [3]
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

// Đặt UseSession ở đây để các trang có thể truy cập Session sớm
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// =======================
// Culture (Tiếng Việt)
// =======================
var supportedCultures = new[] { new CultureInfo("vi-VN") };
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture("vi-VN"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});

// =======================
// Initialize Context
// =======================
ApplicationContext.Configure(
    app.Services.GetRequiredService<IHttpContextAccessor>(),
    app.Services.GetRequiredService<IWebHostEnvironment>(),
    app.Configuration
);

// Khởi tạo BusinessLayer bằng connectionString đã lấy ở trên
SV22T1020587.BusinessLayers.Configuration.Initialize(connectionString);

// =======================
// Routing & Run
// =======================
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();