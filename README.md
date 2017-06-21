---
services: active-directory
platforms: dotnet
author: jmprieur
---

# Build a multi-tenant daemon with the v2.0 endpoint
This sample application shows how to use the [Azure AD v2.0 endpoint](http://aka.ms/aadv2) to access the data of Microsoft business customers in a long-running, non-interactive process.  It uses the OAuth2 client credentials grant to acquire an access token which can be used to call the [Microsoft Graph](https://graph.microsoft.io) and access organizational data.

The app is built as an ASP.NET 4.5 MVC application, using the OWIN OpenID Connect middleware to sign-in users.  Its "daemon" component is simply an API controller which, when called, syncs a list of users from the customer's Azure AD tenant.  This `SyncController.cs` is triggered by an ajax call in the web application, and uses the preview Microsoft Authentication Library (MSAL) to perform token acquisition.

Because the app is a multi-tenant app intended for use by any Microsoft business customer, it must provide a way for customers to "sign up" or "connect" the application to their company data.  During the connect flow, a company administrator can grant **application permissions** directly to the app so that it can access company data in a non-interactive fashion, without the presence of a signed-in user.  The majority of the logic in this sample shows how to achieve this connect flow using the v2.0 **admin consent** endpoint.

For more information on the concepts used in this sample, be sure to read the [v2.0 endpoint client credentials protocol documentation](https://azure.microsoft.com/documentation/articles/active-directory-v2-protocols-oauth-client-creds).

> Looking for previous versions of this code sample? Check out the tags on the [releases](../../releases) GitHub page.

## Running the sample app
Follow the steps below to run the application and create your own multi-tenant daemon.  We reccommend using Visual Studio 2015 to do so.

### Register an app
Create a new app at [apps.dev.microsoft.com](https://apps.dev.microsoft.com), or follow these [detailed steps](https://docs.microsoft.com/azure/active-directory/develop/active-directory-v2-app-registration).  Make sure to:

- Copy down the **Application Id** assigned to your app, you'll need it soon.
- Add the **Web** platform for your app.
- Enter two **Redirect URI**s. The base URL for this sample, `https://localhost:44316/`, as well as `https://localhost:44316/Account/GrantPermissions`.  These are the locations which the v2.0 endpoint will be allowed to return to after authentication.
- Generate an **Application Secret** of the type **password**, and copy it for later.  Note that in production apps you should always use certificates as your application secrets, but for this sample we will use a simple shared secret password.

If you have an existing application that you have registered in the past, feel free to use that instead of creating a new registration.

### Configure your app for admin consent
In order to use the v2.0 admin consent endpoint, you'll need to declare the application permissions your app will use ahead of time.  While still in the registration portal,

- Locate the **Microsoft Graph Permissions** section on your app registration.
- Under **Application Permissions**, add the `User.Read.All` permission.
- Be sure to **Save** your app registration.

### Download & configure the sample code
You can download this repo as a .zip file using the button above, or run the following command:

`git clone https://github.com/Azure-Samples/active-directory-dotnet-daemon-v2.git`

Once you've downloaded the sample, open it using Visual Studio.  Open the `App_Start\Startup.Auth.cs` file, and replace the following values:

- Replace the `clientId` value with the application ID you copied above.
- Replace the `clientSecret` value with the application secret you copied above.

### Run the sample
Start the UserSync application, and begin by signing in as an administrator in your Azure AD tenant.  If you don't have an Azure AD tenant for testing, you can [follow these instructions](https://azure.microsoft.com/documentation/articles/active-directory-howto-tenant/) to get one.

When you sign in, the app will first ask you for permission to sign you in & read your user profile.  This allows the application to ensure that you are a business user.  The application will then try to sync a list of users from your Azure AD tenant via the Microsoft Graph.  If it is unable to do so, it will ask you (the tenant administrator) to connect your tenant to the application.

The application will then ask for permission to read the list of users in your tenant.  When you grant the permission, the application will then be able to query for users at any point.  You can verify this by clicking the **Sync Users** button on the users page, refreshing the list of users.  Try adding or removing a user and re-syncing the list (but note that it only syncs the first page of users!).

The relevant code for this sample is in the following files:

- Initial sign-in: `App_Start\Startup.Auth.cs`, `Controllers\AccountController.cs`
- Syncing the list of users to the local in-memory store: `Controllers\SyncController.cs`
- Displaying the list of users from the local in-memory store: `Controllers\UserController.cs`
- Acquiring permissions from the tenant admin using the admin consent endpoint: `Controllers\AccountController.cs`



