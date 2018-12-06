using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;

namespace Identity.MongoDb
{
    /// <summary>
    /// Note: Deleting and updating do not modify the roles stored on a user document. If you desire this dynamic
    ///     capability, override the appropriate operations on RoleStore as desired for your application. For example you could
    ///     perform a document modification on the users collection before a delete or a rename.
    ///     When passing a cancellation token, it will only be used if the operation requires a database interaction.
    /// </summary>
    /// <typeparam name="TRole">The type of the role.</typeparam>
    /// <seealso cref="Microsoft.AspNetCore.Identity.IRoleStore{TRole}" />
    public class MongoRoleClaimStore<TRole> : IRoleClaimStore<TRole>
        where TRole : MongoIdentityRole
    {
        private readonly ILookupNormalizer keyNormalizer;
        private readonly ILogger<MongoRoleClaimStore<MongoIdentityRole>> logger;
        private readonly IMongoCollection<TRole> roles;

        public MongoRoleClaimStore(IOptions<MongoDbSettings> options, IEnumerable<IRoleValidator<TRole>> roleValidators, ILogger<MongoRoleClaimStore<MongoIdentityRole>> logger, ILookupNormalizer keyNormalizer)
        {
            var database = new MongoClient(options.Value.ConnectionString).GetDatabase(options.Value.Database);
            roles = database.GetCollection<TRole>("Roles");
            if (roleValidators != null)
            {
                foreach (var v in roleValidators)
                {
                    RoleValidators.Add(v);
                }
            }
            this.logger = logger;
            this.keyNormalizer = keyNormalizer;
        }

        public virtual void Dispose()
        {
            // no need to dispose of anything, mongodb handles connection pooling automatically
        }

        /// <summary>
        /// Throws if this class has been disposed.
        /// </summary>

        /// <summary>
        /// Gets a list of validators for roles to call before persistence.
        /// </summary>
        /// <value>A list of validators for roles to call before persistence.</value>
        public IList<IRoleValidator<TRole>> RoleValidators { get; } = new List<IRoleValidator<TRole>>();

        /// <summary>
        /// Should return <see cref="IdentityResult.Success"/> if validation is successful. This is
        /// called before saving the role via Create or Update.
        /// </summary>
        /// <param name="role">The role</param>
        /// <returns>A <see cref="IdentityResult"/> representing whether validation was successful.</returns>
        protected virtual async Task<IdentityResult> ValidateRoleAsync(TRole role)
        {
            var errors = new List<IdentityError>();
            foreach (var v in RoleValidators)
            {
                var result = await v.ValidateAsync(this as RoleManager<TRole>, role);
                if (!result.Succeeded)
                {
                    errors.AddRange(result.Errors);
                }
            }
            if (errors.Count > 0)
            {
                logger.LogWarning(0, "Role {roleId} validation failed: {errors}.", await GetRoleIdAsync(role, CancellationToken.None), string.Join(";", errors.Select(e => e.Code)));
                return IdentityResult.Failed(errors.ToArray());
            }
            return IdentityResult.Success;
        }

        public virtual async Task<IdentityResult> CreateAsync(TRole role, CancellationToken token = default)
        {
            if (role == null)
            {
                throw new ArgumentNullException(nameof(role));
            }
            var result = await ValidateRoleAsync(role);
            if (!result.Succeeded)
            {
                return result;
            }
            await UpdateNormalizedRoleNameAsync(role, token);

            await roles.InsertOneAsync(role, cancellationToken: token);
            return IdentityResult.Success;
        }

        /// <summary>
        /// Updates the normalized name for the specified <paramref name="role" />.
        /// </summary>
        /// <param name="role">The role whose normalized name needs to be updated.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>
        /// The <see cref="Task" /> that represents the asynchronous operation.
        /// </returns>
        public virtual async Task UpdateNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken = default)
        {
            var name = await GetRoleNameAsync(role, cancellationToken);
            await SetNormalizedRoleNameAsync(role, NormalizeKey(name), cancellationToken);
        }

        public virtual async Task<IdentityResult> UpdateAsync(TRole role, CancellationToken token)
        {
            var result = await roles.ReplaceOneAsync(r => r.Id == role.Id, role, cancellationToken: token);
            // todo low priority result based on replace result
            return IdentityResult.Success;
        }

        /// <summary>
        /// Gets a normalized representation of the specified <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The value to normalize.</param>
        /// <returns>A normalized representation of the specified <paramref name="key"/>.</returns>
        public virtual string NormalizeKey(string key)
        {
            return (keyNormalizer == null) ? key : keyNormalizer.Normalize(key);
        }

        public virtual async Task<IdentityResult> DeleteAsync(TRole role, CancellationToken token = default)
        {
            var result = await roles.DeleteOneAsync(r => r.Id == role.Id, token);

            return IdentityResult.Success;
        }

        public virtual async Task<string> GetRoleIdAsync(TRole role, CancellationToken cancellationToken = default)
            => role.Id;

        public virtual async Task<string> GetRoleNameAsync(TRole role, CancellationToken cancellationToken = default)
            => role.Name;

        public virtual async Task SetRoleNameAsync(TRole role, string roleName, CancellationToken cancellationToken = default)
            => role.Name = roleName;

        // note: can't test as of yet through integration testing because the Identity framework doesn't use this method internally anywhere
        public virtual async Task<string> GetNormalizedRoleNameAsync(TRole role, CancellationToken cancellationToken = default)
            => role.NormalizedName;

        public virtual async Task SetNormalizedRoleNameAsync(TRole role, string normalizedName, CancellationToken cancellationToken = default)
            => role.NormalizedName = normalizedName;

        public virtual Task<TRole> FindByIdAsync(string roleId, CancellationToken token)
            => roles.Find(r => r.Id == roleId)
                .FirstOrDefaultAsync(token);

        public virtual Task<TRole> FindByNameAsync(string normalizedName, CancellationToken token)
            => roles.Find(r => r.NormalizedName == normalizedName)
                .FirstOrDefaultAsync(token);

        public async Task<IList<Claim>> GetClaimsAsync(TRole role, CancellationToken cancellationToken = default)
        {
            var foundRole = await FindByIdAsync(role.Id, cancellationToken);

            return foundRole.Claims.Select(x => new Claim(x.ClaimType, x.ClaimValue)).ToList();
        }

        public async Task AddClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            var foundRole = await FindByIdAsync(role.Id, cancellationToken);

            foundRole.Claims.Add(new MongoUserClaim(claim));
            await UpdateAsync(foundRole, cancellationToken);
        }

        public async Task RemoveClaimAsync(TRole role, Claim claim, CancellationToken cancellationToken = default)
        {
            var foundRole = await FindByIdAsync(role.Id, cancellationToken);
            foundRole.Claims.Remove(new MongoUserClaim(claim));
            await UpdateAsync(foundRole, cancellationToken);
        }

        public virtual IQueryable<TRole> Roles
            => roles.AsQueryable();
    }
}