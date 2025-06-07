using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CodePilot.Data;
using CodePilot.Areas.Identity.Data;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("CodePilotContextConnection") ?? throw new InvalidOperationException("Connection string 'CodePilotContextConnection' not found.");

builder.Services.AddDbContext<CodePilotContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = true;
    options.User.RequireUniqueEmail = true;
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredUniqueChars = 0;
})
//.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<CodePilotContext>();

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/RegLog";
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseAuthentication();;

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Chat}/{action=Chat}/{id?}");

app.Run();
