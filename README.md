# HR_system (Buy2 HR Management System)

Open-source enterprise HRMS with shift scheduling, geofenced attendance, gamification points, and digital rewards built on .NET 9/10 & Angular.

## Architecture

This project follows Clean Architecture principles divided into 4 .NET projects + Angular Frontend:

- src/Buy2.Domain/: Core domain entities, enums, exceptions.
- src/Buy2.Application/: Application DTOs, CQRS contracts, interfaces.
- src/Buy2.Infrastructure/: EF Core DbContext, entity configurations, repositories, authentication.
- src/Buy2.Api/: RESTful API Controllers & Middleware.
- src/Buy2.Frontend/: Angular 18+ web application workspace.

## Documentation & Contributor Guide

- **Contributor Task Backlog**: Read [AVAILABLE_TASKS.md](./AVAILABLE_TASKS.md) to claim your first atomic task!
- **Domain Glossary**: Read [CONTEXT.md](./CONTEXT.md) for canonical domain terms.
