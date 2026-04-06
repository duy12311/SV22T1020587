using SV22T1020587.DataLayers.Interfaces;
using SV22T1020587.DataLayers.SQLServer;
using SV22T1020587.Models.Catalog;
using SV22T1020587.Shop.AppCodes;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});


builder.Services.AddHttpContextAccessor();


builder.Services.AddAuthentication("CustomerAuth")
    .AddCookie("CustomerAuth", options =>
    {
        options.Cookie.Name = "CustomerCookie";
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });

builder.Services.AddControllersWithViews();


var connectionString = builder.Configuration.GetConnectionString("LiteCommerceDB");
SV22T1020587.BusinessLayers.Configuration.Initialize(connectionString);


builder.Services.AddScoped<IProductRepository>(sp => new ProductRepository(connectionString));
builder.Services.AddScoped<ICustomerRepository>(sp => new CustomerRepository(connectionString));
builder.Services.AddScoped<IUserAccountRepository>(sp => new CustomerAccountRepository(connectionString));
builder.Services.AddScoped<IOrderRepository>(sp => new OrderRepository(connectionString));
builder.Services.AddScoped<IGenericRepository<Category>>(sp => new CategoryRepository(connectionString));


var app = builder.Build();

SV22T1020587.Shop.AppCodes.ApplicationContext.Configure(
    app.Services.GetRequiredService<IHttpContextAccessor>(),
    app.Environment,
    app.Configuration
);

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "profile",
    pattern: "Profile",
    defaults: new { controller = "Account", action = "Profile" });

// Default route
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();