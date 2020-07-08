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
        private readonly MemoryCache memoryCache = MemoryCache.Default;
        private readonly DateTimeOffset cacheDuration = DateTimeOffset.Now.AddHours(12);

        public MSALAppTokenMemoryCache(ITokenCache appTokenCache)
        {
            appTokenCache.SetBeforeAccess(AppTokenCacheBeforeAccessNotification);
            appTokenCache.SetAfterAccess(AppTokenCacheAfterAccessNotification);
        }

        public void Clear(string cacheKey)
        {
            memoryCache.Remove(cacheKey);
        }

        /// <summary>
        /// Triggered right before MSAL needs to access the cache. Reload the cache from the persistent store in case it changed since the last access.
        /// </summary>
        /// <param name="args">Contains parameters used by the MSAL call accessing the cache.</param>
        private void AppTokenCacheBeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            byte[] tokenCacheBytes = (byte[])memoryCache.Get(args.SuggestedCacheKey);
            if (tokenCacheBytes != null)
            {
                args.TokenCache.DeserializeMsalV3(tokenCacheBytes, shouldClearExistingCache: true);
            }
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
                if (args.HasTokens)
                {
                    // Reflect changes in the persistent store
                    memoryCache.Set(args.SuggestedCacheKey, args.TokenCache.SerializeMsalV3(), cacheDuration);
                }
                else
                {
                    memoryCache.Remove(args.SuggestedCacheKey);
                }
            }
        }
    }
}