using System.Threading;
using System.Threading.Tasks;
using Identity.MongoDb.Tests.Common;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Xunit;

namespace Identity.MongoDb.Tests
{
    public class UserStoreTests
    {
        [Fact]
        public async Task CreateAsync_ShouldCreateUser()
        {
            // ARRANGE
            MongoIdentityUser user;
            var options = Options.Create(new MongoDbSettings() { ConnectionString = "mongodb://localhost:27017", Database = System.Guid.NewGuid().ToString() });

            using (var userStore = new MongoUserStore<MongoIdentityUser>(options) as IUserStore<MongoIdentityUser>)
            {
                user = new MongoIdentityUser(TestUtils.RandomString(10));

                // ACT
                await userStore.CreateAsync(user, CancellationToken.None);


                // ASSERT
                var retrievedUser = await userStore.FindByIdAsync(user.Id, CancellationToken.None);

                Assert.NotNull(retrievedUser);
                Assert.Equal(user.UserName, retrievedUser.UserName);
                Assert.Equal(user.NormalizedUserName, retrievedUser.NormalizedUserName);
            }
        }
    }
}
