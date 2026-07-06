# 🚀 HotSync

HotSync is a real-time collaboration platform built with ASP.NET Core that synchronizes users, conversations, and notifications across devices using Microsoft Azure services.

The application authenticates users through **Microsoft Entra ID (Azure Active Directory)** using OAuth 2.0 and OpenID Connect, providing secure Single Sign-On (SSO). After successful authentication, the backend acquires Microsoft Graph access tokens to interact with Microsoft 365 resources.

To efficiently synchronize data, HotSync leverages the **Microsoft Graph Delta Query API**, which returns a **delta link** after the initial synchronization. Instead of downloading all data repeatedly, subsequent sync operations use this delta link to fetch only the incremental changes, significantly improving performance and reducing API calls.

The backend is developed using **ASP.NET Core**, **Entity Framework Core**, and **SQL Server**, while **SignalR** enables real-time communication between connected users. JWT tokens are used to secure API endpoints, and Swagger provides interactive API documentation for testing.

### Workflow
1. User signs in with Microsoft Entra ID.
2. Azure issues an ID Token and Access Token.
3. The backend calls Microsoft Graph APIs.
4. Initial synchronization retrieves all required resources.
5. A Delta Link is stored for future synchronization.
6. Future syncs use the Delta Link to fetch only changed data.
7. SignalR instantly broadcasts updates to connected clients.

### Tech Stack
- ASP.NET Core 8
- C#
- Entity Framework Core
- SQL Server
- Microsoft Entra ID (Azure AD)
- Microsoft Graph API
- Delta Query (Delta Links)
- SignalR
- JWT Authentication
- Swagger
