using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Clients.Elasticsearch.Aggregations;
using Elastic.Clients.Elasticsearch.Core.Bulk;
using Elastic.Clients.Elasticsearch.QueryDsl;
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
        private readonly string _employeeIndex = "employee_index";

        public ElasticsearchService(IOptions<ElasticsearchConfig> config)
        {
            var settings = config.Value;

            var esSettings = new ElasticsearchClientSettings(new Uri(settings.Url))
                .Authentication(new BasicAuthentication(settings.Username, settings.Password))
                .DefaultIndex(settings.DefaultIndex)
                .RequestTimeout(TimeSpan.FromSeconds(5))
                .EnableDebugMode()
                .DisableDirectStreaming(); // ✅ prevents hanging

            _client = new ElasticsearchClient(esSettings);
            _index = "attendance_index";
        }

        // ======================= GENERIC =======================

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

        // ======================= ATTENDANCE =======================

        public async Task CreateAttendanceIndexAsync()
        {
            var exists = await _client.Indices.ExistsAsync(_index);

            if (exists.Exists)
            {
                Console.WriteLine("Attendance index already exists");
                return;
            }

            var response = await _client.Indices.CreateAsync<AdminReportSearchModel>(c => c
                .Index(_index)
                .Mappings(m => m.Properties(p => p
                    .IntegerNumber(x => x.AttendId)
                    .IntegerNumber(x => x.EmpId)
                    .Text(x => x.EmployeeName)
                    .Keyword(x => x.AttendStatus)
                    .Keyword(x => x.WorkType)
                    .Keyword(x => x.TaskType)
                    .Date(x => x.AttendDate)
                ))
            );

            if (!response.IsValidResponse)
            {
                var error = response.ElasticsearchServerError?.Error?.Reason;
                throw new Exception($"Attendance index creation failed: {error}");
            }

            Console.WriteLine("✅ Attendance index created");
        }

        public async Task IndexAttendanceAsync(AdminReportSearchModel data)
        {
            Console.WriteLine($"Indexing Attendance: {data.AttendId}");

            await _client.IndexAsync(data, i => i
                .Index(_index)
                .Id(data.AttendId)
            );
        }

        public async Task<List<AdminReportSearchModel>> SearchAttendanceByName(string name)
        {
            var response = await _client.SearchAsync<AdminReportSearchModel>(s => s
                .Index(_index)
                .Size(1000)
                .Query(q => q
                    .Bool(b => b
                        .Should(
                            s => s.MatchPhrasePrefix(m => m
                                .Field(f => f.EmployeeName)
                                .Query(name)
                            ),
                            s => s.Match(m => m
                                .Field(f => f.EmployeeName)
                                .Query(name)
                                .Fuzziness(new Fuzziness("AUTO"))
                            )
                        )
                        .MinimumShouldMatch(1)
                    )
                )
            );

            var result = response.Documents
                .GroupBy(x => x.EmpId)
                .Select(g => g.First())
                .ToList();

            return result;
        }
        // ======================= EMPLOYEE =======================

        public async Task CreateEmployeeIndexAsync()
        {
            var exists = await _client.Indices.ExistsAsync(_employeeIndex);

            if (exists.Exists)
            {
                Console.WriteLine("Employee index already exists");
                return;
            }

            var response = await _client.Indices.CreateAsync<EmployeeSearchIndex>(c => c
                .Index(_employeeIndex)
                .Mappings(m => m.Properties(p => p
                    .IntegerNumber(x => x.EmpId)
                    .Text(x => x.Name)
                    .Keyword(x => x.Email)
                    .Keyword(x => x.Gender)
                    .Keyword(x => x.Status)
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

            Console.WriteLine("✅ Employee index created");
        }

        public async Task IndexEmployeeAsync(EmployeeSearchIndex data)
        {
            Console.WriteLine($"Indexing Employee: {data.EmpId}");

            await _client.IndexAsync(data, i => i
                .Index(_employeeIndex)
                .Id(data.EmpId)
            );
        }

        public async Task<List<EmployeeSearchIndex>> SearchEmployee(string name, string status)
        {
            Query query;

            if (string.IsNullOrEmpty(name) && string.IsNullOrEmpty(status))
            {
                query = new MatchAllQuery();
            }
            else if (string.IsNullOrEmpty(name))
            {
                query = new TermQuery(Infer.Field<EmployeeSearchIndex>(f => f.Status))
                {
                    Value = status
                };
            }
            else if (string.IsNullOrEmpty(status))
            {
                // 🔥 NAME ONLY (FUZZY + PREFIX)
                query = new BoolQuery
                {
                    Should = new Query[]
                    {
                        new MatchQuery(Infer.Field<EmployeeSearchIndex>(f => f.Name))
                        {
                            Query = name,
                            Fuzziness = new Fuzziness("AUTO") // 🔥 typo support
                        },
                        new MatchPhrasePrefixQuery(Infer.Field<EmployeeSearchIndex>(f => f.Name))
                        {
                            Query = name // 🔥 partial typing
                        }
                    }
                };
            }
            else
            {
                // 🔥 NAME + STATUS
                query = new BoolQuery
                {
                    Must = new Query[]
                    {
                        new BoolQuery
                        {
                            Should = new Query[]
                            {
                                new MatchQuery(Infer.Field<EmployeeSearchIndex>(f => f.Name))
                                {
                                    Query = name,
                                    Fuzziness = new Fuzziness("AUTO")
                                },
                                new MatchPhrasePrefixQuery(Infer.Field<EmployeeSearchIndex>(f => f.Name))
                                {
                                    Query = name
                                }
                            }
                        }
                    },
                    Filter = new Query[]
                    {
                        new TermQuery(Infer.Field<EmployeeSearchIndex>(f => f.Status))
                        {
                            Value = status
                        }
                    }
                };
            }

            var response = await _client.SearchAsync<EmployeeSearchIndex>(s => s
                .Index("employee_index")
                .Query(query)
            );

            return response.Documents.ToList();
        }
        public async Task BulkIndexAttendanceAsync(List<AdminReportSearchModel> data)
        {
            Console.WriteLine($"Bulk Attendance Count: {data.Count}");

            int batchSize = 1000; // 🔥 important for large data

            for (int i = 0; i < data.Count; i += batchSize)
            {
                var batch = data.Skip(i).Take(batchSize).ToList();

                var bulkRequest = new BulkRequest(_index);
                bulkRequest.Operations = new BulkOperationsCollection();

                foreach (var item in batch)
                {
                    bulkRequest.Operations.Add(new BulkIndexOperation<AdminReportSearchModel>(item)
                    {
                        Id = item.AttendId.ToString() // 🔥 UNIQUE ID (prevents duplicates)
                    });
                }

                var response = await _client.BulkAsync(bulkRequest);

                if (!response.IsValidResponse)
                {
                    Console.WriteLine("❌ Bulk Error:");
                    Console.WriteLine(response.DebugInformation);
                    throw new Exception("Attendance bulk indexing failed");
                }

                Console.WriteLine($"✅ Batch indexed: {i + batch.Count}");
            }

            Console.WriteLine("✅ All attendance data indexed successfully");
        }

        public async Task BulkIndexEmployeeAsync(List<EmployeeSearchIndex> data)
        {
            Console.WriteLine($"Bulk Employee Count: {data.Count}");

            var bulkRequest = new BulkRequest("employee_index");
            bulkRequest.Operations = new BulkOperationsCollection();

            foreach (var item in data)
            {
                bulkRequest.Operations.Add(new BulkIndexOperation<EmployeeSearchIndex>(item)
                {
                    Id = item.EmpId.ToString()  // ✅ unique ID prevents duplicates
                });
            }

            var response = await _client.BulkAsync(bulkRequest);

            if (!response.IsValidResponse)
            {
                Console.WriteLine("❌ Bulk Error:");
                Console.WriteLine(response.DebugInformation);
                throw new Exception("Employee bulk indexing failed");
            }

            Console.WriteLine("✅ Employee bulk indexing done");
        }
    }
}