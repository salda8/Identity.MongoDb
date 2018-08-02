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
using Mongo2Go;
using AutoFixture;

namespace Identity.MongoDb.Tests
{
    public class MongoIdentityRoleTests : IDisposable
    {
        private DisposableDatabase disposableDatabase;
        private IOptions<MongoDbSettings> options;
        private Fixture fixture = new Fixture();

        public MongoIdentityRoleTests()
        {
            disposableDatabase = new DisposableDatabase();
            options = disposableDatabase.MongoDbSettings;
        }

        [Fact]
        public async Task MongoIdentityRole_CanBeSavedAndRetrievedAndDeleted()
        {

            using (var store = new MongoRoleClaimStore<MongoIdentityRole>(options, null, null, null))
            {
                var mongoRole = new MongoIdentityRole(fixture.Create<string>());
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

            using (var store = new MongoRoleClaimStore<MongoIdentityRole>(options, null, null, null))
            {
                var mongoRole = new MongoIdentityRole(fixture.Create<string>());
                var createOne = await store.CreateAsync(mongoRole, CancellationToken.None);
                var retrieveOne = await store.FindByNameAsync(mongoRole.Name, CancellationToken.None);
                Assert.NotNull(retrieveOne);
                var claim = new Claim(ClaimTypes.Role, fixture.Create<string>());
                await store.AddClaimAsync(retrieveOne, claim);
                var retrieveOneAgain = await store.FindByNameAsync(mongoRole.Name, CancellationToken.None);
                Assert.Single(retrieveOneAgain.Claims);

                retrieveOneAgain = await store.FindByIdAsync(retrieveOneAgain.Id, CancellationToken.None);
                await store.RemoveClaimAsync(retrieveOneAgain, retrieveOneAgain.Claims.Single().ToClaim());
                retrieveOneAgain = await store.FindByIdAsync(retrieveOneAgain.Id, CancellationToken.None);
                Assert.Empty(retrieveOneAgain.Claims);


            }

        }

        public void Dispose()
        {
            disposableDatabase.Dispose();
        }
    }
}