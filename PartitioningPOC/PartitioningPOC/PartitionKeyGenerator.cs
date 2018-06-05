using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PartitioningPOC
{
    internal class PartitionKeyGenerator
    {
        private readonly MD5 _md5;

        public PartitionKeyGenerator()
        {
            _md5 = MD5.Create();
        }

        public string Create(string prefix, string id, int numberOfPartitions)
        {
            var hashedValue = _md5.ComputeHash(Encoding.UTF8.GetBytes(id));
            var asInt = BitConverter.ToInt32(hashedValue, 0);
            asInt = asInt == int.MinValue ? asInt + 1 : asInt;
            return ComposePartitionKey(prefix, Math.Abs(asInt) % numberOfPartitions);
        }

        public IEnumerable<string> CreateAll(string prefix, int numberOfPartitions)
        {
            return Enumerable.Range(0, numberOfPartitions).Select(n => ComposePartitionKey(prefix, n));
        }

        private static string ComposePartitionKey(string prefix, int postfix)
        {
            return $"{prefix}-{postfix}";
        }
    }
}
