---
languages:
- csharp
- powershell
- javascript
- html
page_type: sample
description: "Use the Microsoft identity platform endpoint to access the data of Microsoft business customers in a long-running, non-interactive process."
products:
- azure
- azure-active-directory
- dotnet
urlFragment: build-multi-tenant-daemon-aad
---

# Build a multi-tenant daemon with the Microsoft identity platform endpoint

![Build Badge](https://identitydivision.visualstudio.com/_apis/public/build/definitions/a7934fdd-dcde-4492-a406-7fad6ac00e17/33/badge)

> For a simpler console daemon application, check out the following sample: [active-directory-dotnetcore-daemon-v2](https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2)

> We have renamed the default branch to main. To rename your local repo follow the directions [here](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-branches-in-your-repository/renaming-a-branch#updating-a-local-clone-after-a-branch-name-changes).

## About this sample

### Overview

This sample application shows how to use the [Microsoft identity platform endpoint](http://aka.ms/aadv2) to access the data of Microsoft business customers in a long-running, non-interactive process. It uses the [OAuth2 client credentials grant](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-client-creds-grant-flow) to acquire an access token, which it then uses to call the [Microsoft Graph](https://graph.microsoft.io) and access organizational data.

The app is built as an ASP.NET MVC application, and uses  the OWIN OpenID Connect middleware to sign in users.  Its "daemon" component in this sample is just an API controller, which, when called, pulls in a list of users  in customer's Azure AD tenant from Microsoft Graph.  This `SyncController.cs` is triggered by an AJAX call in the web application, and uses the [Microsoft Authentication Library (MSAL) for .NET](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet) to acquire an access token for Microsoft Graph.

## Scenario

Because the app is a multi-tenant app intended for use by any Microsoft business customer, it must provide a way for customers to "sign up" or "connect" the application to their company data.  During the connect flow, a company administrator first grants **application permissions** directly to the app so that it can access company data in a non-interactive fashion, without the presence of a signed-in user.  The majority of the logic in this sample shows how to achieve this connect flow using the identity platform [admin consent](https://docs.microsoft.com/azure/active-directory/develop/v2-permissions-and-consent#using-the-admin-consent-endpoint) endpoint.

![Topology](./ReadmeFiles/topology.png)

For more information on the concepts used in this sample, be sure to read the [identity platform endpoint client credentials protocol documentation](https://docs.microsoft.com/azure/active-directory/develop/v2-oauth2-client-creds-grant-flow).

- Developers who wish to gain good familiarity of programming for Microsoft Graph are advised to go through the [An introduction to Microsoft Graph for developers](https://www.youtube.com/watch?v=EBbnpFdB92A) recorded session. 

> Looking for previous versions of this code sample? Check out the tags on the [releases](../../releases) GitHub page.

## How to run this sample

To run this sample, you'll need:

- [Visual Studio 2017](https://aka.ms/vsdownload)
- An Internet connection
- An Azure Active Directory (Azure AD) tenant. For more information on how to get an Azure AD tenant, see [How to get an Azure AD tenant](https://azure.microsoft.com/en-us/documentation/articles/active-directory-howto-tenant/)
- One or more user accounts in your Azure AD tenant. This sample will not work with a Microsoft account (formerly Windows Live account). Therefore, if you signed in to the [Azure portal](https://portal.azure.com) with a Microsoft account and have never created a user account in your directory before, you need to do that now.

### Step 1:  Clone or download this repository

From your shell or command line:

```Shell
git clone https://github.com/Azure-Samples/active-directory-dotnet-daemon-v2.git
```

or download and exact the repository .zip file.

> Given that the name of the sample is pretty long, and so are the name of the referenced NuGet packages, you might want to clone it in a folder close to the root of your hard drive, to avoid file size limitations on Windows.

### Step 2:  Register the sample application with your Azure Active Directory tenant

There is one project in this sample. To register it, you can:

- either follow the steps [Step 2: Register the sample with your Azure Active Directory tenant](#step-2-register-the-sample-with-your-azure-active-directory-tenant) and [Step 3:  Configure the sample to use your Azure AD tenant](#choose-the-azure-ad-tenant-where-you-want-to-create-your-applications)
- or use PowerShell scripts that:
  - **automatically** creates the Azure AD applications and related objects (passwords, permissions, dependencies) for you
  - modify the Visual Studio projects' configuration files.

If you want to use this automation:

1. On Windows, run PowerShell and navigate to the root of the cloned directory
1. In PowerShell run:

   ```PowerShell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope Process -Force
   ```

1. Run the script to create your Azure AD application and configure the code of the sample application accordingly.
1. In PowerShell run:

   ```PowerShell
   .\AppCreationScripts\Configure.ps1
   ```

   > Other ways of running the scripts are described in [App Creation Scripts](./AppCreationScripts/AppCreationScripts.md)

1. Open the Visual Studio solution and click start to run the code.

If you don't want to use this automation, follow the steps below.

#### Choose the Azure AD tenant where you want to create your applications

As a first step you'll need to:

1. Sign in to the [Azure portal](https://portal.azure.com) using either a work or school account or a personal Microsoft account.
1. If your account is present in more than one Azure AD tenant, select your profile at the top right corner in the menu on top of the page, and then **switch directory**.
   Change your portal session to the desired Azure AD tenant.


#### Register the client app (dotnet-web-daemon-v2)

1. Navigate to the Microsoft identity platform for developers [App registrations](https://go.microsoft.com/fwlink/?linkid=2083908) page.
1. Select **New registration**.
1. When the **Register an application page** appears, enter your application's registration information:
   - In the **Name** section, enter a meaningful application name that will be displayed to users of the app, for example `dotnet-web-daemon-v2`.
   - In the **Supported account types** section, select **Accounts in any organizational directory**.
   - In the Redirect URI (optional) section, select **Web** in the combo-box and enter the following redirect URIs.
       - `https://localhost:44316/`
       - `https://localhost:44316/Account/GrantPermissions`
    > Note that if there are more than one redirect URIs, you'd need to add them from the **Authentication** tab later after the app has been created successfully.
1. Select **Register** to create the application.
1. On the app **Overview** page, find the **Application (client) ID** value and record it for later. You'll need it to configure the Visual Studio configuration file for this project.
1. In the list of pages for the app, select **Authentication**.
   - In the **Advanced settings** section set **Logout URL** to `https://localhost:44316/Account/EndSession`
   - In the **Advanced settings** | **Implicit grant** section, check **Access tokens** and **ID tokens** as this sample requires the [Implicit grant flow](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-implicit-grant-flow) to be enabled to sign-in the user, and call an API.
1. Select **Save**.
1. From the **Certificates & secrets** page, in the **Client secrets** section, choose **New client secret**:

   - Type a key description (of instance `app secret`),
   - Select a key duration of either **3 months**, **12 months**, **18 months**, **24 months** or **Recommended: 6 months**.
   - When you press the **Add** button, the key value will be displayed, copy, and save the value in a safe location.
   - You'll need this key later to configure the project in Visual Studio. This key value will not be displayed again, nor retrievable by any other means,
     so record it as soon as it is visible from the Azure portal.
1. In the list of pages for the app, select **API permissions**
   - Click the **Add a permission** button and then,
   - Ensure that the **Microsoft APIs** tab is selected
   - In the *Commonly used Microsoft APIs* section, click on **Microsoft Graph**
   - In the **Application permissions** section, ensure that the right permissions are checked: **User.Read.All**
   - Select the **Add permissions** button

### Step 3:  Configure the sample to use your Azure AD tenant

In the steps below, "ClientID" is the same as "Application ID" or "AppId".

Open the solution in Visual Studio to configure the projects

#### Configure the client project

> Note: if you used the setup scripts, the changes below will have been applied for you

1. Open the `UserSync\Web.Config` file
1. Find the app key `ida:ClientId` and replace the existing value with the application ID (clientId) of the `dotnet-web-daemon-v2` application copied from the Azure portal.
1. Find the app key `ida:ClientSecret` and replace the existing value with the key you saved during the creation of the `dotnet-web-daemon-v2` app, in the Azure portal.

### Run the sample

Clean the solution, rebuild the solution, and run  UserSync application, and begin by signing in as an administrator in your Azure AD tenant.  If you don't have an Azure AD tenant for testing, you can [follow these instructions](https://azure.microsoft.com/documentation/articles/active-directory-howto-tenant/) to get one.

When you sign in, the app will first ask you for permission to sign you in & read your user profile.  This consent allows the application to ensure that you are a business user.

![User Consent](./ReadmeFiles/FirstConsent.PNG)

The application will then try to sync a list of users from your Azure AD tenant, via the Microsoft Graph.  If it is unable to do so, it will ask you (the tenant administrator) to connect your tenant to the application.

The application will then ask for permission to read the list of users in your tenant.

![Admin Consent](./ReadmeFiles/adminconsent.PNG)

**You will be signed out from the app after granting permission**. This is done to ensure that any existing access tokens for Graph is removed from the token cache. Once you sign in again, the  fresh token obtained will have the necessary permissions to make calls to MS Graph.
When you grant the permission, the application will then be able to query for users at any point.  You can verify this by clicking the **Sync Users** button on the users page, refreshing the list of users.  Try adding or removing a user and re-syncing the list (but note that it only syncs the first page of users!).

> [Consider taking a moment to share your experience with us.](https://forms.office.com/Pages/ResponsePage.aspx?id=v4j5cvGGr0GRqy180BHbR73pcsbpbxNJuZCMKN0lURpUMDVTMEM4SzA1N0FZR1lCQzE5S1lXN0ZLUSQlQCN0PWcu)

## About the code

The relevant code for this sample is in the following files:

- Initial sign-in: `App_Start\Startup.Auth.cs`, `Controllers\AccountController.cs`.  In particular, the actions on the controller have an Authorize attribute, which forces the user to sign in. The application uses the [authorization code flow](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/wiki/Acquiring-tokens-with-authorization-codes-on-web-apps) to sign in the user.
- Syncing the list of users to the local in-memory store: `Controllers\SyncController.cs`
- Displaying the list of users from the local in-memory store: `Controllers\UserController.cs`
- Acquiring permissions from the tenant admin using the admin consent endpoint: `Controllers\AccountController.cs`

## How to recreate this sample

### Code for the service

1. In Visual Studio 2017, create a new `Visual C#` `ASP.NET Web Application (.NET Framework)` project. In the next screen, choose the `MVC` project template. Also add folder and core references for `Web API` as you would be adding a Web API controller later.  Leave the project's chosen authentication mode as the default, that is, `No Authentication`".
2. Select the project in the **Solution Explorer** window and press the **F4** key to bring project properties. In the project properties, set **SSL Enabled** to be `True`. Note the **SSL URL**. You will need it when configuring this application's registration in the Azure portal.
3. Add the following ASP.Net OWIN middleware NuGets: `Microsoft.Owin.Security.ActiveDirectory`, `Microsoft.Owin.Security.Cookies` and `Microsoft.Owin.Host.SystemWeb`, `Microsoft.IdentityModel.Protocol.Extensions`, `Microsoft.Owin.Security.OpenIdConnect` and `Microsoft.Identity.Client`. 
4. In the `App_Start` folder, create a class `Startup.Auth.cs`.You will need to remove `.App_Start` from the namespace name.  Replace the code for the `Startup` class with the code from the same file of the sample app.  Be sure to take the whole class definition!  The definition changes from `public class Startup` to `public partial class Startup`
5. In `Startup.Auth.cs` resolve missing references by adding `using` statements as suggested by Visual Studio IntelliSense.
6. Right-click on the project, select Add, select "Class", and in the search box enter "OWIN".  "OWIN Startup class" will appear as a selection; select it, and name the class `Startup.cs`.
7. In `Startup.cs`, replace the code for the `Startup` class with the code from the same file of the sample app.  Again, note the definition changes from `public class Startup` to `public partial class Startup`.
8. In the  folder, add a new class called `MsGraphUser.cs`.  Replace the implementation with the contents of the file of the same name from the sample.
9. Add a new **MVC 5 Controller - Empty** called `AccountController`. Replace the implementation with the contents of the file of the same name from the sample.
10. Add a new **MVC 5 Controller - Empty** called `UserController`. Replace the implementation with the contents of the file of the same name from the sample.
11. Add a new **Web API 2 Controller - Empty** called `SyncController`. Replace the implementation with the contents of the file of the same name from the sample.
12. For the user interface, in the `Views\Account` folder, add three **Empty (without model) Views** named `GrantPermissions`, `Index` and `UserMismatch` and one named `Index` in the `Views\User` folder. Replace the implementation with the contents of the file of the same name from the sample.
13. Update the `Shared\_Layout.cshtml` and `Home\Index.cshtml` to correctly link the various views together.

## How to deploy this sample to Azure

This project has one WebApp / Web API projects. To deploy them to Azure Web Sites, you'll need, for each one, to:

- create an Azure Web Site
- publish the Web App / Web APIs to the web site, and
- update its client(s) to call the web site instead of IIS Express.

### Create and publish the `dotnet-web-daemon-v2` to an Azure Web Site

1. Sign in to the [Azure portal](https://portal.azure.com).
1. Click `Create a resource` in the top left-hand corner, select **Web** --> **Web App**, and give your web site a name, for example, `dotnet-web-daemon-v2-contoso.azurewebsites.net`.
1. Thereafter select the `Subscription`, `Resource Group`, `App service plan and Location`. `OS` will be **Windows** and `Publish` will be **Code**.
1. Click `Create` and wait for the App Service to be created.
1. Once you get the `Deployment succeeded` notification, then click on `Go to resource` to navigate to the newly created App service.
1. Once the web site is created, locate it it in the **Dashboard** and click it to open **App Services** **Overview** screen.
1. From the **Overview** tab of the App Service, download the publish profile by clicking the **Get publish profile** link and save it.  Other deployment mechanisms, such as from source control, can also be used.
1. Switch to Visual Studio and go to the dotnet-web-daemon-v2 project.  Right click on the project in the Solution Explorer and select **Publish**.  Click **Import Profile** on the bottom bar, and import the publish profile that you downloaded earlier.
1. Click on **Configure** and in the `Connection tab`, update the Destination URL so that it is a `https` in the home page URL, for example [https://dotnet-web-daemon-v2-contoso.azurewebsites.net](https://dotnet-web-daemon-v2-contoso.azurewebsites.net). Click **Next**.
1. On the Settings tab, make sure `Enable Organizational Authentication` is NOT selected.  Click **Save**. Click on **Publish** on the main screen.
1. Visual Studio will publish the project and automatically open a browser to the URL of the project.  If you see the default web page of the project, the publication was successful.

### Update the Active Directory tenant application registration for `dotnet-web-daemon-v2`

1. Navigate back to to the [Azure portal](https://portal.azure.com).
In the left-hand navigation pane, select the **Azure Active Directory** service, and then select **App registrations**.
1. In the resultant screen, select the `dotnet-web-daemon-v2` application.
1. In the **Authentication** | page for your application, update the Logout URL fields with the address of your service, for example [https://dotnet-web-daemon-v2-contoso.azurewebsites.net](https://dotnet-web-daemon-v2-contoso.azurewebsites.net)
1. From the *Branding* menu, update the **Home page URL**, to the address of your service, for example [https://dotnet-web-daemon-v2-contoso.azurewebsites.net](https://dotnet-web-daemon-v2-contoso.azurewebsites.net). Save the configuration.
1. Add the same URL in the list of values of the *Authentication -> Redirect URIs* menu. If you have multiple redirect URLs, make sure that there a new entry using the App service's URI for each redirect URL.

## Troubleshooting

If you get an HTTP 403, even after admin consent, you might have chosen **Delegated permissions** for **User.Read.All** instead of **Application permissions**.

## Community Help and Support

Use [Stack Overflow](http://stackoverflow.com/questions/tagged/msal) to get support from the community.
Ask your questions on Stack Overflow first and browse existing issues to see if someone has asked your question before.
Make sure that your questions or comments are tagged with [`adal` `msal` `dotnet`].

If you find and bug in the sample, please raise the issue on [GitHub Issues](../../issues).

If you find a bug in msal.Net, please raise the issue on [MSAL.NET GitHub Issues](https://github.com/AzureAD/microsoft-authentication-library-for-dotnet/issues).

To provide a recommendation, visit the following [User Voice page](https://feedback.azure.com/forums/169401-azure-active-directory).

## Contributing

If you'd like to contribute to this sample, see [CONTRIBUTING.MD](/CONTRIBUTING.md).

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/). For more information, see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## More information

For more information, see MSAL.NET's conceptual documentation:

- [Tenancy in Azure Active Directory](https://docs.microsoft.com/en-us/azure/active-directory/develop/single-and-multi-tenant-apps)
- [Understanding Azure AD application consent experiences](https://docs.microsoft.com/en-us/azure/active-directory/develop/application-consent-experience)
- [How to: Sign in any Azure Active Directory user using the multi-tenant application pattern](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-convert-app-to-be-multi-tenant)
- [Understand user and admin consent](https://docs.microsoft.com/en-us/azure/active-directory/develop/howto-convert-app-to-be-multi-tenant#understand-user-and-admin-consent)
- [Application and service principal objects in Azure Active Directory](https://docs.microsoft.com/en-us/azure/active-directory/develop/app-objects-and-service-principals)
- [Quickstart: Register an application with the Microsoft identity platform](https://docs.microsoft.com/azure/active-directory/develop/quickstart-register-app)
- [Quickstart: Configure a client application to access web APIs](https://docs.microsoft.com/azure/active-directory/develop/quickstart-configure-app-access-web-apis)
- [Acquiring a token for an application with client credential flows](https://aka.ms/msal-net-client-credentials)

For more information about the underlying protocol:

- [Microsoft identity platform and the OAuth 2.0 client credentials flow](https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-client-creds-grant-flow)

For a simpler multi-tenant console daemon application, see [active-directory-dotnetcore-daemon-v2](https://github.com/Azure-Samples/active-directory-dotnetcore-daemon-v2)
