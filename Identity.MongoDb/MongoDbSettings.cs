using IdentityServer4.MongoDB.Configuration;

namespace Identity.MongoDb
{
    public class MongoDbSettings : MongoDBConfiguration
    {
        public override string ToString()
        {
            return $"{ConnectionString}/{Database}";
        }
    }
}