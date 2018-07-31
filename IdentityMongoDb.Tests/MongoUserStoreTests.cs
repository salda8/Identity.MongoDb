using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Identity.MongoDb.Tests.Common;
using Microsoft.Extensions.Options;
using Identity.MongoDb.Models;
using Identity.MongoDb;
using Microsoft.AspNetCore.Identity;
using Xunit;
using Mongo2Go;

namespace Identity.MongoDb.Tests
{
    public class MongoUserStoreTests : IDisposable
    {
        private MongoDbRunner runner;
        private IOptions<MongoDbSettings> options;

        public MongoUserStoreTests()
        {
            runner = MongoDbRunner.Start();
            options = Options.Create(new MongoDbSettings() { ConnectionString = runner.ConnectionString, Database = Guid.NewGuid().ToString() });
        }

        [Fact]
        public async Task MongoUserStore_ShouldPutThingsIntoUsersCollectionByDefault()
        {
            var user = new MongoIdentityUser(TestUtils.RandomString(10));


            using (var store = new MongoUserStore<MongoIdentityUser>(options))
            {

                // ACT
                var result = await store.CreateAsync(user, CancellationToken.None);

                // ASSERT
                Assert.True(result.Succeeded);

                Assert.Equal(1, await store.GetUserCountAsync());

            }
        }

        [Fact]
        public async Task CreateAsync_ShouldCreateUser()
        {
            // ARRANGE
            MongoIdentityUser user;

            using (var userStore = new MongoUserStore<MongoIdentityUser>(options) as IUserStore<MongoIdentityUser>)
            {
                user = new MongoIdentityUser(TestUtils.RandomString(10));
                await userStore.CreateAsync(user, CancellationToken.None);
                var retrievedUser = await userStore.FindByIdAsync(user.Id, CancellationToken.None);
                Assert.NotNull(retrievedUser);
                Assert.Equal(user.UserName, retrievedUser.UserName);
                Assert.Equal(user.NormalizedUserName, retrievedUser.NormalizedUserName);
            }
        }


        public void Dispose()
        {
            runner.Dispose();
        }
    }
}