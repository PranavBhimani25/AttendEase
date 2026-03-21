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

// =================== Elasticsearch ===================
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
builder.Services.Configure<EmailModal>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<MailService>();

builder.Services.AddScoped<NpgsqlConnection>(_ =>
    new NpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")));

// // Safe log — no crash if section is missing
// var esConfig = builder.Configuration.GetSection("Elasticsearch").Get<ElasticsearchConfig>();
// Console.WriteLine($"Elasticsearch URL: {esConfig?.Url ?? "Not configured"}");

// =================== Repositories ===================
builder.Services.AddScoped<IAuthInterface,       AuthRepository>();
builder.Services.AddScoped<IAdminInterface,      AdminRepository>();
builder.Services.AddScoped<IEmployeeInterface,   EmployeeRepository>();
builder.Services.AddScoped<IAttendanceInterface, AttendanceRepository>();
builder.Services.AddScoped<INotificationInterface, NotificationRepository>();
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
app.UseSession();
app.UseAuthorization();



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

async Task IndexElasticData()
{
    using var scope = app.Services.CreateScope();

    var es = scope.ServiceProvider.GetRequiredService<ElasticsearchService>();
    var adminRepo = scope.ServiceProvider.GetRequiredService<IAdminInterface>();
    var employeeRepo = scope.ServiceProvider.GetRequiredService<IEmployeeInterface>();

    // ================= CREATE INDEX =================
    await es.CreateAttendanceIndexAsync();
    await es.CreateEmployeeIndexAsync();

    // ================= EMPLOYEE DATA =================
    var employeeList = await adminRepo.ListEmployee();
    Console.WriteLine($"Employee Count: {employeeList.Count}");

    // ================= ATTENDANCE DATA =================
    var attendanceList = new List<AttendanceModel>();

    foreach (var emp in employeeList)
    {
        var empAttendance = await Task.Run(() =>
            employeeRepo.GetAttendanceByEmployee(emp.EmpId)
        );

        attendanceList.AddRange(empAttendance);
    }

    var attendanceData = (from a in attendanceList
                          join e in employeeList
                          on a.EmpId equals e.EmpId
                          select new AdminReportSearchModel
                          {
                              AttendId = a.AttendId,
                              EmpId = a.EmpId,
                              EmployeeName = e.Name,
                              AttendDate = a.AttendDate,
                              AttendStatus = a.AttendStatus,
                              WorkType = a.WorkType,
                              TaskType = a.TaskType
                          }).ToList();

    // ================= CHECK ATTENDANCE =================
    var existingAttendance = await es.GetAllAsync<AdminReportSearchModel>();

    if (existingAttendance.Count == 0)
    {
        Console.WriteLine("🚀 Indexing Attendance...");
        await es.BulkIndexAttendanceAsync(attendanceData);
    }
    else
    {
        Console.WriteLine("⚠️ Attendance already indexed");
    }

    // ================= EMPLOYEE INDEX (ALWAYS RUN) =================
    var employeeIndexData = employeeList.Select(e => new EmployeeSearchIndex
    {
        EmpId = e.EmpId,
        Name = e.Name,
        Email = e.Email,
        Gender = e.Gender,
        Status = e.Status,
        Role = e.Role,
        TotalWorkingHours = 0,
        TotalDaysPresent = 0,
        LateInCount = 0,
        EarlyOutCount = 0,
        LastAttendDate = DateTime.Now
    }).ToList();

    Console.WriteLine("🚀 Indexing Employees...");
    await es.BulkIndexEmployeeAsync(employeeIndexData);

    Console.WriteLine("✅ Elasticsearch indexing completed");
}

app.Lifetime.ApplicationStarted.Register(() =>
{
    Task.Run(async () =>
    {
        try
        {
            await IndexElasticData();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Indexing Error: {ex.Message}");
        }
    });
});

app.Run();
