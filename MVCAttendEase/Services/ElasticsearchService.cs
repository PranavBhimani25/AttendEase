using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Options;
using MVCAttendEase.Models;
using Repositories.Models;
using Elastic.Clients.Elasticsearch.QueryDsl;

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
            var response = await _client.IndexAsync(data, i => i.Index(_index));

            if (!response.IsValidResponse)
                throw new Exception($"Index failed: {response.DebugInformation}");
        }

        public async Task IndexAttendanceAsync(EmployeeWork attendance)
        {
            var response = await _client.IndexAsync(attendance, i => i
                .Index(_index)
                .Id(attendance.AttendId.ToString())
            );

            if (!response.IsValidResponse)
            {
                throw new Exception($"Attendance index failed: {response.DebugInformation}");
            }
        }

        public async Task IndexWithIdAsync<T>(string indexName, string documentId, T data)
        {
            var response = await _client.IndexAsync(data, i => i
                .Index(indexName)
                .Id(documentId)
            );

            if (!response.IsValidResponse)
            {
                throw new Exception($"Index failed for {indexName}/{documentId}: {response.DebugInformation}");
            }
        }



        public async Task<List<T>> GetAllAsync<T>()
        {
            var response = await _client.SearchAsync<T>(s => s
                .Index(_index)
                .Size(1000)
            );

            return response.Documents.ToList();
        }


        public async Task<List<EmployeeWork>> SearchAsync(string searchText)
        {
            var response = await _client.SearchAsync<EmployeeWork>(s => s
                .Index("employee_attendance")
                .Query(q => q
                    .MultiMatch(m => m
                        .Fields(new[]
                        {
                    "c_worktype",
                    "c_tasktype",
                    "c_attendstatus"
                        })
                        .Query(searchText)
                        .Fuzziness(new Elastic.Clients.Elasticsearch.Fuzziness("AUTO"))
                    )
                )
                .Size(100)
            );

            // 🔥 IMPORTANT FIX
            if (!response.IsValidResponse)
            {
                throw new Exception($"Elasticsearch error: {response.DebugInformation}");
            }

            // 🔥 NULL SAFE
            return response.Documents?.ToList() ?? new List<EmployeeWork>();
        }

        public async Task<HashSet<long>> SearchAttendanceIdsAsync(string searchText, long? empId = null)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return new HashSet<long>();
            }

            var mustQueries = new List<Query>();

            if (empId.HasValue)
            {
                mustQueries.Add(new TermQuery("c_empid")
                {
                    Value = FieldValue.Long(empId.Value)
                });
            }

            mustQueries.Add(new MultiMatchQuery
            {
                Fields = new[] { "c_worktype", "c_tasktype", "c_attendstatus" },
                Query = searchText,
                Fuzziness = new Fuzziness("AUTO")
            });

            var response = await _client.SearchAsync<EmployeeWork>(s => s
                .Index(_index)
                .Query(q => q
                    .Bool(b => b
                        .Must(mustQueries.ToArray())
                    )
                )
                .Size(10000)
            );

            if (!response.IsValidResponse)
            {
                throw new Exception($"Elasticsearch error: {response.DebugInformation}");
            }

            return response.Documents
                .Where(d => d.AttendId > 0)
                .Select(d => d.AttendId)
                .ToHashSet();
        }


        public async Task<List<EmployeeWork>> SearchWithFilterAsync(
           string? searchText,
           DateTime? fromDate,
           DateTime? toDate,
           string? status,
           string? workType,
           long? empId = null)
        {
            var mustQueries = new List<Query>();

            // ── Employee ID filter ────────────────────────────────────────────
            if (empId.HasValue)
            {
                mustQueries.Add(new TermQuery("c_empid")
                {
                    Value = FieldValue.Long(empId.Value)
                });
            }

            // ── Full text search across text fields ───────────────────────────
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                mustQueries.Add(new MultiMatchQuery
                {
                    Fields = new[] { "c_tasktype", "c_worktype", "c_attendstatus" },
                    Query = searchText,
                    Fuzziness = new Fuzziness("AUTO")
                });
            }

            // ── Date range ────────────────────────────────────────────────────
            if (fromDate.HasValue || toDate.HasValue)
            {
                mustQueries.Add(new DateRangeQuery("c_attenddate")
                {
                    Gte = fromDate,
                    Lte = toDate
                });
            }

            // ── Attendance status ─────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(status))
            {
                mustQueries.Add(new MatchQuery("c_attendstatus")
                {
                    Query = status
                });
            }

            // ── Work type ─────────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(workType))
            {
                mustQueries.Add(new MatchQuery("c_worktype")
                {
                    Query = workType
                });
            }

            // ── Build final query ─────────────────────────────────────────────
            SearchResponse<EmployeeWork> response;

if (mustQueries.Any())
{
    response = await _client.SearchAsync<EmployeeWork>(s => s
        .Index(_index)
        .Query(q => q
            .Bool(b => b
                .Must(mustQueries.ToArray())
            )
        )
        .Size(10000)
    );
}
else
{
    response = await _client.SearchAsync<EmployeeWork>(s => s
        .Index(_index)
        .Query(q => q
            .MatchAll(m => m
            .QueryName("match_all")))
        .Size(10000)
    );
}

if (!response.IsValidResponse)
    throw new Exception($"Elasticsearch error: {response.DebugInformation}");

return response.Documents?.ToList() ?? new List<EmployeeWork>();
        }
    }
}