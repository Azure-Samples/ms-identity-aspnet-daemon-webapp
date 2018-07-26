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
using System.Threading;
using System.Web;

namespace UserSync.Utils
{
    /// <summary>
    /// An implementation of token cache for Confidential Clients
    /// </summary>
    public class MSALCache
    {
        private static readonly ReaderWriterLockSlim SessionLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private readonly string appId;
        private readonly string cacheId;

        private readonly MemoryCache memoryCache = MemoryCache.Default;
        private readonly DateTimeOffset cacheDuration = DateTimeOffset.Now.AddHours(12);
        private readonly TokenCache cache = new TokenCache();

        public MSALCache(string clientId)
        {
            // not object, we want the subject
            this.appId = clientId;
            this.cacheId = this.appId + "_TokenCache";
            this.Load();
        }

        public void Load()
        {
            SessionLock.EnterReadLock();
            this.cache.Deserialize((byte[])this.memoryCache.Get(this.cacheId));
            SessionLock.ExitReadLock();
        }

        public void Persist()
        {
            SessionLock.EnterWriteLock();

            // Optimistically set HasStateChanged to false. We need to do it early to avoid losing changes made by a concurrent thread.
            this.cache.HasStateChanged = false;

            // Reflect changes in the persistent store
            this.memoryCache.Set(this.cacheId, this.cache.Serialize(), this.cacheDuration);
            SessionLock.ExitWriteLock();
        }

        public void Clear()
        {
            SessionLock.EnterWriteLock();

            // Optimistically set HasStateChanged to false. We need to do it early to avoid losing changes made by a concurrent thread.
            this.cache.HasStateChanged = false;

            // Reflect changes in the persistent store
            this.memoryCache.Remove(this.cacheId);

            this.Load(); // Nulls the currently deserialized instance
            SessionLock.ExitWriteLock();
        }

        // Triggered right before MSAL needs to access the cache.
        // Reload the cache from the persistent store in case it changed since the last access.
        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            this.Load();
        }

        // Triggered right after MSAL accessed the cache.
        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // if the access operation resulted in a cache update
            if (this.cache.HasStateChanged)
            {
                this.Persist();
            }
        }

        public TokenCache GetMsalCacheInstance()
        {
            this.cache.SetBeforeAccess(this.BeforeAccessNotification);
            this.cache.SetAfterAccess(this.AfterAccessNotification);
            this.Load();

            return this.cache;
        }
    }
}