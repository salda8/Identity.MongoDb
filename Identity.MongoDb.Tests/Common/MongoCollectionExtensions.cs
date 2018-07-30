using System.Threading.Tasks;
using MongoDB.Driver;

namespace Identity.MongoDb.Tests.Common
{
    public static class MongoCollectionExtensions
    {
        public static Task<TUser> FindByIdAsync<TUser>(this IMongoCollection<TUser> collection, string id)
            where TUser : MongoIdentityUser => 
            collection.Find(Builders<TUser>.Filter.Eq(x => x.Id, id)).FirstOrDefaultAsync();
    }
}