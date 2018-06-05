using System.Collections.Generic;
using System.Threading.Tasks;

namespace PartitioningPOC
{
    internal interface IUserRepository
    {
        Task<UserDto> CreateAsync(UserDto model);
        Task<UserDto> GetAsync(string tenantId, string id);
        Task<IEnumerable<UserDto>> GetAllAsync(string tenantId);
    }
}