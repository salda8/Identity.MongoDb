using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Identity.MongoDb.Tests")]
namespace Identity.MongoDb
{
    internal static class Constants
    {
        public const string DefaultCollectionName = "users";
    }
}