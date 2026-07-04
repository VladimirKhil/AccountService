# AccountService

Centralized user account and external authentication service.

## Features

- Provider-independent user id
- Steam authentication (initial provider)
- JWT cookie session
- User profile update (username, avatar, gender)
- Soft-delete with delayed hard purge
- Admin validation endpoint for internal services

## Projects

- src/AccountService - ASP.NET Core service
- src/AccountService.Database - LinqToDB + migrations
- src/AccountService.Contract - Contracts
- src/AccountService.Client - .NET client
- test/AccountService.IntegrationTests - integration tests
- web - TypeScript client
- helm/src - Helm chart

## Build

- dotnet build AccountService.slnx
