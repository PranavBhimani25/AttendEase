using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Options;
using MVCAttendEase.Models;
using Repositories.Models;

namespace MVCAttendEase.Services
{
    public class ElasticsearchService
    {
        private readonly ElasticsearchClient _client;
        private readonly string _index;

        public ElasticsearchService(IOptions<ElasticsearchConfig> config)
        {
            var settings = config.Value;

            var esSettings = new ElasticsearchClientSettings(new Uri(settings.Url))
                .Authentication(new BasicAuthentication(settings.Username, settings.Password))
                .DefaultIndex(settings.DefaultIndex);

            _client = new ElasticsearchClient(esSettings);
            _index = settings.DefaultIndex;
        }

        public async Task CreateIndexAsync(string indexName)
        {
             var exists = await _client.Indices.ExistsAsync(indexName);

            if (exists.Exists)
            {
                Console.WriteLine("Index already exists");
                return;
            }

            var response = await _client.Indices.CreateAsync(indexName);

            if (!response.IsValidResponse)
            {
                var error = response.ElasticsearchServerError?.Error?.Reason;
                throw new Exception($"Index creation failed: {error}");
            }

            Console.WriteLine("Index created successfully");
        }

        public async Task IndexAsync<T>(T data)
        {
            await _client.IndexAsync(data, i => i.Index(_index));
        }

        public async Task<List<T>> GetAllAsync<T>()
        {
            var response = await _client.SearchAsync<T>(s => s
                .Index(_index)
                .Size(1000)
            );

            return response.Documents.ToList();
        }

        

        // ✅ Create Index with Mapping (for AdminReportSearchModel)
        public async Task CreateAttendanceIndexAsync()
        {
            var exists = await _client.Indices.ExistsAsync(_index);

            if (exists.Exists)
            {
                Console.WriteLine("Index already exists");
                return;
            }

            var response = await _client.Indices.CreateAsync<AdminReportSearchModel>(c => c
                .Index("attendance_index")
                .Mappings(m => m.Properties(p => p
                    .IntegerNumber(x => x.AttendId)
                    .IntegerNumber(x => x.EmpId)
                    .Text(x => x.EmployeeName) // 🔥 searchable
                    .Keyword(x => x.AttendStatus)
                    .Keyword(x => x.WorkType)
                    .Keyword(x => x.TaskType)
                    .Date(x => x.AttendDate)
                ))
            );

            if (!response.IsValidResponse)
            {
                var error = response.ElasticsearchServerError?.Error?.Reason;
                throw new Exception($"Index creation failed: {error}");
            }

            Console.WriteLine("Attendance index created successfully");
        }


        // ✅ Index Attendance (NO DUPLICATE)
        public async Task IndexAttendanceAsync(AdminReportSearchModel data)
        {
            await _client.IndexAsync(data, i => i
                .Index("attendance_index")
                .Id(data.AttendId) // 🔥 prevents duplicate
            );
        }


        // ✅ Search by Employee Name (Admin Report)
        public async Task<List<AdminReportSearchModel>> SearchAttendanceByName(string name)
        {
            var response = await _client.SearchAsync<AdminReportSearchModel>(s => s
                .Index("attendance_index")
                .Query(q => q
                    .Match(m => m
                        .Field(f => f.EmployeeName)
                        .Query(name)
                    )
                )
            );

            return response.Documents.ToList();
        }

        // ======================= 🔥 EMPLOYEE SEARCH ADD START =======================

// ✅ Create Employee Index with Mapping
public async Task CreateEmployeeIndexAsync()
{
    var exists = await _client.Indices.ExistsAsync(_index);

    if (exists.Exists)
    {
        Console.WriteLine("Employee index already exists");
        return;
    }

    var response = await _client.Indices.CreateAsync<EmployeeSearchIndex>(c => c
        .Index("employee_index")
        .Mappings(m => m.Properties(p => p
            .IntegerNumber(x => x.EmpId)
            .Text(x => x.Name) // 🔥 search
            .Keyword(x => x.Email)
            .Keyword(x => x.Gender)
            .Keyword(x => x.Status) // 🔥 filter
            .Keyword(x => x.Role)

            .IntegerNumber(x => x.TotalWorkingHours)
            .IntegerNumber(x => x.TotalDaysPresent)
            .IntegerNumber(x => x.LateInCount)
            .IntegerNumber(x => x.EarlyOutCount)

            .Date(x => x.LastAttendDate)
        ))
    );

    if (!response.IsValidResponse)
    {
        var error = response.ElasticsearchServerError?.Error?.Reason;
        throw new Exception($"Employee index creation failed: {error}");
    }

    Console.WriteLine("Employee index created successfully");
}


// ✅ Index Employee (NO DUPLICATE)
public async Task IndexEmployeeAsync(EmployeeSearchIndex data)
{
    await _client.IndexAsync(data, i => i
        .Index("employee_index")
        .Id(data.EmpId) // 🔥 unique
    );
}


// ✅ SEARCH BY NAME + STATUS 🔥
public async Task<List<EmployeeSearchIndex>> SearchEmployee(string name, string status)
{
    var response = await _client.SearchAsync<EmployeeSearchIndex>(s => s
        .Index("employee_index")
        .Query(q => q
            .Bool(b => b
                .Must(
                    m => m.Wildcard(w => w
                        .Field(f => f.Name.Suffix("keyword"))
                        .Value(name + "*") // 🔥 name search
                    )
                )
                .Filter(
                    f => f.Term(t => t
                        .Field(x => x.Status)
                        .Value(status) // 🔥 Active / Inactive
                    )
                )
            )
        )
    );

    return response.Documents.ToList();
}

// ======================= 🔥 EMPLOYEE SEARCH ADD END =======================

    }
}