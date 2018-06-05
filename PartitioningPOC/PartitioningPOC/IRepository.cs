using System.Threading.Tasks;

namespace PartitioningPOC
{
    internal interface IRepository<in TKey, TModel>
        where TModel : class
    {
        Task<TModel> CreateAsync(TModel model);
        Task<TModel> GetAsync(string partitionKey, TKey id);
        //Task DeleteAsync(TModel model);
        //Task<TModel> UpdateAsync(TModel model);
    }
}
