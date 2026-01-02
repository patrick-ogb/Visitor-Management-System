# Technical Requirements Document (TRD)
## Notification Module – Visitor Management System (VMS)

---

## 1. Document Control

| Item | Description |
|---|---|
| Document Name | VMS Notification Module TRD |
| Version | 1.0 |
| Prepared By | — |
| Date | — |
| Related Document | Notification Module BRD |

---

## 2. Purpose

This Technical Requirements Document (TRD) describes the **technical design, components, data structures, and processing logic** for implementing the Notification Module within the Visitor Management System (VMS).

The implementation is constrained to **first-party infrastructure only**, using:
- .NET 8 Web API
- C#
- Blazor
- SQL Server
- In-process Background Services
- SignalR (built-in ASP.NET Core real-time framework)
- CQRS, Unit of Work, Repository patterns

---

## 3. Architectural Overview

### 3.1 Architectural Style

- Modular Monolith
- Event-driven (internally)
- Asynchronous background processing

### 3.2 High-Level Components

1. **Domain Events (CQRS)**
   - InvitationCreatedEvent
   - WalkInCreatedEvent

2. **Notification Background Service**
   - Polls and processes notification events
   - Performs aggregation
   - Triggers delivery (push / email)

3. **SignalR Infrastructure**
   - Maintains persistent connections
   - Handles real-time push notifications

4. **Persistence Layer (SQL Server)**
   - Notification Events
   - Notifications
   - Pending Notifications
   - User Presence

---

## 4. Component Design

### 4.1 Notification Event Producer

**Responsibility**:
- Publish notification events when business actions occur

**Source Modules**:
- Invitation Command Handler
- Walk-in Command Handler

**Implementation Detail**:
- Events are persisted to SQL Server within the same transaction as the business action

---

### 4.2 Notification Background Service

**Type**:
- .NET BackgroundService

**Responsibilities**:
- Poll unprocessed notification events
- Determine user presence
- Aggregate notifications
- Deliver notifications
- Send emails
- Mark events as processed

**Polling Strategy**:
- Interval configurable via appsettings (default: 5 seconds)

---

### 4.3 SignalR Hub Server

**Responsibilities**:
- Maintain authenticated client connections
- Track user online/offline status
- Push notifications in real time

**Integration**:
- Hosted within the same ASP.NET Core application

---

### 4.4 Presence Management

**Mechanism**:
- WebSocket connection lifecycle
- Periodic heartbeat from client (30 seconds)

**Failure Handling**:
- If heartbeat not received within timeout → mark user offline

---

## 5. Data Model (Logical)

### 5.1 NotificationEvent

| Field | Type | Description |
|---|---|---|
| Id | GUID | Primary Key |
| EventType | string | Invitation / WalkIn |
| ReferenceId | GUID | InvitationId / WalkInId |
| TargetUserId | GUID | Recipient |
| Processed | bit | Processing status |
| CreatedAt | datetime | Event creation time |

---

### 5.2 Notification

| Field | Type | Description |
|---|---|---|
| Id | GUID | Primary Key |
| UserId | GUID | Recipient |
| Type | string | Notification type |
| Message | nvarchar | Display message |
| IsRead | bit | Read status |
| CreatedAt | datetime | Creation timestamp |

---

### 5.3 PendingNotification

| Field | Type | Description |
|---|---|---|
| Id | GUID | Primary Key |
| UserId | GUID | Recipient |
| ReferenceId | GUID | Business reference |
| Type | string | Notification type |
| CreatedAt | datetime | Creation timestamp |

---

### 5.4 UserPresence

| Field | Type | Description |
|---|---|---|
| UserId | GUID | User |
| IsOnline | bit | Online status |
| LastHeartbeat | datetime | Last heartbeat time |

---

## 6. Processing Logic

### 6.1 Event Processing Flow

1. Fetch unprocessed NotificationEvents
2. For each event:
   - Check UserPresence
   - Aggregate notification if applicable
   - Deliver notification if user online
   - Store pending notification if offline
   - Send email if required
   - Persist Notification
   - Mark event as processed

---

### 6.2 Aggregation Logic

**Rules**:
- Same user
- Same notification type
- Within configurable time window (default: 5 minutes)

**Result**:
- Single summarized notification message

---

### 6.3 Email Processing

- Email sending executed asynchronously
- SMTP configuration stored in application settings
- Failures logged and retried

---

## 7. CQRS Integration

### 7.1 Commands

- CreateNotificationEventCommand
- MarkNotificationAsReadCommand

### 7.2 Queries

- GetUnreadNotificationsQuery
- GetNotificationHistoryQuery

---

## 8. Security Requirements

- SignalR connections require authentication tokens
- Authorization checks before delivery
- Notifications scoped strictly to target user

---

## 9. Error Handling & Logging

- Background service failures logged
- Retry logic for transient failures
- Dead-letter pattern via status flags

---

## 10. Configuration Parameters

| Key | Description | Default |
|---|---|---|
| NotificationPollingInterval | Background worker interval | 5s |
| AggregationWindowMinutes | Notification aggregation window | 5 |
| HeartbeatIntervalSeconds | Client heartbeat | 30 |

---

## 11. Performance Considerations

- Indexed lookup on TargetUserId
- Batch event processing
- Optimistic concurrency handling

---

## 12. Deployment Considerations

- Background service enabled by default
- SignalR hub endpoints exposed securely
- SQL migrations required

---

## 13. Traceability Matrix

| BRD Requirement | TRD Section |
|---|---|
| Real-time push | 4.3 |
| Offline handling | 6.1 |
| Aggregation | 6.2 |
| Email notification | 6.3 |

---

## 14. Future Enhancements

- Redis-based presence store
- Distributed background workers
- Notification preference management

---

**End of Document**

