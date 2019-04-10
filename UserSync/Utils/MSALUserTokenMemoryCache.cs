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
using System.Security.Claims;

namespace UserSync.Utils
{
    /// <summary>
    /// An implementation of token cache for both Confidential and Public clients backed by MemoryCache.
    /// MemoryCache is useful in Api scenarios where there is no HttpContext to cache data.
    /// </summary>
    public class MSALUserTokenMemoryCache
    {
        private readonly string appId;
        private readonly MemoryCache memoryCache = MemoryCache.Default;
        private readonly DateTimeOffset cacheDuration = DateTimeOffset.Now.AddHours(12);

        private readonly ITokenCache userTokenCache;

        public MSALUserTokenMemoryCache(string clientId, ITokenCache userTokenCache)
        {
            this.appId = clientId;
            if (this.userTokenCache == null)
            {
                this.userTokenCache = userTokenCache;
                this.userTokenCache.SetBeforeAccess(this.UserTokenCacheBeforeAccessNotification);
                this.userTokenCache.SetAfterAccess(this.UserTokenCacheAfterAccessNotification);
            }

            this.LoadUserTokenCacheFromMemory();
        }

        public void LoadUserTokenCacheFromMemory()
        {
            // Ideally, methods that load and persist should be thread safe. MemoryCache.Get() is thread safe.
            byte[] tokenCacheBytes = (byte[])this.memoryCache.Get(this.GetSignedInUsersCacheKey());
            if (tokenCacheBytes != null)
            {
                this.userTokenCache.DeserializeMsalV3(tokenCacheBytes);
            }
        }

        public void PersistUserTokenCache()
        {
            // Ideally, methods that load and persist should be thread safe.MemoryCache.Get() is thread safe.
            this.memoryCache.Set(this.GetSignedInUsersCacheKey(), this.userTokenCache.SerializeMsalV3(), this.cacheDuration);
        }

        public void Clear()
        {
            this.memoryCache.Remove(this.GetSignedInUsersCacheKey());

            // Nulls the currently deserialized instance
            this.LoadUserTokenCacheFromMemory();
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

        public string GetSignedInUsersCacheKey()
        {
            string objectIdClaimType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
            string signedInUsersId = string.Empty;

            if (ClaimsPrincipal.Current != null)
            {
                signedInUsersId = ClaimsPrincipal.Current.FindFirst(objectIdClaimType)?.Value;
            }
            return $"{this.appId}_UserTokenCache_{signedInUsersId}";
        }
    }
}