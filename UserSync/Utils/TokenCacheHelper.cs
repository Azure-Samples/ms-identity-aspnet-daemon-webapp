/*
 The MIT License (MIT)

Copyright (c) 2015 Microsoft Corporation

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using Microsoft.Identity.Client;
using System;
using System.Security.Claims;
using System.Threading;
using System.Web;

namespace UserSync.Utils
{
    /// <summary>
    /// An implementation of token cache for both Confidential and Public clients backed by HttpContext Session
    /// </summary>
    public class TokenCacheHelper
    {
        private readonly string appId;
        private readonly string AppCacheId;
        private readonly string UserCacheId;

        private static ReaderWriterLockSlim SessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private HttpContextBase HttpContext = null;
        
        private readonly TokenCache appTokenCache;
        private readonly TokenCache userTokenCache;

        public TokenCacheHelper(string clientId, HttpContextBase httpcontext)
        {
            this.appId = clientId;
            this.AppCacheId = this.appId + "_AppTokenCache";
            this.UserCacheId = this.appId + "_UserTokenCache_";
            this.HttpContext = httpcontext;

            if (this.appTokenCache == null)
            {
                this.appTokenCache = new TokenCache();
                this.appTokenCache.SetBeforeAccess(this.AppTokenCacheBeforeAccessNotification);
                this.appTokenCache.SetAfterAccess(this.AppTokenCacheAfterAccessNotification);
            }

            if (this.userTokenCache == null)
            {
                this.userTokenCache = new TokenCache();
                this.userTokenCache.SetBeforeAccess(this.UserTokenCacheBeforeAccessNotification);
                this.userTokenCache.SetAfterAccess(this.UserTokenCacheAfterAccessNotification);
            }

            this.LoadAppTokenCache();
            this.LoadUserTokenCache();
        }

        public void LoadAppTokenCache()
        {
            SessionLock.EnterReadLock();
            this.appTokenCache.Deserialize((byte[])this.HttpContext.Session[this.AppCacheId]);
            SessionLock.ExitReadLock();
        }

        public void LoadUserTokenCache()
        {
            SessionLock.EnterReadLock();
            this.userTokenCache.Deserialize((byte[])this.HttpContext.Session[this.GetSignedInUsersCacheKey()]);
            SessionLock.ExitReadLock();
        }

        public void PersistAppTokenCache()
        {
            SessionLock.EnterWriteLock();
            this.HttpContext.Session[this.AppCacheId] = this.appTokenCache.Serialize();
            SessionLock.ExitWriteLock();
        }

        public void PersistUserTokenCache()
        {
            SessionLock.EnterWriteLock();
            this.HttpContext.Session[this.GetSignedInUsersCacheKey()] = this.userTokenCache.Serialize();
            SessionLock.ExitWriteLock();
        }

        public void Clear()
        {
            SessionLock.EnterWriteLock();
            this.HttpContext.Session.Remove(this.GetSignedInUsersCacheKey());
            this.HttpContext.Session.Remove(this.AppCacheId);
            SessionLock.ExitWriteLock();
        }

        /// <summary>
        /// Triggered right before MSAL needs to access the cache. Reload the cache from the persistent store in case it changed since the last access.
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private void AppTokenCacheBeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            this.LoadAppTokenCache();
        }

        /// <summary>
        /// Triggered right after MSAL accessed the cache.
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private void AppTokenCacheAfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                this.PersistAppTokenCache();
            }
        }

        private void UserTokenCacheAfterAccessNotification(TokenCacheNotificationArgs args)
        {
            this.LoadUserTokenCache();
        }

        private void UserTokenCacheBeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (args.HasStateChanged)
            {
                this.PersistUserTokenCache();
            }
        }

        public TokenCache GetMsalAppTokenCacheInstance()
        {
            this.LoadAppTokenCache();
            return this.appTokenCache;
        }

        public TokenCache GetMsalUserTokenCacheInstance()
        {
            this.LoadUserTokenCache();
            return this.userTokenCache;
        }

        public string GetSignedInUsersCacheKey()
        {
            string objectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
            string signedInUsersId = string.Empty;

            if (ClaimsPrincipal.Current != null)
            {
                signedInUsersId = ClaimsPrincipal.Current.FindFirst(objectIdClaimType)?.Value;
            }
            return $"{this.UserCacheId}{signedInUsersId}";
        }
    }
}