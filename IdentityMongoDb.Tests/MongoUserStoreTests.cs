using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Identity.MongoDb.Tests.Common;
using Microsoft.Extensions.Options;
using Xunit;
using static Identity.MongoDb.Tests.MongoIdentityUserTests;
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
            var user = new MyIdentityUser(TestUtils.RandomString(10));
            using (var dbProvider = MongoDbServerTestUtils.CreateDatabase())
            {

                using (var store = new MongoUserStore<MyIdentityUser>(options))
                {

                    // ACT
                    var result = await store.CreateAsync(user, CancellationToken.None);

                    // ASSERT
                    Assert.True(result.Succeeded);

                    Assert.Equal(1, await store.GetUserCountAsync());

                }
            }
        }

        public void Dispose(){
            runner.Dispose();
        }
    }
}