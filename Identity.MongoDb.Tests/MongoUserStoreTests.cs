﻿using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Identity.MongoDb.Tests.Common;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Xunit;

namespace Identity.MongoDb.Tests
{
    public class MongoUserStoreTests
    {
        [Fact]
        public async Task MongoUserStore_ShouldPutThingsIntoUsersCollectionByDefault()
        {
            var user = new MongoIdentityUser(TestUtils.RandomString(10));
            using (var dbProvider = MongoDbServerTestUtils.CreateDatabase())
            {
                var options = Options.Create(new MongoDbSettings(){ConnectionString="mongodb://localhost:27017", Database=Guid.NewGuid().ToString() });
                using (var store = new MongoUserStore<MongoIdentityUser>(options))
                {

                    // ACT
                    var result = await store.CreateAsync(user, CancellationToken.None);

                    // ASSERT
                    Assert.True(result.Succeeded);
                    var collections = await (await dbProvider.Database.ListCollectionsAsync()).ToListAsync();
                    var collectionExists = collections.Any(x => x["name"].ToString().Equals("users", StringComparison.Ordinal));
                    Assert.True(collectionExists, "Default collection name should not be changed from the initial collection name ('users') since it will cause breaking change to current users");
                }
            }
        }
    }
}