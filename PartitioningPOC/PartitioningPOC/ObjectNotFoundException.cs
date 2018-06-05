using System;

namespace PartitioningPOC
{
    public class ObjectNotFoundException : Exception
    {
        public ObjectNotFoundException(string partitionKey, string id)
            : this(partitionKey, id, null)
        {
        }

        public ObjectNotFoundException(string partitionKey, string id, string message)
            : this(partitionKey, id, message, null)
        {
        }

        public ObjectNotFoundException(string partitionKey, string id, string message, Exception innerException)
            : base(message, innerException)
        {
            Id = id;
            PartitionKey = partitionKey;
        }

        public string Id { get; }
        public string PartitionKey { get; }
    }
}
