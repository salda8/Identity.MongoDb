using System;
using MongoDB.Driver;
using Mongo2Go;
using Microsoft.Extensions.Options;

namespace Identity.MongoDb.Tests.Common
{
    internal static class MongoDbServerTestUtils
    {
        public static DisposableDatabase CreateDatabase() => new DisposableDatabase();

    }

        public class DisposableDatabase : IDisposable
        {
            private MongoDbRunner runner = MongoDbRunner.Start();
            public IOptions<MongoDbSettings> MongoDbSettings{get;}
            private bool _disposed;

            public DisposableDatabase()
            {
                MongoDbSettings = Options.Create(new MongoDbSettings() { ConnectionString = runner.ConnectionString, Database = Guid.NewGuid().ToString() });
            }

            public void Dispose()
            {
                if (_disposed == false && !runner.Disposed)
                {
                    runner.Dispose();
                    _disposed = true;
                }
            }
        }
    
}