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

async Task IndexElasticData()
{
    using var scope = app.Services.CreateScope();

    var es = scope.ServiceProvider.GetRequiredService<ElasticsearchService>();
    var adminRepo = scope.ServiceProvider.GetRequiredService<IAdminInterface>();
    var employeeRepo = scope.ServiceProvider.GetRequiredService<IEmployeeInterface>();

    // ✅ Create Index
    await es.CreateAttendanceIndexAsync();
    await es.CreateEmployeeIndexAsync();

    // ================= EMPLOYEE DATA =================
    var employeeList = await adminRepo.ListEmployee();

    // ================= ATTENDANCE DATA =================
    var attendanceList = new List<AttendanceModel>();

    foreach (var emp in employeeList)
    {
        var empAttendance = employeeRepo.GetAttendanceByEmployee(emp.EmpId);
        attendanceList.AddRange(empAttendance);
    }

    // ================= ATTENDANCE INDEX =================
    var attendanceData = from a in attendanceList
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
                         };

    foreach (var item in attendanceData)
    {
        await es.IndexAttendanceAsync(item);
    }

    // ================= EMPLOYEE INDEX =================
    var employeeIndexData = employeeList.Select(e => new EmployeeSearchIndex
    {
        EmpId = e.EmpId,
        Name = e.Name,
        Email = e.Email,
        Gender = e.Gender,
        Status = e.Status,
        Role = e.Role,

        // You can improve this later
        TotalWorkingHours = 0,
        TotalDaysPresent = 0,
        LateInCount = 0,
        EarlyOutCount = 0,
        LastAttendDate = DateTime.Now
    });

    foreach (var emp in employeeIndexData)
    {
        await es.IndexEmployeeAsync(emp);
    }

    Console.WriteLine("✅ Elasticsearch indexing completed");
}

 

app.Run();
