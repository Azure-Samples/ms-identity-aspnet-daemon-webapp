using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using Microsoft.Identity.Client;

namespace UserSync.Controllers
{
    public class UserController : Controller
    {
        private const string tenantIdClaimType = "http://schemas.microsoft.com/identity/claims/tenantid";
        private const string authorityFormat = "https://login.microsoftonline.com/{0}/v2.0";
        private const string msGraphScope = "https://graph.microsoft.com/.default";
        private const string msGraphQuery = "https://graph.microsoft.com/v1.0/users";

        // GET: Calendar
        public async Task<ActionResult> Index()
        {
            // Make sure the user is signed in
            if (!Request.IsAuthenticated)
            {
                return new RedirectResult("/Account/Index");
            }

            string tenantId = ClaimsPrincipal.Current.FindFirst(tenantIdClaimType).Value;
            try
            {
                // Try to get a token for the tenant
                ConfidentialClientApplication daemonClient = new ConfidentialClientApplication(String.Format(authorityFormat, tenantId), Startup.clientId, Startup.redirectUri, new ClientCredential(Startup.clientSecret), null);
                AuthenticationResult authResult = await daemonClient.AcquireTokenForClient(new string[] { msGraphScope }, null);

                // Query for list of users in the tenant, to ensure we have been granted the necessary permissions
                HttpClient client = new HttpClient();
                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, msGraphQuery);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.Token);
                HttpResponseMessage response = await client.SendAsync(request);

                // If we get back a 403, we need to ask the admin for permissions
                if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    daemonClient.AppTokenCache.Clear(Startup.clientId);
                    return new RedirectResult("/Account/GrantPermissions");
                }
                else if (!response.IsSuccessStatusCode)
                {
                    throw new HttpResponseException(response.StatusCode);
                }
            }
            catch (MsalException ex)
            {
                // If we can't get a token, we need to ask the admin for permissions as well
                if (ex.ErrorCode == "failed_to_acquire_token_silently")
                {
                    return new RedirectResult("/Account/GrantPermissions");
                }

                return View("Error");
            }
            catch (Exception ex)
            {
                return View("Error");
            }

            // If we can get a token & make the query, permissions have been granted and we can proceed to showing the list of users
            ViewBag.TenantId = tenantId;
            ViewBag.Users = SyncController.GetUsersForTenant(tenantId);

            return View();
        }
    }
}