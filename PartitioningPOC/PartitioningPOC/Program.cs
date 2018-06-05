using System;
using System.Configuration;
using System.Threading.Tasks;
using GenFu;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Generator = GenFu.GenFu;

namespace PartitioningPOC
{
    class Program
    {
        private readonly string _databaseName = "samples";
        private readonly string _collectionName = "partitioning-poc";

        private readonly string _endpointUrl = ConfigurationManager.AppSettings["EndPointUrl"] ?? "https://localhost:8081";
        private readonly string _authorizationKey = ConfigurationManager.AppSettings["AuthorizationKey"] ?? "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";

        private DocumentClient _client;

        private static async Task Main(string[] args)
        {
            try
            {
                var app = new Program();
                await app.Run();
            }
            catch (Exception e)
            {
                LogException(e);
            }
            finally
            {
                Console.WriteLine("End of demo, press any key to exit.");
                Console.ReadKey();
            }
        }

        private async Task Run()
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

                Console.WriteLine(nameof(repository.GetAsync));
                foreach (var userInfo in users)
                {
                    await repository.CreateAsync(userInfo);
                    var user = await repository.GetAsync(userInfo.TenantId, userInfo.Id);
                    Console.WriteLine($"TenantId:{user.TenantId} Id:{user.Id} Email:{user.Email}");
                }

                Console.WriteLine(nameof(repository.GetAllAsync));
                foreach (var user in await repository.GetAllAsync(tenantId))
                {
                    Console.WriteLine($"TenantId:{user.TenantId} Id:{user.Id} Email:{user.Email}");
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

        private static void LogException(Exception e)
        {
            ConsoleColor color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;

            if (e is DocumentClientException)
            {
                DocumentClientException de = (DocumentClientException)e;
                Exception baseException = de.GetBaseException();
                Console.WriteLine("{0} error occurred: {1}, Message: {2}", de.StatusCode, de.Message, baseException.Message);
            }
            else
            {
                Exception baseException = e.GetBaseException();
                Console.WriteLine("Error: {0}, Message: {1}", e.Message, baseException.Message);
            }

            Console.ForegroundColor = color;
        }
    }
}
