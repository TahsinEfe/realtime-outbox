# Realtime Outbox

A learning-focused .NET project that demonstrates the Outbox Pattern with real-time delivery.

## Purpose

The primary goal of this repository is to learn how these pieces work together in one flow:

- PostgreSQL
- RabbitMQ
- ASP.NET Core `BackgroundService`
- SignalR (real-time fanout)
- Clean separation of concerns

## What This Project Implements

This solution is split into focused projects:

- `src/RealtimeOutbox.ChatService`
- `src/RealtimeOutbox.OutboxWorker`
- `src/RealtimeOutbox.RealtimeGateway`
- `src/RealtimeOutbox.Contracts`
- `infra/docker-compose.yml`

### 1) PostgreSQL

`ChatService` writes business data (`messages`) and integration data (`outbox_events`) into PostgreSQL. The message and outbox record are persisted in the same transaction so event publication can happen reliably later.

### 2) RabbitMQ

`OutboxWorker` polls unsent rows from `outbox_events` and publishes them to RabbitMQ (`outbox_events` exchange / `outbox_events_queue`). This decouples API write latency from message delivery latency.

### 3) ASP.NET Core BackgroundService

Both worker-style responsibilities are implemented with hosted/background services:

- Outbox polling and publishing in `OutboxWorker`
- RabbitMQ consuming in `RealtimeGateway`

This models real production behavior where async workloads run continuously in the background.

### 4) SignalR (real-time fanout)

`RealtimeGateway` consumes RabbitMQ messages and pushes payloads to connected SignalR clients (`/hubs/chat`).

- Broadcast path: `outbox.test`
- Group-based path scaffold: `chat.message.created` (tenant/channel group model)

A simple browser test client exists at `HTML/SignalR.html`.

### 5) Clean Separation of Concerns

The solution separates responsibilities by service boundary:

- **ChatService**: request handling + transactional writes
- **OutboxWorker**: reliable async publication
- **RealtimeGateway**: real-time fanout to clients
- **Contracts**: shared event contracts

This keeps each project focused and easier to reason about independently.

## End-to-End Flow

1. Client sends `POST /api/messages` to `ChatService`.
2. `ChatService` writes `messages` and `outbox_events` in one transaction.
3. `OutboxWorker` reads pending outbox rows.
4. `OutboxWorker` publishes events to RabbitMQ.
5. `RealtimeGateway` consumes RabbitMQ messages.
6. `RealtimeGateway` sends updates to SignalR clients.

## Prerequisites

- .NET 9 SDK
- Docker Desktop (or Docker Engine + Compose)

## Run Locally

### 1. Start infrastructure

```bash
docker compose -f infra/docker-compose.yml up -d
```

This starts:

- PostgreSQL on `localhost:5433`
- RabbitMQ on `localhost:5672`
- RabbitMQ management UI on `http://localhost:15672`

### 2. Apply database migrations

```bash
dotnet ef database update --project src/RealtimeOutbox.ChatService --startup-project src/RealtimeOutbox.ChatService
```

### 3. Run services in separate terminals

```bash
dotnet run --project src/RealtimeOutbox.ChatService
dotnet run --project src/RealtimeOutbox.OutboxWorker
dotnet run --project src/RealtimeOutbox.RealtimeGateway
```

Default local URLs:

- ChatService: `http://localhost:5178`
- RealtimeGateway: `http://localhost:5214`

### 4. Send a test message

Example request to `ChatService`:

```http
POST http://localhost:5178/api/messages
Content-Type: application/json

{
  "tenantId": "11111111-1111-1111-1111-111111111111",
  "channelId": "22222222-2222-2222-2222-222222222222",
  "senderUserId": "33333333-3333-3333-3333-333333333333",
  "content": "Hello from outbox"
}
```

### 5. Observe real-time output

Open `HTML/SignalR.html` in a browser and monitor incoming events.

## Notes

- This repository is intentionally built as a learning project, not a production-hardened template.
- For real deployments, keep credentials in environment variables or secret stores (not in tracked config files).
