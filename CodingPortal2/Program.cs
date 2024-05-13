using CodingPortal2.Database;
using CodingPortal2.Services;
using Microsoft.EntityFrameworkCore;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages().AddRazorRuntimeCompilation();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseMySql(
        "server=localhost;port=3306;database=codingportaldb;user=root;password=;",
        new MySqlServerVersion(new Version(10, 4, 28)));
});

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(240); // Set session timeout as needed
});

builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddScoped<AssignmentService>();
builder.Services.AddScoped<GroupService>();
builder.Services.AddScoped<AssignmentCleanupService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Seed database on application start
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    try
    {
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        SeedData.Initialize(dbContext);
    }
    catch (Exception ex)
    {
        // Handle exceptions or logging if needed
    }
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseSession();

app.UseAuthorization();

app.MapRazorPages();

app.Run();