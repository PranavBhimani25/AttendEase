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

// =================== MVC ===================
builder.Services.AddControllersWithViews();

builder.Services.AddScoped<AdminFilter>();
builder.Services.AddScoped<EmployeeFilter>();
builder.Services.AddScoped<CloudinaryService>();

// =================== Redis ===================
builder.Services.Configure<RedisConfig>(
    builder.Configuration.GetSection("Redis")
);

// Register IConnectionMultiplexer — reads Host from appsettings.json
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var config = sp.GetRequiredService<IOptions<RedisConfig>>().Value;

    var options = new ConfigurationOptions
    {
        EndPoints = { { config.Host, config.Port } },
        Password  = config.Password,
        Ssl       = config.Ssl,
        AbortOnConnectFail = false,
        ConnectTimeout     = 10000,
        SyncTimeout        = 10000,
          // Allow the SCAN command used by InvalidateByPrefixAsync
        AllowAdmin         = true
    };

    return ConnectionMultiplexer.Connect(options);
});

// Register RedisService AFTER IConnectionMultiplexer so DI can inject it correctly
builder.Services.AddScoped<RedisService>();

// =================== RabbitMQ ===================
builder.Services.Configure<RabbitMQConfig>(
    builder.Configuration.GetSection("RabbitMQ")
);
builder.Services.AddScoped<RabbitMQService>();
builder.Services.AddSingleton<NotificationService>();
builder.Services.AddSingleton<NotificationPublisher>();
builder.Services.AddHostedService<RabbitMqNotificationConsumer>();
builder.Services.AddHostedService<RabbitMqAttendanceConsumer>();  

// =================== Elasticsearch ===================
// builder.Services.Configure<ElasticsearchConfig>(
//     builder.Configuration.GetSection("Elasticsearch")
// );
// builder.Services.AddScoped<ElasticsearchService>();

// // Safe log — no crash if section is missing
// var esConfig = builder.Configuration.GetSection("Elasticsearch").Get<ElasticsearchConfig>();
// Console.WriteLine($"Elasticsearch URL: {esConfig?.Url ?? "Not configured"}");

// =================== Repositories ===================
builder.Services.AddScoped<IAuthInterface,       AuthRepository>();
builder.Services.AddScoped<IAdminInterface,      AdminRepository>();
builder.Services.AddScoped<IEmployeeInterface,   EmployeeRepository>();
builder.Services.AddScoped<IAttendanceInterface, AttendanceRepository>();
builder.Services.Configure<EmailModal>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<MailService>();

builder.Services.AddScoped<NpgsqlConnection>(_ =>
    new NpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

// =================== Session ===================
builder.Services.AddSession(options =>
{
    options.IdleTimeout      = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly  = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

// =================== Middleware ===================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.UseSession();

app.MapControllerRoute(
    name:    "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}");

app.Run();
