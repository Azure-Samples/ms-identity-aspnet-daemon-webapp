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
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using System;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using UserSync.Utils;

namespace UserSync.Controllers
{
    public class AccountController : Controller
    {
        private const string AuthorityFormat = "https://login.microsoftonline.com/{0}/v2.0";
        private const string adminConsentUrlFormat = "https://login.microsoftonline.com/{0}/adminconsent?client_id={1}&redirect_uri={2}";

        // GET: Account
        public ActionResult Index()
        {
            return View();
        }

        public void SignIn()
        {
            HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties { RedirectUri = "/User" }, OpenIdConnectAuthenticationDefaults.AuthenticationType);
        }

        public void SignOut()
        {
            this.RemovedCachedTokensForApp();
            string callbackUrl = Url.Action("SignOutCallback", "Account", routeValues: null, protocol: Request.Url.Scheme);

            HttpContext.GetOwinContext().Authentication.SignOut(
                new AuthenticationProperties { RedirectUri = callbackUrl },
                OpenIdConnectAuthenticationDefaults.AuthenticationType, CookieAuthenticationDefaults.AuthenticationType);
        }

        public ActionResult UserMismatch()
        {
            return View();
        }

        [Authorize]
        public ActionResult GrantPermissions(string admin_consent, string tenant, string error, string error_description)
        {
            // If there was an error getting permissions from the admin. ask for permissions again
            if (error != null)
            {
                this.RemovedCachedTokensForApp();
                ViewBag.ErrorDescription = error_description;
            }

            // If the admin successfully granted permissions, continue to showing the list of users
            else if (admin_consent == "True" && tenant != null)
            {
                // Do a full Sign-out, so that the logged in user can obtain a fresh token
                string signOutUrl = Url.Action("SignOut", "Account", routeValues: null, protocol: Request.Url.Scheme);
                return new RedirectResult(signOutUrl);
            }

            return View();
        }

        [Authorize]
        public ActionResult RequestPermissions()
        {
            return new RedirectResult(
                String.Format(adminConsentUrlFormat,
                ClaimsPrincipal.Current.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value,
                Startup.clientId,
                HttpUtility.UrlEncode(Startup.redirectUri + "Account/GrantPermissions")));
        }

        /// <summary>
        /// Called by Azure AD. Here we end the user's session, but don't redirect to AAD for sign out.
        /// </summary>
        public void EndSession()
        {
            this.RemovedCachedTokensForApp();
            HttpContext.GetOwinContext().Authentication.SignOut(CookieAuthenticationDefaults.AuthenticationType);
        }

        public ActionResult SignOutCallback()
        {
            if (this.Request.IsAuthenticated)
                return this.RedirectToAction("Index", "Home");

            return this.View();
        }

        /// <summary>
        /// Clears all cached tokens obtained and cached for the app itself. 
        /// If you have scenarios like on-behalf-of which results in the user token cache caching tokens for users as well, that'd be cleared up here as well
        /// </summary>
        private void RemovedCachedTokensForApp()
        {
            string tenantId = ClaimsPrincipal.Current?.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value;

            if(string.IsNullOrEmpty(tenantId))
            {
                return;
            }

            IConfidentialClientApplication daemonClient;
            daemonClient = ConfidentialClientApplicationBuilder.Create(Startup.clientId)
                .WithAuthority(string.Format(AuthorityFormat, tenantId))
                .WithRedirectUri(Startup.redirectUri)
                .WithClientSecret(Startup.clientSecret)
                .Build();
        }
    }
}