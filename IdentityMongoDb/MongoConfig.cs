using Identity.MongoDb.Models;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using System;
using System.Threading;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.IdGenerators;

namespace Identity.MongoDb
{
    internal static class MongoConfig
    {
        private static bool _initialized = false;
        private static object _initializationLock = new object();
        private static object _initializationTarget;

        public static void EnsureConfigured()
        {
            //todo figure out why it is beeing called more than one time and it is causing exception
            try
            {
                        
            EnsureConfiguredImpl();
            }
            catch (System.Exception)
            {
                
            }
        }

        private static void EnsureConfiguredImpl()
        {
            lock (_initializationLock)
            {
               if(!_initialized){
                Configure();
                _initialized=true;
               }
                
            }
            // LazyInitializer.EnsureInitialized(ref _initializationTarget, ref _initialized, ref _initializationLock, () =>
            // {
            //     Configure();
            //     _initialized = true;
            //    // _initializationTarget = new object();
            //     return new object();
            // });
        }

        private static void Configure()
        {
            
            RegisterConventions();
           

             BsonClassMap.RegisterClassMap<IdentityRole<string>>(cm=>{
                cm.AutoMap();
                cm.MapIdField(x=>x.Id).SetSerializer(new StringSerializer(BsonType.ObjectId))
                    .SetIdGenerator(StringObjectIdGenerator.Instance);
                cm.MapCreator(role=> new IdentityRole<string>(role.Name));
            });

             BsonClassMap.RegisterClassMap<MongoIdentityRole>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(role => new MongoIdentityRole(role.Name));
            });



            BsonClassMap.RegisterClassMap<IdentityUser<string>>(cm =>
            {
                cm.AutoMap();
                cm.MapIdField(c => c.Id)
                    .SetSerializer(new StringSerializer(BsonType.ObjectId))
                    .SetIdGenerator(StringObjectIdGenerator.Instance);
                cm.MapCreator(user => new IdentityUser<string>(user.UserName));
            });

            BsonClassMap.RegisterClassMap<MongoIdentityUser>(cm =>
            {
                cm.AutoMap();
                //cm.MapIdMember(c => c.Id)
                //    .SetSerializer(new StringSerializer(BsonType.ObjectId))
                //    .SetIdGenerator(StringObjectIdGenerator.Instance);

                cm.MapCreator(user => new MongoIdentityUser(user.UserName, user.Email));
            });

            //BsonClassMap.RegisterClassMap<MongoUserClaim>(cm =>
            //{
            //    cm.AutoMap();
            //    cm.MapCreator(c => new MongoUserClaim(c.ClaimType, c.ClaimValue));
            //});

            BsonClassMap.RegisterClassMap<MongoUserEmail>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(cr => new MongoUserEmail(cr.Value));
            });

            BsonClassMap.RegisterClassMap<MongoUserContactRecord>(cm =>
            {
                cm.AutoMap();
            });

            BsonClassMap.RegisterClassMap<MongoUserPhoneNumber>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(cr => new MongoUserPhoneNumber(cr.Value));
            });

            BsonClassMap.RegisterClassMap<MongoUserLogin>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(l => new MongoUserLogin(new UserLoginInfo(l.LoginProvider, l.ProviderKey, l.ProviderDisplayName)));
            });

            BsonClassMap.RegisterClassMap<Occurrence>(cm =>
            {
                cm.AutoMap();
                cm.MapCreator(cr => new Occurrence(cr.Instant));
                cm.MapMember(x => x.Instant).SetSerializer(new DateTimeSerializer(DateTimeKind.Utc, BsonType.Document));
            });
        }

        private static void RegisterConventions()
        {
            var pack = new ConventionPack
            {
                new IgnoreIfNullConvention(false),
                new CamelCaseElementNameConvention(),
            };

            ConventionRegistry.Register("Identity.MongoDb", pack, IsConventionApplicable);
        }

        private static bool IsConventionApplicable(Type type)
        {
            return type == typeof(MongoIdentityUser)
                || type == typeof(MongoUserClaim)
                || type == typeof(MongoUserContactRecord)
                || type == typeof(MongoUserEmail)
                || type == typeof(MongoUserLogin)
                || type == typeof(MongoUserPhoneNumber)
                || type == typeof(ConfirmationOccurrence)
                || type == typeof(FutureOccurrence)
                || type == typeof(Occurrence);
        }
    }
}