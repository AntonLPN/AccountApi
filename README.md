Account API (Distributed User Account & Identity Management System)
A production-ready, highly scalable, and resilient backend microservice implementing Domain-Driven Design (DDD), CQRS, and Event-Driven Architecture (EDA). This project serves as a showcase of engineering practices, focusing on robust lifecycle management, explicit domain boundaries, fault tolerance, and asynchronous process orchestration for user management.

Known Tradeoffs

At the current stage, AppUser is linked to IdentityUser to maintain the stability of the production registration and authentication flow without risky Big Bang refactoring.

This is a conscious compromise:

Pros: Lower risk of regressions, faster functionality delivery.
Cons: Some infrastructure dependencies creep into the domain model.

⚙️ Core Architecture & Design Patterns
The system is built on the pillars of Clean Architecture combined with tactical and strategic DDD patterns, ensuring the identity and profile business logic remains isolated, testable, and independent of infrastructure.

1. Domain-Driven Design (DDD)
Rich Domain Model: Zero anemic models. The Account aggregate encapsulates its entire lifecycle (e.g., Draft, PendingVerification, Active, Suspended). State transitions are strictly enforced via internal business rules, preventing invalid states.
Aggregate Roots & Boundaries: Strict transactional boundaries. Modifications to user profiles, security settings, or preferences are routed exclusively through the Account Aggregate Root.
Value Objects: Avoidance of Primitive Obsession. Concepts like Email, PhoneNumber, and AccountStatus are modeled as immutable Value Objects with built-in validation rules.
Domain Events: Used to decouple side-effects within the same context (e.g., raising an AccountRegisteredDomainEvent to trigger internal password hashing or default preference generation).
2. CQRS (Command Query Responsibility Segregation)
The application strictly separates write operations (Commands) from read operations (Queries) to optimize performance and scalability:

Write Side: Handled via commands that transition the account state, validated using FluentValidation pipelines before hitting the domain.
Read Side: Highly optimized projections for user profiles and account statuses. While this project uses Entity Framework Core for both sides as a baseline, the architecture allows a seamless drop-in replacement for raw SQL/Dapper on the query side without affecting business logic.


3. Event-Driven Architecture (EDA) & Messaging
MassTransit over RabbitMQ: Leveraged as the enterprise service bus for reliable, asynchronous communication.
Integration Events: Used for cross-context communication (e.g., notifying external services like Notification API or Analytics API when an account status changes).
Idempotent Consumers: Every message consumer implements deduplication strategies to guarantee idempotency and handle network retries gracefully.


5. Distributed Workflows via Saga Pattern
MassTransit State Machine Saga: Orchestrates the complex, multi-stage User Onboarding Workflow. Registration requires multiple asynchronous steps: initial data entry, identity verification (mocked external service), and welcome flow triggers.
Compensating Actions: Built-in rollback and fallback mechanisms. If identity verification fails or a duplicate check triggers a violation late in the process, the Saga gracefully transitions the account to a Rejected or RolledBack state.


7. Advanced Infrastructure & Data Resiliency
Database First-Class Citizen: MySQL database managed entirely via EF Core Code-First migrations.
Transactional Outbox / Inbox Pattern: Guarantees At-Least-Once Delivery and prevents dual-write problems. Integration events are persisted to the database within the same ACID transaction as the account state change and published asynchronously.
Resiliency Pipelines: Configured MassTransit retry configurations, exponential backoffs, and Dead-Letter Exchanges (DLX) for robust error handling.


🛠️ Technology Stack
Runtime: .NET 8 

Frameworks: ASP.NET Core Web API, Entity Framework Core

Database: MySQL (Object-Relational Mapping & Migrations)

Authentication: Keycloak

CQRS: MediatR

Service Bus: RabbitMQ

Abstraction Layer: MassTransit (Sagas, Outbox, Inboxes, Retries)

Testing: xUnit, FluentAssertions, Moq/NSubstitute, WebApplicationFactory (for integration tests)
🏗️ Project Structure & Clean Architecture Layers
src/

├── Account.Domain/          # Pure Domain Layer (Aggregates, Value Objects, Domain Events, Repository Interfaces)

├── Account.Application/     # CQRS Commands/Queries, Handlers, DTOs, Event Consumers, Saga State Machines

├── Account.Infrastructure/  # DbContext, EF Configurations, Migrations, MassTransit Setup, External Services

└── AccountApi/         # Entry Point: Controllers, Middlewares, Configuration, Program.cs

---

## 📄 License & Commercial Usage

Copyright © 2026. All rights reserved.

This software is **proprietary** and closed-source. It is **not** licensed under any open-source license (such as MIT, Apache, or GPL). 

* **Restrictions:** You are strictly prohibited from copying, modifying, redistributing, or using this codebase for commercial production, white-labeling, or reselling as an independent API service without acquiring an explicit commercial license or written permission from the copyright owner.
* **Intended Use:** This repository is published strictly for demonstration, code-review, and educational purposes.

For licensing inquiries, B2B integrations, or custom feature deployment, please contact the author directly.
