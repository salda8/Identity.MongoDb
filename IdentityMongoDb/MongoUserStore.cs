using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Identity.MongoDb.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Identity.MongoDb
{
    public class MongoUserStore<TUser> :
        IUserStore<TUser>,
        IUserRoleStore<TUser>,
        IUserLoginStore<TUser>,
        IUserClaimStore<TUser>,
        IUserPasswordStore<TUser>,
        IUserSecurityStampStore<TUser>,
        IUserTwoFactorStore<TUser>,
        IUserEmailStore<TUser>,
        IUserLockoutStore<TUser>,
        IUserPhoneNumberStore<TUser>,
        IUserAuthenticatorKeyStore<TUser>,
        IUserTwoFactorRecoveryCodeStore<TUser>
        where TUser : MongoIdentityUser
    {
        [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
        private static object _initializationLock = new object();

        [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
        private static object _initializationTarget;

        [SuppressMessage("ReSharper", "StaticMemberInGenericType")]
        private static bool _initialized = false;

        private readonly IMongoCollection<TUser> _usersCollection;

        static MongoUserStore()
        {
            MongoConfig.EnsureConfigured();
        }

        public MongoUserStore(IOptions<MongoDbSettings> options)
            : this(Constants.DefaultCollectionName, options)
        {
        }

        public MongoUserStore(string userCollectionName, IOptions<MongoDbSettings> options)
        {
            if (userCollectionName == null)
            {
                throw new ArgumentNullException(nameof(userCollectionName));
            }

            IMongoDatabase database = new MongoClient(options.Value.ConnectionString).GetDatabase(options.Value.Database);
            _usersCollection = database.GetCollection<TUser>(userCollectionName);

            EnsureIndicesCreatedAsync().GetAwaiter().GetResult();
        }

        public Task AddClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }

            foreach (Claim claim in claims)
            {
                user.AddClaim(claim);
            }

            return Task.FromResult(0);
        }

        public Task AddLoginAsync(TUser user, UserLoginInfo login, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (login == null)
            {
                throw new ArgumentNullException(nameof(login));
            }

            // NOTE: Not the best way to ensure uniquness.
            if (user.Logins.Any(x => x.Equals(login)))
            {
                throw new InvalidOperationException("Login already exists.");
            }

            user.AddLogin(new MongoUserLogin(login));

            return Task.FromResult(0);
        }

        public Task AddToRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            user.AddRole(roleName);
            return Task.FromResult(0);
        }

        public Task<int> CountCodesAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken)) => Task.FromResult(user.CountOfRecoveryCodes());

        public async Task<IdentityResult> CreateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            cancellationToken.ThrowIfCancellationRequested();

            await _usersCollection.InsertOneAsync(user, cancellationToken: cancellationToken)
                ;

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> DeleteAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            cancellationToken.ThrowIfCancellationRequested();

            user.Delete();
            FilterDefinition<TUser> query = Builders<TUser>.Filter.Eq(u => u.Id, user.Id);
            await _usersCollection.DeleteOneAsync(query, cancellationToken);
            return IdentityResult.Success;
        }

        public void Dispose()
        {
        }

        public Task<TUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (normalizedEmail == null)
            {
                throw new ArgumentNullException(nameof(normalizedEmail));
            }

            FilterDefinition<TUser> query = Builders<TUser>.Filter.And(
                Builders<TUser>.Filter.Eq(u => u.NormalizedEmail, normalizedEmail),
                Builders<TUser>.Filter.Eq(u => u.DeletedOn, null)
            );

            return _usersCollection.Find(query).FirstOrDefaultAsync(cancellationToken);
        }

        public Task<TUser> FindByIdAsync(string userId, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (userId == null)
            {
                throw new ArgumentNullException(nameof(userId));
            }

            cancellationToken.ThrowIfCancellationRequested();

            FilterDefinition<TUser> query = Builders<TUser>.Filter.And(
                Builders<TUser>.Filter.Eq(u => u.Id, userId),
                Builders<TUser>.Filter.Eq(u => u.DeletedOn, null)
            );

            return _usersCollection.Find(query).FirstOrDefaultAsync(cancellationToken);
        }

        public Task<TUser> FindByLoginAsync(string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (loginProvider == null)
            {
                throw new ArgumentNullException(nameof(loginProvider));
            }

            if (providerKey == null)
            {
                throw new ArgumentNullException(nameof(providerKey));
            }

            FilterDefinition<TUser> notDeletedQuery = Builders<TUser>.Filter.Eq(u => u.DeletedOn, null);
            FilterDefinition<TUser> loginQuery = Builders<TUser>.Filter.ElemMatch(usr => usr.Logins,
                Builders<MongoUserLogin>.Filter.And(
                    Builders<MongoUserLogin>.Filter.Eq(lg => lg.LoginProvider, loginProvider),
                    Builders<MongoUserLogin>.Filter.Eq(lg => lg.ProviderKey, providerKey)
                )
            );

            FilterDefinition<TUser> query = Builders<TUser>.Filter.And(notDeletedQuery, loginQuery);

            return _usersCollection.Find(query).FirstOrDefaultAsync();
        }

        public Task<TUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (normalizedUserName == null)
            {
                throw new ArgumentNullException(nameof(normalizedUserName));
            }

            cancellationToken.ThrowIfCancellationRequested();

            FilterDefinition<TUser> query = Builders<TUser>.Filter.And(
                Builders<TUser>.Filter.Eq(u => u.NormalizedUserName, normalizedUserName),
                Builders<TUser>.Filter.Eq(u => u.DeletedOn, null)
            );

            return _usersCollection.Find(query).FirstOrDefaultAsync(cancellationToken);
        }

        public Task<int> GetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.AccessFailedCount);
        }

        public Task<string> GetAuthenticatorKeyAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken)) => Task.FromResult(user.AuthenticatorKey);

        public Task<IList<Claim>> GetClaimsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            var claims = user.Claims.Select(clm => new Claim(clm.ClaimType, clm.ClaimValue)).ToList();

            return Task.FromResult<IList<Claim>>(claims);
        }

        public Task<string> GetEmailAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            string email = (user.Email != null) ? user.Email : null;

            return Task.FromResult(email);
        }

        public Task<bool> GetEmailConfirmedAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (user.Email == null)
            {
                throw new InvalidOperationException("Cannot get the confirmation status of the e-mail since the user doesn't have an e-mail.");
            }

            return Task.FromResult(user.EmailConfirmed);
        }

        public Task<bool> GetLockoutEnabledAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.IsLockoutEnabled);
        }

        public Task<DateTimeOffset?> GetLockoutEndDateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            DateTimeOffset? lockoutEndDate = user.LockoutEndDate != null
                ? new DateTimeOffset(user.LockoutEndDate.Instant)
                : default(DateTimeOffset?);

            return Task.FromResult(lockoutEndDate);
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            IEnumerable<UserLoginInfo> logins = user.Logins.Select(login =>
                new UserLoginInfo(login.LoginProvider, login.ProviderKey, login.ProviderDisplayName));

            return Task.FromResult<IList<UserLoginInfo>>(logins.ToList());
        }

        public Task<string> GetNormalizedEmailAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            string normalizedEmail = (user.Email != null) ? user.NormalizedEmail : null;

            return Task.FromResult(normalizedEmail);
        }

        public Task<string> GetNormalizedUserNameAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.NormalizedUserName);
        }

        public Task<string> GetPasswordHashAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.PasswordHash);
        }

        public Task<string> GetPhoneNumberAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.PhoneNumber);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (user.PhoneNumber == null)
            {
                throw new InvalidOperationException("Cannot get the confirmation status of the phone number since the user doesn't have a phone number.");
            }

            return Task.FromResult(user.PhoneNumberConfirmed);
        }

        public Task<IList<string>> GetRolesAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            IList<string> result = user.Roles.ToList();
            return Task.FromResult(result);
        }

        public Task<string> GetSecurityStampAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.SecurityStamp);
        }

        public Task<bool> GetTwoFactorEnabledAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.IsTwoFactorEnabled);
        }

        public Task<long> GetUserCountAsync() => _usersCollection.CountAsync(_ => true);

        public Task<string> GetUserIdAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.Id);
        }

        public Task<string> GetUserNameAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.UserName);
        }

        public async Task<IList<TUser>> GetUsersForClaimAsync(Claim claim, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            FilterDefinition<TUser> notDeletedQuery = Builders<TUser>.Filter.Eq(u => u.DeletedOn, null);
            FilterDefinition<TUser> claimQuery = Builders<TUser>.Filter.ElemMatch(usr => usr.Claims,
                Builders<MongoUserClaim>.Filter.And(
                    Builders<MongoUserClaim>.Filter.Eq(c => c.ClaimType, claim.Type),
                    Builders<MongoUserClaim>.Filter.Eq(c => c.ClaimValue, claim.Value)
                )
            );

            FilterDefinition<TUser> query = Builders<TUser>.Filter.And(notDeletedQuery, claimQuery);
            List<TUser> users = await _usersCollection.Find(query).ToListAsync();

            return users;
        }

        public Task<IList<TUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken = default(CancellationToken)) => throw new NotImplementedException();

        public Task<bool> HasPasswordAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            return Task.FromResult(user.PasswordHash != null);
        }

        public async Task<int> IncrementAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            FilterDefinition<TUser> filter = Builders<TUser>.Filter.Eq(u => u.Id, user.Id);
            UpdateDefinition<TUser> update = Builders<TUser>.Update.Inc(usr => usr.AccessFailedCount, 1);
            var findOneAndUpdateOptions = new FindOneAndUpdateOptions<TUser, int>
            {
                ReturnDocument = ReturnDocument.After,
                Projection = Builders<TUser>.Projection.Expression(usr => usr.AccessFailedCount)
            };

            int newCount = await _usersCollection
                .FindOneAndUpdateAsync<int>(filter, update, findOneAndUpdateOptions)
                ;

            user.SetAccessFailedCount(newCount);

            return newCount;
        }

        public Task<bool> IsInRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken)) => Task.FromResult(user.Roles.Any(x => x == roleName));

        public Task<bool> RedeemCodeAsync(TUser user, string code, CancellationToken cancellationToken = default(CancellationToken)) => Task.FromResult(user.RedeemCode(code));

        public Task RemoveClaimsAsync(TUser user, IEnumerable<Claim> claims, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (claims == null)
            {
                throw new ArgumentNullException(nameof(claims));
            }

            foreach (Claim claim in claims)
            {
                user.RemoveClaim(new MongoUserClaim(claim));
            }

            return Task.FromResult(0);
        }

        public Task RemoveFromRoleAsync(TUser user, string roleName, CancellationToken cancellationToken = default(CancellationToken))
        {
            user.RemoveRole(roleName);
            return Task.FromResult(0);
        }

        public Task RemoveLoginAsync(TUser user, string loginProvider, string providerKey, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (loginProvider == null)
            {
                throw new ArgumentNullException(nameof(loginProvider));
            }

            if (providerKey == null)
            {
                throw new ArgumentNullException(nameof(providerKey));
            }

            var login = new UserLoginInfo(loginProvider, providerKey, string.Empty);
            MongoUserLogin loginToRemove = user.Logins.FirstOrDefault(x => x.Equals(login));

            if (loginToRemove != null)
            {
                user.RemoveLogin(loginToRemove);
            }

            return Task.FromResult(0);
        }

        public Task ReplaceClaimAsync(TUser user, Claim claim, Claim newClaim, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (claim == null)
            {
                throw new ArgumentNullException(nameof(claim));
            }

            if (newClaim == null)
            {
                throw new ArgumentNullException(nameof(newClaim));
            }

            user.RemoveClaim(new MongoUserClaim(claim));
            user.AddClaim(newClaim);

            return Task.FromResult(0);
        }

        public Task ReplaceCodesAsync(TUser user, IEnumerable<string> recoveryCodes, CancellationToken cancellationToken = default(CancellationToken))
        {
            user.ReplaceRecoveryCodes(recoveryCodes);
            return Task.FromResult(0);
        }

        public Task ResetAccessFailedCountAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.ResetAccessFailedCount();

            return Task.FromResult(0);
        }

        public async Task<IdentityResult> SetAsDeletedAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            cancellationToken.ThrowIfCancellationRequested();

            user.Delete();
            FilterDefinition<TUser> query = Builders<TUser>.Filter.Eq(u => u.Id, user.Id);
            //set delete true
            UpdateDefinition<TUser> update = Builders<TUser>.Update.Set(u => u.DeletedOn, user.DeletedOn);
            await _usersCollection.UpdateOneAsync(query, update, cancellationToken: cancellationToken);
            return IdentityResult.Success;
        }

        public Task SetAuthenticatorKeyAsync(TUser user, string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }
            if (user is null) { throw new ArgumentNullException(nameof(user)); }
            user.SetAuthenticatorKeyAsync(key);
            return Task.FromResult(0);
        }

        public Task SetEmailAsync(TUser user, string email, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (email == null)
            {
                throw new ArgumentNullException(nameof(email));
            }

            user.SetEmail(email);

            return Task.FromResult(0);
        }

        public Task SetEmailConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (user.Email == null)
            {
                throw new InvalidOperationException("Cannot set the confirmation status of the e-mail because user doesn't have an e-mail.");
            }

            if (confirmed)
            {
                user.EmailConfirmed = true;
            }
            else
            {
                user.EmailConfirmed = false;
            }

            return Task.FromResult(0);
        }

        public Task SetLockoutEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (enabled)
            {
                user.EnableLockout();
            }
            else
            {
                user.DisableLockout();
            }

            return Task.FromResult(0);
        }

        public Task SetLockoutEndDateAsync(TUser user, DateTimeOffset? lockoutEnd, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (lockoutEnd != null)
            {
                user.LockUntil(lockoutEnd.Value.UtcDateTime);
            }

            return Task.FromResult(0);
        }

        public Task SetNormalizedEmailAsync(TUser user, string normalizedEmail, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            // This method can be called even if user doesn't have an e-mail.
            // Act cool in this case and gracefully handle.
            // More info: https://github.com/aspnet/Identity/issues/645

            if (normalizedEmail != null && user.Email != null)
            {
                user.NormalizedEmail = normalizedEmail;
            }

            return Task.FromResult(0);
        }

        public Task SetNormalizedUserNameAsync(TUser user, string normalizedName, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (normalizedName == null)
            {
                throw new ArgumentNullException(nameof(normalizedName));
            }

            user.SetNormalizedUserName(normalizedName);

            return Task.FromResult(0);
        }

        public Task SetPasswordHashAsync(TUser user, string passwordHash, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            user.SetPasswordHash(passwordHash);

            return Task.FromResult(0);
        }

        public Task SetPhoneNumberAsync(TUser user, string phoneNumber, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (phoneNumber == null)
            {
                throw new ArgumentNullException(nameof(phoneNumber));
            }

            user.SetPhoneNumber(phoneNumber);

            return Task.FromResult(0);
        }

        public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (user.PhoneNumber == null)
            {
                throw new InvalidOperationException("Cannot set the confirmation status of the phone number since the user doesn't have a phone number.");
            }

            user.PhoneNumberConfirmed = false;

            return Task.FromResult(0);
        }

        public Task SetSecurityStampAsync(TUser user, string stamp, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (stamp == null)
            {
                throw new ArgumentNullException(nameof(stamp));
            }

            user.SetSecurityStamp(stamp);

            return Task.FromResult(0);
        }

        public Task SetTwoFactorEnabledAsync(TUser user, bool enabled, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            if (enabled)
            {
                user.EnableTwoFactorAuthentication();
            }
            else
            {
                user.DisableTwoFactorAuthentication();
            }

            return Task.FromResult(0);
        }

        public Task SetUserNameAsync(TUser user, string userName, CancellationToken cancellationToken = default(CancellationToken)) => throw new NotSupportedException("Changing the username is not supported.");

        public async Task<IdentityResult> UpdateAsync(TUser user, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (user == null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            FilterDefinition<TUser> query = Builders<TUser>.Filter.And(
                Builders<TUser>.Filter.Eq(u => u.Id, user.Id),
                Builders<TUser>.Filter.Eq(u => u.DeletedOn, null)
            );

            ReplaceOneResult replaceResult = await _usersCollection.ReplaceOneAsync(query, user, new UpdateOptions { IsUpsert = false });

            return replaceResult.IsModifiedCountAvailable && replaceResult.ModifiedCount == 1
                ? IdentityResult.Success
                : IdentityResult.Failed();
        }

        private async Task EnsureIndicesCreatedAsync()
        {
            object obj = LazyInitializer.EnsureInitialized(ref _initializationTarget, ref _initialized, ref _initializationLock, () =>
            {
                return EnsureIndicesCreatedImpl();
            });

            if (obj != null)
            {
                var taskToAwait = (Task)obj;
                await taskToAwait;
            }
        }

        private Task EnsureIndicesCreatedImpl()
        {
            var indexNames = new
            {
                UniqueEmail = "identity_email_unique",
                Login = "identity_logins_loginProvider_providerKey"
            };

            IConventionPack pack = ConventionRegistry.Lookup(typeof(CamelCaseElementNameConvention));
            IndexKeysDefinition<TUser> emailKeyBuilder = Builders<TUser>.IndexKeys.Ascending(user => user.Email);
            IndexKeysDefinition<TUser> loginKeyBuilder = Builders<TUser>.IndexKeys.Ascending("logins.loginProvider").Ascending("logins.providerKey");

            Task<string>[] tasks =
            {
                _usersCollection.Indexes.CreateOneAsync(emailKeyBuilder, new CreateIndexOptions { Unique = true, Name = indexNames.UniqueEmail }),
                _usersCollection.Indexes.CreateOneAsync(loginKeyBuilder, new CreateIndexOptions { Name = indexNames.Login })
            };

            return Task.WhenAll(tasks);
        }
    }
}