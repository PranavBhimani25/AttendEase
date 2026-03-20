using Microsoft.Extensions.Options;
using MVCAttendEase.Filters;
using MVCAttendEase.Models;
using MVCAttendEase.Services;
using Npgsql;
using Repositories.Implementation;
using Repositories.Interfaces;
using Repositories.Models;
using StackExchange.Redis;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<AdminFilter>();
builder.Services.AddScoped<EmployeeFilter>();
builder.Services.AddScoped<CloudinaryService>();

// ===================Radis Configuration===================
builder.Services.AddScoped<RedisService>();

builder.Services.Configure<RedisConfig>(
    builder.Configuration.GetSection("Redis")
);

// var redisTest = builder.Configuration.GetSection("Redis").Get<RedisConfig>();
// Console.WriteLine("==== REDIS CONFIG ====");
// Console.WriteLine(redisTest.Host);
// Console.WriteLine(redisTest.Port);

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = sp.GetRequiredService<IOptions<RedisConfig>>().Value;

    if (string.IsNullOrEmpty(config.Host))
        throw new Exception("Redis Host is NULL ❌");

    var options = new ConfigurationOptions
    {
        EndPoints = { { config.Host, config.Port } },
        Password = config.Password,
        Ssl = config.Ssl,
        AbortOnConnectFail = false,
        ConnectTimeout = 10000,
        SyncTimeout = 10000
    };

    return ConnectionMultiplexer.Connect(options);
});

// ===================RabbitMQ Configuration===================

builder.Services.Configure<RabbitMQConfig>(
    builder.Configuration.GetSection("RabbitMQ")
);

builder.Services.AddScoped<RabbitMQService>();

// ===================Elasticsearch Configuration===================
builder.Services.Configure<ElasticsearchConfig>(
    builder.Configuration.GetSection("Elasticsearch")
);
builder.Services.AddScoped<ElasticsearchService>();

var test = builder.Configuration.GetSection("Elasticsearch").Get<ElasticsearchConfig>();
Console.WriteLine(test.Url);

builder.Services.AddScoped<IAuthInterface, AuthRepository>();
builder.Services.AddScoped<IAdminInterface, AdminRepository>();
builder.Services.AddScoped<IEmployeeInterface, EmployeeRepository>();
builder.Services.AddScoped<IAttendanceInterface, AttendanceRepository>();
builder.Services.AddScoped<INotificationInterface, NotificationRepository>();
builder.Services.Configure<EmailModal>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<MailService>();

builder.Services.AddScoped<NpgsqlConnection>(_ =>
    new NpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

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

app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
