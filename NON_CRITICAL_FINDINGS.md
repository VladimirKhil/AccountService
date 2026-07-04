# Non-Critical Findings (Remaining)

Updated: 2026-07-04

This file tracks issues that are not critical blockers but should be addressed.

## Medium

- Login error mapping currently returns 400 for all InvalidOperationException in the Steam login flow, including service/config failures.
  - File: src/AccountService/Program.cs
  - Why it matters: clients cannot reliably distinguish invalid credentials from upstream/service failures.

- Steam service returns raw exception messages in API error payloads.
  - File: src/AccountService/Services/SteamAuthorizationService.cs
  - Why it matters: implementation details may leak to clients.

- Profile update input is not validated before persistence (username emptiness/length and avatar payload size are not constrained at service layer).
  - Files: src/AccountService/Services/AccountManager.cs, src/AccountService.Database/Migrations/202607010001_Initial.cs
  - Why it matters: risk of bad data, avoidable DB errors, and large payload abuse.

## Low

- Admin basic auth secret comparison is plain string equality (not constant-time).
  - File: src/AccountService/Middlewares/AdminAuthMiddleware.cs
  - Why it matters: minor timing side-channel risk on admin endpoint.

- Development settings include a hardcoded admin secret.
  - File: src/AccountService/appsettings.Development.json
  - Why it matters: accidental secret reuse risk if copied into non-dev environments.
