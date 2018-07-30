using Identity.MongoDb.Models;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Identity.MongoDb
{
    public class MongoIdentityRole : IdentityRole
    {
        public MongoIdentityRole() 
        {
            Id = ObjectId.GenerateNewId().ToString();
        }

        public MongoIdentityRole(string roleName) : this()
        {
            Name = roleName;
        }

        public MongoIdentityRole(string roleName, string normalizedName) : this()
        {
            Name = roleName;
            NormalizedName = normalizedName;
        }
      

        public override string ToString() => Name;

        public List<MongoUserClaim> Claims { get; set; } = new List<MongoUserClaim>();
    }
}

