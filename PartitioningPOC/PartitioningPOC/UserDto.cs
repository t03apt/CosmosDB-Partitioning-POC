using Newtonsoft.Json;

namespace PartitioningPOC
{
    public class UserDto
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("tenantId")]
        public string TenantId { get; set; }

        [JsonProperty("partitionKey")]
        public string PartitionKey { get; set; }

        [JsonProperty("email")]
        public string Email { get; set; }
    }
}
