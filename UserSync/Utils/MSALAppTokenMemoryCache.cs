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
    /// An implementation of token cache for both Confidential clients backed by MemoryCache.
    /// MemoryCache is useful in Api scenarios where there is no HttpContext to cache data.
    /// </summary>
    public class MSALAppTokenMemoryCache
    {
        private readonly string AppCacheId;

        private readonly MemoryCache memoryCache = MemoryCache.Default;
        private readonly DateTimeOffset cacheDuration = DateTimeOffset.Now.AddHours(12);

        private readonly ITokenCache appTokenCache;

        public MSALAppTokenMemoryCache(string clientId, ITokenCache appTokenCache)
        {
            this.AppCacheId = clientId + "_AppTokenCache";

            if (this.appTokenCache == null)
            {
                this.appTokenCache = appTokenCache;
                this.appTokenCache.SetBeforeAccess(this.AppTokenCacheBeforeAccessNotification);
                this.appTokenCache.SetAfterAccess(this.AppTokenCacheAfterAccessNotification);
            }

            this.LoadAppTokenCacheFromMemory();
        }

        public void LoadAppTokenCacheFromMemory()
        {
            // Ideally, methods that load and persist should be thread safe. MemoryCache.Get() is thread safe.
            byte[] tokenCacheBytes = (byte[])this.memoryCache.Get(this.AppCacheId);
            if (tokenCacheBytes != null)
            {
                this.appTokenCache.DeserializeMsalV3(tokenCacheBytes);
            }
        }

        public void PersistAppTokenCache()
        {
            // Ideally, methods that load and persist should be thread safe.MemoryCache.Get() is thread safe.
            // Reflect changes in the persistent store
            this.memoryCache.Set(this.AppCacheId, this.appTokenCache.SerializeMsalV3(), this.cacheDuration);
        }

        public void Clear()
        {
            this.memoryCache.Remove(this.AppCacheId);

            // Nulls the currently deserialized instance
            this.LoadAppTokenCacheFromMemory();
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
    }
}