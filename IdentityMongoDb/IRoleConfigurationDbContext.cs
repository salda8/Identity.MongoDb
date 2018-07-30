using IdentityServer4.MongoDB.Interfaces;
using System.Linq;
using System.Threading.Tasks;

namespace Identity.MongoDb
{
    public interface IRoleConfigurationDbContext : IConfigurationDbContext
    {
        IQueryable<MongoIdentityRole> Roles { get; }

        Task AddRolesAsync(MongoIdentityRole entity);
    }
}