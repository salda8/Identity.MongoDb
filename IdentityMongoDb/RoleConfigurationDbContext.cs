using IdentityServer4.MongoDB.DbContexts;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using System.Linq;
using System.Threading.Tasks;

namespace Identity.MongoDb
{
    public class RoleConfigurationDbContext : ConfigurationDbContext, IRoleConfigurationDbContext
    {
        private const string RolesTableName = "Roles";
        private readonly IMongoCollection<MongoIdentityRole> roles;

        public RoleConfigurationDbContext(IOptions<MongoDbSettings> settings)
            : base(settings)
        {
            roles = Database.GetCollection<MongoIdentityRole>(RolesTableName);
        }

        public IQueryable<MongoIdentityRole> Roles
        {
            get { return roles.AsQueryable(); }
        }

        public async Task AddRolesAsync(MongoIdentityRole entity)
        {
            await roles.InsertOneAsync(entity);
        }
    }
}