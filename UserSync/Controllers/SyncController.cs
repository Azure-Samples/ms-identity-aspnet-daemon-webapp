using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
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
                //daemonClient.AppTokenCache.Clear(Startup.clientId); TODO
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