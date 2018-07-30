using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Identity.MongoDb.Models;
using System.Security.Claims;
using MongoDB.Bson;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson.Serialization.Attributes;

namespace Identity.MongoDb
{
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Local", Justification = "MongoDB serialization needs private setters")]
    public class MongoIdentityUser : IdentityUser
    {
        private List<MongoUserClaim> _claims;
        private List<MongoUserLogin> _logins;
        private List<MongoIdentityRole> mongoRoles;
        private List<string> roles;

        public MongoIdentityUser(string userName, string email) : this(userName)
        {
            if (email != null)
            {
                Email = email;
            }
        }

        

        public MongoIdentityUser(string userName) : base(userName)
        {
            if (userName == null)
            {
                throw new ArgumentNullException(nameof(userName));
            }

            Id = ObjectId.GenerateNewId().ToString();
            UserName = userName;
            CreatedOn = new Occurrence();

            EnsureClaimsIsSet();
            EnsureLoginsIsSet();
            EnsureRolesIsSet();
        }
        public bool IsTwoFactorEnabled { get; private set; }

        public IEnumerable<string> Roles
        {
            get
            {
                EnsureRolesIsSet();
                return roles;
            }

            // ReSharper disable once UnusedMember.Local, MongoDB serialization needs private setters
            private set
            {
                EnsureRolesIsSet();
                if (value != null)
                {
                    roles.AddRange(value);
                }
            }
        }

        public IEnumerable<MongoUserClaim> Claims
        {
            get
            {
                EnsureClaimsIsSet();
                return _claims;
            }

            // ReSharper disable once UnusedMember.Local, MongoDB serialization needs private setters
            private set
            {
                EnsureClaimsIsSet();
                if (value != null)
                {
                    _claims.AddRange(value);
                }
            }
        }

        public IEnumerable<MongoUserLogin> Logins
        {
            get
            {
                EnsureLoginsIsSet();
                return _logins;
            }

            // ReSharper disable once UnusedMember.Local, MongoDB serialization needs private setters
            private set
            {
                EnsureLoginsIsSet();
                if (value != null)
                {
                    _logins.AddRange(value);
                }
            }
        }
        public bool IsLockoutEnabled { get; private set; }
        public FutureOccurrence LockoutEndDate { get; private set; }

        public Occurrence CreatedOn { get; private set; }
        public Occurrence DeletedOn { get; private set; }
        public string AuthenticatorKey { get; private set; }
        public List<string> RecoveryCodes { get; private set; }

        public virtual void EnableTwoFactorAuthentication()
        {
            IsTwoFactorEnabled = true;
        }

        public virtual void DisableTwoFactorAuthentication()
        {
            IsTwoFactorEnabled = false;
        }

        public virtual void EnableLockout()
        {
            IsLockoutEnabled = true;
        }

        public virtual void DisableLockout()
        {
            IsLockoutEnabled = false;
        }

        public virtual void SetEmail(string email)
        {
            var mongoUserEmail = new MongoUserEmail(email);
            SetEmail(mongoUserEmail);
        }

        public virtual void SetEmail(MongoUserEmail mongoUserEmail)
        {
            Email = mongoUserEmail.NormalizedValue;
        }

        public virtual void SetNormalizedUserName(string normalizedUserName)
        {
            if (normalizedUserName == null)
            {
                throw new ArgumentNullException(nameof(normalizedUserName));
            }

            NormalizedUserName = normalizedUserName;
        }

        public virtual void SetPhoneNumber(string phoneNumber)
        {
            var mongoUserPhoneNumber = new MongoUserPhoneNumber(phoneNumber);
            SetPhoneNumber(mongoUserPhoneNumber);
        }

        public virtual void SetPhoneNumber(MongoUserPhoneNumber mongoUserPhoneNumber)
        {
            PhoneNumber = mongoUserPhoneNumber.Value;
        }

        public virtual void SetPasswordHash(string passwordHash)
        {
            PasswordHash = passwordHash;
        }

        public virtual void SetSecurityStamp(string securityStamp)
        {
            SecurityStamp = securityStamp;
        }

        public virtual void SetAccessFailedCount(int accessFailedCount)
        {
            AccessFailedCount = accessFailedCount;
        }

        public virtual void ResetAccessFailedCount()
        {
            AccessFailedCount = 0;
        }

        public virtual void LockUntil(DateTime lockoutEndDate)
        {
            LockoutEndDate = new FutureOccurrence(lockoutEndDate);
        }

        public virtual void AddClaim(Claim claim)
        {
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            AddClaim(new MongoUserClaim(claim));
        }

        public virtual void AddClaim(MongoUserClaim mongoUserClaim)
        {
            if (mongoUserClaim == null)
            {
                throw new ArgumentNullException(nameof(mongoUserClaim));
            }

            _claims.Add(mongoUserClaim);
        }

        public virtual void RemoveClaim(MongoUserClaim mongoUserClaim)
        {
            if (mongoUserClaim == null)
            {
                throw new ArgumentNullException(nameof(mongoUserClaim));
            }

            _claims.Remove(mongoUserClaim);
        }

        public virtual void AddLogin(MongoUserLogin mongoUserLogin)
        {
            if (mongoUserLogin == null)
            {
                throw new ArgumentNullException(nameof(mongoUserLogin));
            }

            _logins.Add(mongoUserLogin);
        }

        public virtual void RemoveLogin(MongoUserLogin mongoUserLogin)
        {
            if (mongoUserLogin == null)
            {
                throw new ArgumentNullException(nameof(mongoUserLogin));
            }

            _logins.Remove(mongoUserLogin);
        }

        public void Delete()
        {
            if (DeletedOn != null)
            {
                throw new InvalidOperationException($"User '{Id}' has already been deleted.");
            }

            DeletedOn = new Occurrence();
        }

        private void EnsureClaimsIsSet()
        {
            if (_claims == null)
            {
                _claims = new List<MongoUserClaim>();
            }
        }

        private void EnsureLoginsIsSet()
        {
            if (_logins == null)
            {
                _logins = new List<MongoUserLogin>();
            }
        }

        private void EnsureRolesIsSet()
        {
            if (roles == null)
            {
                roles = new List<string>();
            }
            if(mongoRoles == null)
            {
                mongoRoles = new List<MongoIdentityRole>();
            }
        }

        internal void SetAuthenticatorKeyAsync(string key) {
            AuthenticatorKey = key;


        }

        public virtual void AddRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentNullException(nameof(roleName));
            roles.Add(roleName);
        }

        public virtual void AddRole(MongoIdentityRole role)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }

            mongoRoles.Add(role);
        }



        public virtual void RemoveRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                throw new ArgumentNullException(nameof(roleName));
            roles.Remove(roleName);
        }
               

        internal void ReplaceRecoveryCodes(IEnumerable<string> recoveryCodes) {
            RecoveryCodes =new List<string>(recoveryCodes);

        }
        internal bool RedeemCode(string code) {

            
            return RecoveryCodes.RemoveAll(x => x == code)==1;

        }
        internal int CountOfRecoveryCodes() {

            return RecoveryCodes.Count();
        }


    }
}