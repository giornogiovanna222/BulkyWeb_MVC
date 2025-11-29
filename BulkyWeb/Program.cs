using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.DataAcess.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using BulkyBook.Utility;
using Stripe;
using BulkyBook.DataAccess.DbInitializer;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// normalde bu sekilde tanitmamiz gerekiyor ama unitofwork geldigi zaman hepsini ona atiyoruz.
// builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
// ve tanimlamayi sadece unitofwork uzerinden yapiyoruz. ama yine de dursun burada bu.

// Transient - Scoped ve Singleton.
// Transient her seferinde degisir. Kendi icinde bile. her istekte farkli gelir.
// Scoped istek basina 1 kez degisir. 2 tane scoped ayni anda istersen ayni gelir ama.
// Transient surekli degisir 1 ve 2 si farkli olur. Scoped 1. ve 2.si ( atilan isteklerde)
// ayni doner. Singleton ise program basina 1 kez belirlenir. Asla degismez program degismedikce.
// Scoped kullaniyoruz o yuzden her seferinde farkli istek atiyoruz ama transient gibi asiri degisken
// de degil.
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
	options.UseSqlServer(connectionString));

builder.Services.Configure<StripeSettings>(builder.Configuration.GetSection("Stripe"));

builder.Services.AddIdentity<IdentityUser,IdentityRole>().AddEntityFrameworkStores<ApplicationDbContext>().AddDefaultTokenProviders();
builder.Services.ConfigureApplicationCookie(options => {
    options.LoginPath = $"/Identity/Account/Login";
    options.LogoutPath = $"/Identity/Account/Logout";
    options.AccessDeniedPath = $"/Identity/Account/AccessDenied";
});
builder.Services.AddAuthentication().AddFacebook(option => {
    option.AppId = "";
    option.AppSecret = "";
});
builder.Services.AddAuthentication().AddMicrosoftAccount(option => {
    option.ClientId = "e";
    option.ClientSecret = "";
});

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options => {
    options.IdleTimeout = TimeSpan.FromMinutes(100);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<IDbInitializer, DbInitializer>();
builder.Services.AddRazorPages();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
StripeConfiguration.ApiKey = builder.Configuration.GetSection("Stripe:UnpublishableKey").Get<string>();
app.UseRouting();
app.UseAuthentication();// id password doðru mu kontrol etsin diye ekledik. authen authorizationdan önce gelmek zorunda eklendiðinde.
app.UseAuthorization(); 
app.UseSession();
SeedDatabase();
app.MapRazorPages();
app.MapControllerRoute(
    name: "default",
    pattern: "{area=Customer}/{controller=Home}/{action=Index}/{id?}");

app.Run();


void SeedDatabase() {
    using (var scope = app.Services.CreateScope()) {
        var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
        dbInitializer.Initialize();
    }
}
