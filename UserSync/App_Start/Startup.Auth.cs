using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.IdentityModel.Protocols;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Notifications;
using Microsoft.Owin.Security.OpenIdConnect;
using Owin;

namespace UserSync
{
    public partial class Startup
    {
        public static string clientId = "[Enter your client ID, as obtained from the app registration portal]";
        public static string clientSecret = "[Enter your client secret, as obtained from the app registration portal]";
        private static string authority = "https://login.microsoftonline.com/common/v2.0";
        public static string redirectUri = "https://localhost:44316/";

        private void ConfigureAuth(IAppBuilder app)
        {   
            app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(new CookieAuthenticationOptions());

            app.UseOpenIdConnectAuthentication(new OpenIdConnectAuthenticationOptions {
                ClientId = clientId,
                Authority = authority,
                RedirectUri = redirectUri,
                PostLogoutRedirectUri = redirectUri,
                Scope = "openid profile",
                ResponseType = "id_token",
                TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    NameClaimType = "name"
                },
                Notifications = new OpenIdConnectAuthenticationNotifications
                {
                    AuthenticationFailed = OnAuthenticationFailed,
                    SecurityTokenValidated = OnSecurityTokenValidated,
                }
            });
        }

        private Task OnSecurityTokenValidated(SecurityTokenValidatedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
        {
            // Make sure that the user didn't sign in with a personal Microsoft account
            if (notification.AuthenticationTicket.Identity.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value == "9188040d-6c67-4c5b-b112-36a304b66dad")
            {
                notification.HandleResponse();
                notification.Response.Redirect("/Account/UserMismatch");
            }

            return Task.FromResult(0);
        }

        private Task OnAuthenticationFailed(AuthenticationFailedNotification<OpenIdConnectMessage, OpenIdConnectAuthenticationOptions> notification)
        {
            notification.HandleResponse();
            notification.Response.Redirect("/Home/Error");
            return Task.FromResult(0);
        }
    }
}