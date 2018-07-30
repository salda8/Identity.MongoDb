using System.Threading;
using System.Threading.Tasks;
using Identity.MongoDb.Tests.Common;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace Identity.MongoDb.Tests
{
    public class UserStoreTests
    {
        [Fact]
        public async Task CreateAsync_ShouldCreateUser()
        {
            // ARRANGE
            using (var dbProvider = MongoDbServerTestUtils.CreateDatabase())
            {
                MongoIdentityUser user;
                using (var userStore = new MongoUserStore<MongoIdentityUser>(dbProvider.Database) as IUserStore<MongoIdentityUser>)
                {
                    user = new MongoIdentityUser(TestUtils.RandomString(10));

                    // ACT
                    await userStore.CreateAsync(user, CancellationToken.None);
                }

                // ASSERT
                var collection = dbProvider.Database.GetDefaultCollection();
                var retrievedUser = await collection.FindByIdAsync(user.Id).ConfigureAwait(false);

                Assert.NotNull(retrievedUser);
                Assert.Equal(user.UserName, retrievedUser.UserName);
                Assert.Equal(user.NormalizedUserName, retrievedUser.NormalizedUserName);
            }
        }
    }
}