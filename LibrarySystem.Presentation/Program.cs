using LibrarySystem.BusinessLogic.Books;
using LibrarySystem.BusinessLogic.Options;
using LibrarySystem.BusinessLogic.PasswordHashers;
using LibrarySystem.BusinessLogic.Tokens;
using LibrarySystem.BusinessLogic.Users;
using LibrarySystem.Data.Books;
using LibrarySystem.Data.Cache;
using LibrarySystem.Data.Cache.Jobs;
using LibrarySystem.Data.Migrations;
using LibrarySystem.Data.Users;
using LibrarySystem.Presentation.Middlewares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using StackExchange.Redis;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")!;

MigrationRunner.Run(connectionString);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped(_ =>
    new NpgsqlConnection(connectionString)
);

var redisConnectionString = builder.Configuration.GetConnectionString("Redis")!;
builder.Services.AddStackExchangeRedisCache(redisOpitons =>
{
    redisOpitons.Configuration = redisConnectionString;
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(redisConnectionString)
);

builder.Services.AddHybridCache();

builder.Services.AddScoped<IHybridCacheService, HybridCacheService>();

builder.Services.AddHostedService<CacheInvalidationKeyBackgroundService>();
builder.Services.AddHostedService<CacheInvalidationTagBackgroundService>();

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<ITokenProvider, TokenProvider>();
builder.Services.AddScoped<IBookService, BookService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<PasswordHasher>();

builder.Services.AddHttpContextAccessor();

var jwtOptionsSection = builder.Configuration.GetSection("JwtOptions");

builder.Services.Configure<JwtOptions>(jwtOptionsSection);

var jwtOptions = jwtOptionsSection.Get<JwtOptions>();

var signingKey = Encoding.ASCII.GetBytes(jwtOptions!.SigningKey);

var tokenValidationParameters = new TokenValidationParameters
{
    ValidateIssuerSigningKey = true,
    IssuerSigningKey = new SymmetricSecurityKey(signingKey),
    ValidateIssuer = true,
    ValidIssuer = jwtOptions.ValidIssuer,
    ValidateAudience = true,
    ValidAudience = jwtOptions.ValidAudience,
    RequireExpirationTime = true,
    ValidateLifetime = true,
};

builder.Services.AddSingleton(tokenValidationParameters);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = tokenValidationParameters;
    options.SaveToken = true;
})
.AddCookie(options =>
{
    options.LoginPath = "/User/Login";
    options.AccessDeniedPath = "/Home/AccessDenied";
    options.ExpireTimeSpan = jwtOptions.ExpireTime;
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

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
app.UseRouting();

app.Use(async (context, next) =>
{
    if (context.Request.Cookies.TryGetValue("LibrarySystem.AuthToken", out var token))
    {
        context.Request.Headers.Authorization = $"Bearer {token}";
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.UseExceptionHandler();

app.Run();
