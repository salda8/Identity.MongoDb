using System;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Identity.MongoDb.Models;
using Identity.MongoDb.Tests.Common;
using Microsoft.AspNetCore.Identity;
using Xunit;
using Identity.MongoDb;
using Microsoft.Extensions.Options;

namespace Identity.MongoDb.Tests
{
    public class MongoIdentityRoleTests
    {
        [Fact]
        public async Task MongoIdentityRole_CanBeSavedAndRetrievedAndDeleted()
        {
            var options = Options.Create(new MongoDbSettings() { ConnectionString = "mongodb://localhost:27017", Database = Guid.NewGuid().ToString() });
            using (var store = new MongoRoleClaimStore<MongoIdentityRole>(options, null, null, null))
            {
                var mongoRole = new MongoIdentityRole("godRole");
                var createOne = await store.CreateAsync(mongoRole, CancellationToken.None);
                var retrieveOne = await store.FindByNameAsync(mongoRole.Name, CancellationToken.None);
                Assert.NotNull(retrieveOne);
                await store.DeleteAsync(retrieveOne);
                retrieveOne = await store.FindByNameAsync(mongoRole.Name, CancellationToken.None);
                Assert.Null(retrieveOne);

            }

        }
        [Fact]
        public async Task MongoIdentityRole_CanAddAndRetrieveAndRemoveClaims()
        {
            var options = Options.Create(new MongoDbSettings() { ConnectionString = "mongodb://localhost:27017", Database = Guid.NewGuid().ToString() });
            using (var store = new MongoRoleClaimStore<MongoIdentityRole>(options, null, null, null))
            {
                var mongoRole = new MongoIdentityRole("godRole");
                var createOne = await store.CreateAsync(mongoRole, CancellationToken.None);
                var retrieveOne = await store.FindByNameAsync(mongoRole.Name, CancellationToken.None);
                Assert.NotNull(retrieveOne);
                var claim = new Claim(ClaimTypes.Role, "god");
                await store.AddClaimAsync(retrieveOne, claim);
                var retrieveOneAgain = await store.FindByNameAsync(mongoRole.Name, CancellationToken.None);
                Assert.Single(retrieveOneAgain.Claims);

                retrieveOneAgain = await store.FindByIdAsync(retrieveOneAgain.Id, CancellationToken.None);
                await store.RemoveClaimAsync(retrieveOneAgain, retrieveOneAgain.Claims.Single().ToClaim());
                retrieveOneAgain = await store.FindByIdAsync(retrieveOneAgain.Id, CancellationToken.None);
                Assert.Empty(retrieveOneAgain.Claims);


            }

        }
    }
}