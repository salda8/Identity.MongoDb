using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Identity.MongoDb.Tests.Common;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Xunit;
using static Identity.MongoDb.Tests.MongoIdentityUserTests;

namespace Identity.MongoDb.Tests
{
    public class MongoUserStoreTests
    {
        [Fact]
        public async Task MongoUserStore_ShouldPutThingsIntoUsersCollectionByDefault()
        {
            var user = new MyIdentityUser(TestUtils.RandomString(10));
            using (var dbProvider = MongoDbServerTestUtils.CreateDatabase())
            {
                var options = Options.Create(new MongoDbSettings(){ConnectionString="mongodb://localhost:27017", Database=Guid.NewGuid().ToString() });
                using (var store = new MongoUserStore<MyIdentityUser>(options))
                {

                    // ACT
                    var result = await store.CreateAsync(user, CancellationToken.None);

                    // ASSERT
                    Assert.True(result.Succeeded);
                   
                    Assert.Equal(1,await store.GetUserCountAsync());
               
                }
            }
        }
    }
}