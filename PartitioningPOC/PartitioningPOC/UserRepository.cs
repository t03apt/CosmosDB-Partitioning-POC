using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;

namespace PartitioningPOC
{
    internal class UserRepository : IRepository<string, UserDto>
    {
        private const int NumberOfPartitions = 1000;

        private readonly DocumentClient _client;
        private readonly string _databaseId;
        private readonly string _collectionId;
        private readonly PartitionKeyGenerator _partitionKeyGenerator;
        private readonly Uri _collectionUri;

        public UserRepository(DocumentClient client, string databaseId, string collectionId)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _databaseId = databaseId ?? throw new ArgumentNullException(nameof(databaseId));
            _collectionId = collectionId ?? throw new ArgumentNullException(nameof(collectionId));

            _collectionUri = UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId);
            _partitionKeyGenerator = new PartitionKeyGenerator();
        }


        public async Task<UserDto> CreateAsync(UserDto model)
        {
            model.PartitionKey = GetPartitionKey(model);
            Document user = await _client.CreateDocumentAsync(UriFactory.CreateDocumentCollectionUri(_databaseId, _collectionId), model);
            return (UserDto)(dynamic)user;
        }

        public async Task<UserDto> GetAsync(string tenantId, string id)
        {
            var partitionKey = GetPartitionKey(tenantId, id);
            try
            {
                UserDto user = await _client.ReadDocumentAsync<UserDto>(
                    UriFactory.CreateDocumentUri(_databaseId, _collectionId, id),
                    new RequestOptions
                    {
                        PartitionKey = new PartitionKey(partitionKey)
                    });
                return user;
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    throw new ObjectNotFoundException(partitionKey, id);
                }

                throw;
            }
        }

        public async Task<IEnumerable<UserDto>> GetAllAsync(string tenantId)
        {
            var query = _client.CreateDocumentQuery<UserDto>(
                _collectionUri,
                new FeedOptions
                {
                    MaxItemCount = -1,
                    EnableCrossPartitionQuery = true
                })
                .Where(user => user.TenantId == tenantId)
                .AsDocumentQuery();

            var results = new List<UserDto>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<UserDto>());
            }

            return results;
        }

        private string GetPartitionKey(UserDto user) => GetPartitionKey(user.TenantId, user.Id);

        private string GetPartitionKey(string tenantId, string userId) => _partitionKeyGenerator.Create(tenantId, userId, NumberOfPartitions);
    }
}
