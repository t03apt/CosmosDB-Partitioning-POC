using System;
using System.Configuration;
using System.Threading.Tasks;
using GenFu;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Extensions.Logging;
using Generator = GenFu.GenFu;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace PartitioningPOC
{
    internal class Application
    {
        private readonly string _databaseName = "samples";
        private readonly string _collectionName = "partitioning-poc";

        private readonly string _endpointUrl = ConfigurationManager.AppSettings["EndPointUrl"] ?? "https://localhost:8081";
        private readonly string _authorizationKey = ConfigurationManager.AppSettings["AuthorizationKey"] ?? "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        private readonly ILogger _logger;
        private DocumentClient _client;

        public Application(ILogger<Application> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Run()
        {
            using (_client = new DocumentClient(
                new Uri(_endpointUrl), _authorizationKey,
                new ConnectionPolicy
                {
                    EnableEndpointDiscovery = false
                }))
            {
                await EnsureDatabaseExists();
                await EnsureCollectionExists();

                var repository = new UserRepository(_client, _databaseName, _collectionName);

                var tenantId = Guid.NewGuid().ToString();
                Generator.Configure<UserDto>()
                    .Fill(p => p.Id, _ => Guid.NewGuid().ToString())
                    .Fill(p => p.TenantId, tenantId)
                    .Fill(p => p.Email).AsEmailAddress()
                    .Fill(p => p.PartitionKey, (string)null);

                var users = Generator.ListOf<UserDto>(2);

                _logger.LogInformation(nameof(repository.GetAsync));
                foreach (var userInfo in users)
                {
                    await repository.CreateAsync(userInfo);
                    var user = await repository.GetAsync(userInfo.TenantId, userInfo.Id);
                    _logger.LogInformation("TenantId:{TenantId} Id:{UserId} Email:{Email}", user.TenantId, user.Id, user.Email);
                }

                _logger.LogInformation(nameof(repository.GetAllAsync));
                foreach (var user in await repository.GetAllAsync(tenantId))
                {
                    _logger.LogInformation("TenantId:{TenantId} Id:{UserId} Email:{Email}", user.TenantId, user.Id, user.Email);
                }
            }
        }

        private async Task EnsureDatabaseExists()
        {
            await _client.CreateDatabaseIfNotExistsAsync(
                new Database
                {
                    Id = _databaseName
                });
        }

        private async Task<DocumentCollection> EnsureCollectionExists()
        {
            // Define "deviceId" as the partition key
            // Set throughput to the minimum value of 10,100 RU/s

            var collectionDefinition = new DocumentCollection
            {
                Id = _collectionName,
                PartitionKey = new PartitionKeyDefinition
                {
                    Paths =
                    {
                        "/partitionKey"
                    }
                }
            };

            DocumentCollection partitionedCollection = await _client.CreateDocumentCollectionIfNotExistsAsync(
                UriFactory.CreateDatabaseUri(_databaseName),
                collectionDefinition,
                new RequestOptions { OfferThroughput = 400 });

            return partitionedCollection;
        }
    }
}
