# Microsoft Entra ID sign-in for Contoso.WebApp

This project is configured to authenticate users with Microsoft Entra ID (formerly Azure AD) using Microsoft.Identity.Web.

## Configure app registration

1. Create an app registration in Microsoft Entra ID (single tenant is fine).
2. Note the following values:
   - Tenant ID
   - Application (client) ID
3. Add a Redirect URI (Web) pointing to `https://localhost:7163/signin-oidc` or the HTTPS port you run on (check `Properties/launchSettings.json`).
4. If you use a certificate for confidential client credentials, upload it and copy its thumbprint; otherwise, you can remove the `ClientCredentials` section and configure a client secret instead.

## Update appsettings

Edit `appsettings.json` (and `appsettings.Development.json`) under `Contoso.WebApp/`:

```
"AzureAd": {
  "Instance": "https://login.microsoftonline.com/",
  "TenantId": "<your-tenant-id>",
  "ClientId": "<your-client-id>",
  "CallbackPath": "/signin-oidc"
},
"DownstreamApis": {
  "MicrosoftGraph": {
    "BaseUrl": "https://graph.microsoft.com/v1.0/",
    "RelativePath": "me",
    "Scopes": ["user.read"]
  }
}
```

If you don't plan to call Graph or other APIs with on-behalf-of tokens, you can remove the `DownstreamApis` section and the token acquisition in `Program.cs`.

## Sign-in and sign-out

The app uses the built-in UI from `Microsoft.Identity.Web.UI`:
- Sign in: link in the navbar → `MicrosoftIdentity/Account/SignIn`
- Sign out: link in the navbar → `MicrosoftIdentity/Account/SignOut`

All Razor Pages are protected by default via an `AuthorizeFilter`. For public pages, add `[AllowAnonymous]` to the corresponding PageModel.

## Calling the backend API

The `HttpClient` for `IContosoAPI` automatically attaches the user's access token via `AuthHandler`. Ensure your API is configured to validate the incoming JWT (issuer/audience) or adjust to accept Microsoft Entra tokens if you switch your API to AAD JWTs.

## Run locally

1. Restore & build:

```bash
cd Contoso.WebApp
dotnet restore
dotnet build
```

2. Run the WebApp from VS Code or:

```bash
dotnet run
```

Open the HTTPS URL shown in the console, sign in, and verify access to pages.
