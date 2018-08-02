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
using FluentAssertions;
using FluentAssertions.Extensions;

namespace Identity.MongoDb.Tests
{
    public class MongoUserStoreTests : IDisposable
    {
        private DisposableDatabase disposableDatabase;
        private IOptions<MongoDbSettings> options;

        public MongoUserStoreTests()
        {
            disposableDatabase = new DisposableDatabase();
            options = disposableDatabase.MongoDbSettings;
        }

        [Fact]
        public async Task MongoUserStore_ShouldPutThingsIntoUsersCollectionByDefault()
        {
            var user = new MongoIdentityUser(TestUtils.RandomString(10));


            using (var store = new MongoUserStore<MongoIdentityUser>(options))
            {

                var result = await store.CreateAsync(user, CancellationToken.None);

                result.Succeeded.Should().BeTrue();
                var count = await store.GetUserCountAsync();
                count.Should().Be(1);
              

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
                retrievedUser.Should().NotBeNull();
                retrievedUser.UserName.Should().Be(user.UserName);
                retrievedUser.NormalizedUserName.Should().Be(user.NormalizedUserName);

            }
        }


        public void Dispose()
        {
            disposableDatabase.Dispose();
        }
    }
}