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
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;
using UserSync.Models;

namespace UserSync.Controllers
{
    public class SyncController : ApiController
    {
        // Not a good idea.  We're using an in-memory data store in this sample instead
        // of a database purely for purposes of simplifying the sample code.
        private static ConcurrentDictionary<string, List<MsGraphUser>> usersByTenant = new ConcurrentDictionary<string, List<MsGraphUser>>();

        private const string authorityFormat = "https://login.microsoftonline.com/{0}/v2.0";
        private const string msGraphScope = "https://graph.microsoft.com/.default";
        private const string msGraphQuery = "https://graph.microsoft.com/v1.0/users";

        [Authorize]
        public async Task Get(string tenantId)
        {
            // Get a token for the Microsoft Graph. If this line throws an exception for
            // any reason, we'll just let the exception be returned as a 500 response
            // to the caller, and show a generic error message to the user.
            ConfidentialClientApplication daemonClient = new ConfidentialClientApplication(Startup.clientId, String.Format(authorityFormat, tenantId), Startup.redirectUri, new ClientCredential(Startup.clientSecret), null, new TokenCache());
            AuthenticationResult authResult = await daemonClient.AcquireTokenForClientAsync(new string[] { msGraphScope });

            // Query for list of users in the tenant
            HttpClient client = new HttpClient();
            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, msGraphQuery);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            HttpResponseMessage response = await client.SendAsync(request);

            // If the token we used was insufficient to make the query, drop the token from the cache.
            // The Users page of the website will show a message to the user instructing them to grant
            // permissions to the app (see User/Index.cshtml).
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                // BUG: Here, we should clear MSAL's app token cache to ensure that on a subsequent call
                // to SyncController, MSAL does not return the same access token that resulted in this 403.
                // By clearing the cache, MSAL will be forced to retrieve a new access token from AAD, 
                // which will contain the most up-to-date set of permissions granted to the app. Since MSAL
                // currently does not provide a way to clear the app token cache, we have commented this line
                // out. Thankfully, since this app uses the default in-memory app token cache, the app still
                // works correctly, since the in-memory cache is not persistent across calls to SyncController
                // anyway. If you build a persistent app token cache for MSAL, you should make sure to clear 
                // it at this point in the code.
                //
                //daemonClient.AppTokenCache.Clear(Startup.clientId);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpResponseException(response.StatusCode);
            }

            // Record users in the data store (note that this only records the first page of users)
            string json = await response.Content.ReadAsStringAsync();
            MsGraphUserListResponse users = JsonConvert.DeserializeObject<MsGraphUserListResponse>(json);
            usersByTenant[tenantId] = users.value;
            return;
        }

        public static List<MsGraphUser> GetUsersForTenant(string tenantId)
        {
            List<MsGraphUser> users = null;
            usersByTenant.TryGetValue(tenantId, out users);
            return users ?? new List<MsGraphUser>();
        }
    }
}
