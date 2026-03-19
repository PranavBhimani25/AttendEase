using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using Microsoft.Extensions.Options;
using MVCAttendEase.Models;

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
    }
}