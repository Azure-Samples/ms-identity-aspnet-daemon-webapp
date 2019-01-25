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
using System.Runtime.Caching;

namespace UserSync.Utils
{
    /// <summary>
    /// An implementation of token cache for both Confidential and Public clients backed by MemoryCache
    /// </summary>
    public class MSALMemoryTokenCache
    {
        private readonly string appId;
        private readonly string AppCacheId;
        private readonly string UserCacheId;

        private readonly MemoryCache memoryCache = MemoryCache.Default;
        private readonly DateTimeOffset cacheDuration = DateTimeOffset.Now.AddHours(12);

        private readonly TokenCache appTokenCache;
        private readonly TokenCache userTokenCache;

        public MSALMemoryTokenCache(string clientId)
        {
            this.appId = clientId;
            this.AppCacheId = this.appId + "_AppTokenCache";
            this.UserCacheId = this.appId + "_UserTokenCache";

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

            this.LoadAppTokenCacheFromMemory();
            this.LoadUserTokenCacheFromMemory();
        }
        
        public void LoadAppTokenCacheFromMemory()
        {
            // Ideally, methods that load and persist should be thread safe. MemoryCache.Get() is thread safe.
            byte[] tokenCacheBytes = (byte[])this.memoryCache.Get(this.AppCacheId);
            if (tokenCacheBytes != null)
            {
                this.appTokenCache.Deserialize(tokenCacheBytes);
            }
        }

        public void LoadUserTokenCacheFromMemory()
        {
            // Ideally, methods that load and persist should be thread safe. MemoryCache.Get() is thread safe.
            byte[] tokenCacheBytes = (byte[])this.memoryCache.Get(this.UserCacheId);
            if (tokenCacheBytes != null)
            {
                this.userTokenCache.Deserialize(tokenCacheBytes);
            }
        }

        public void PersistAppTokenCache()
        {
            // Ideally, methods that load and persist should be thread safe.MemoryCache.Get() is thread safe.
            // Reflect changes in the persistent store
            this.memoryCache.Set(this.AppCacheId, this.appTokenCache.Serialize(), this.cacheDuration);
        }

        public void PersistUserTokenCache()
        {
            // Ideally, methods that load and persist should be thread safe.MemoryCache.Get() is thread safe.            
            this.memoryCache.Set(this.UserCacheId, this.userTokenCache.Serialize(), this.cacheDuration);
        }

        public void Clear()
        {
            this.memoryCache.Remove(this.AppCacheId);
            this.memoryCache.Remove(this.UserCacheId);

            // Nulls the currently deserialized instance
            this.LoadAppTokenCacheFromMemory();
            this.LoadUserTokenCacheFromMemory();
        }

        /// <summary>
        /// Triggered right before MSAL needs to access the cache. Reload the cache from the persistent store in case it changed since the last access.
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private void AppTokenCacheBeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            this.LoadAppTokenCacheFromMemory();
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
            this.LoadUserTokenCacheFromMemory();
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
            this.LoadAppTokenCacheFromMemory();
            return this.appTokenCache;
        }

        public TokenCache GetMsalUserTokenCacheInstance()
        {
            this.LoadUserTokenCacheFromMemory();
            return this.userTokenCache;
        }
    }
}