# Web Client Guide: Steam Authorization And Cookie Issuance

This guide explains how a web client can authorize a user with Steam and get the AccountService auth cookie.

## Recommended client package

Prefer using the npm package `accountservice-client`.

Install:

```bash
npm install accountservice-client
```

The package already uses `credentials: "include"` for requests, which is required for cookie-based auth.

## What the endpoint expects

AccountService login endpoint:

- `POST /api/v1/auth/steam`
- JSON body: `{ "authTicket": "..." }`

The `authTicket` must be a Steam Web API auth ticket that can be validated by Steam `AuthenticateUserTicket`.

## End-to-end flow

1. Web client obtains a Steam auth ticket from Steam runtime/SDK.
2. Web client calls AccountService `POST /api/v1/auth/steam` with that ticket.
3. AccountService validates ticket with Steam and issues auth cookie.
4. Browser stores cookie and sends it on later API calls.

## Important platform note

Pure browser JavaScript cannot directly produce the required Steam auth ticket by itself.

You need a Steam-capable runtime path, for example:

- Steam client + game runtime with Steamworks APIs
- desktop shell (Electron, CEF, game overlay webview) that bridges Steamworks to JS

If your frontend is only a normal public website, use a separate Steam web sign-in flow on backend and then create AccountService session in your backend.

## Step 1: obtain Steam auth ticket

Exact API depends on your Steamworks binding. Pseudocode:

```ts
const identity = "account-service";
const authTicket = await steamBridge.getAuthTicketForWebApi(identity);

if (!authTicket) {
  throw new Error("Failed to obtain Steam auth ticket");
}
```

Notes:

- `identity` should match AccountService Steam identity configuration.
- Ticket lifetime is short, send it to AccountService immediately.

## Step 2: call login endpoint and receive cookie

Use `credentials: "include"` so browser accepts/sends cookies.

```ts
const response = await fetch("https://your-account-service-host/api/v1/auth/steam", {
  method: "POST",
  credentials: "include",
  headers: {
    "Content-Type": "application/json",
  },
  body: JSON.stringify({ authTicket }),
});

if (!response.ok) {
  throw new Error(`Steam login failed with status ${response.status}`);
}

const payload = await response.json();
console.log("Authorized user id:", payload.userId);
```

On success, AccountService sets the auth cookie (default name `account_auth`).

Cookie settings currently used by AccountService:

- HttpOnly = true
- Secure = true
- SameSite = Strict
- Path = /

## Step 3: call authenticated endpoints with cookie

```ts
const meResponse = await fetch("https://your-account-service-host/api/v1/account/me", {
  method: "GET",
  credentials: "include",
});

if (meResponse.status === 401) {
  throw new Error("Not authenticated");
}

if (!meResponse.ok) {
  throw new Error(`Get profile failed with status ${meResponse.status}`);
}

const me = await meResponse.json();
console.log(me);
```

## Using the accountservice-client npm package

```ts
import { AccountServiceClient } from "accountservice-client";

const client = new AccountServiceClient("https://your-account-service-host");

const login = await client.loginBySteamAsync({ authTicket });
console.log(login.userId);

const me = await client.getMeAsync();
console.log(me);
```

## Using local source client (development fallback)

If you use `web/src/AccountServiceClient.ts`, it already sets `credentials: "include"`.

```ts
import { AccountServiceClient } from "./AccountServiceClient";

const client = new AccountServiceClient("https://your-account-service-host");

const login = await client.loginBySteamAsync({ authTicket });
console.log(login.userId);

const me = await client.getMeAsync();
console.log(me);
```

## Cookie delivery requirements

To actually receive and send the cookie, all of the following must be true:

1. Requests use HTTPS (cookie is `Secure`).
2. Requests include credentials (`credentials: "include"`).
3. Frontend and AccountService are same-site friendly for `SameSite=Strict` behavior.

Because `SameSite=Strict` is currently used, cross-site frontend hosting will usually not work for cookie-based auth without server-side changes.

## Troubleshooting

- Login returns 400 with Steam auth error:
  - ticket format is wrong or expired
  - identity/app id mismatch
- Login returns 500 with Steam config message:
  - AccountService Steam API key or app id is not configured
- Login is 200 but cookie is missing:
  - not using HTTPS
  - missing `credentials: "include"`
  - browser blocked cookie due to cross-site + `SameSite=Strict`
- Auth works in one environment but not another:
  - host/domain/site boundary changed
  - reverse proxy strips or rewrites `Set-Cookie`

## Security notes

- Keep Steam ticket exchange strictly over HTTPS.
- Do not expose cookie value to JavaScript (cookie is HttpOnly by design).
- Do not store Steam auth ticket for long-term reuse.
