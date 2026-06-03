# Product Requirements Document
## Algerian Phone Repair Shop Management System
**V1.0 · June 2026**

---

## 1. Overview

A small phone repair and parts shop in Algeria, staffed by one owner and two workers, currently manages repair jobs, spare parts inventory, and client payments through a combination of paper, phone calls, and memory. This causes lost job history, no inventory visibility, and no cash tracking.

The solution is a two-component system:

- A staff-facing web application for managing all shop operations
- A client-facing Telegram bot for browsing available parts and checking repair status

Both components share the same database. The web app is the single source of truth; the bot is a read-only window into it.

---

## 2. Goals

### V1 goals

- Replace paper and memory with a fast, reliable digital system for repair job tracking
- Give the owner visibility into shop activity without requiring daily involvement
- Let clients check their repair status independently via Telegram bot using their job code
- Let clients browse available parts by brand and model via Telegram bot
- Generate a printed receipt for every job containing the job code

### Out of scope for V1

- Client notifications (Telegram or WhatsApp message when repair is ready)
- Part sales without an associated repair job
- Monthly profit and loss reporting
- Worker performance analytics
- Offline / PWA mode
- Low stock alerts
- Supplier management

---

## 3. Users & Roles

| Role | Description |
|------|-------------|
| **Owner (1)** | Full access to all modules. Views the dashboard for a quick shop overview. Does not manage day-to-day operations but needs to glance at job status, payments, and inventory at any time. |
| **Worker (2)** | Creates and manages repair tickets, updates job status, manages inventory, records payments. Cannot access aggregate financial reports. |
| **Client (public)** | Uses the Telegram bot only. Can browse the parts catalog and look up their repair status by job code. Has no access to the web app. |

---

## 4. System Architecture

Two interfaces backed by a single shared database:

| Component | Description |
|-----------|-------------|
| **Web app** | Staff-only. Responsive, works on mobile browser and desktop. Login required. Arabic UI (RTL layout). |
| **Telegram bot** | Client-facing. Read-only. No login required. Clients interact via menus. Reads live data from the same database as the web app. |
| **Database** | Single source of truth. The web app writes data; the bot reads it. No sync complexity. |

### Tech stack

| Layer | Technology |
|-------|------------|
| Framework | ASP.NET Core with Razor Pages |
| Database | Microsoft SQL Server (MSSQL) |
| ORM | Entity Framework Core |
| Hosting | MonsterASP (free tier) |
| Bot | Telegram Bot API |

The web app and the Telegram bot share the same database and backend. The bot is an additional interface layer, not a separate system. Entity Framework Core handles all database interactions, keeping the data layer clean and consistent across both interfaces.

---

## 5. Core Modules — Web App

### 5.1 Repair Jobs

The central module. Every device that enters the shop gets a repair ticket.

#### Creating a ticket

When a client drops off a device, a worker creates a ticket with the following fields:

| Field | Details |
|-------|---------|
| Customer name | Required, free text |
| Customer phone | Required. Primary identifier for the customer record. |
| Customer WhatsApp / Telegram | Optional. Used for manual follow-up by staff. |
| Device brand | Selected from a predefined list (matches inventory hierarchy) |
| Device model | Selected from models under the chosen brand |
| Problem description | Free text |
| Assigned worker | Defaults to the logged-in worker; can be changed |
| Estimated price (DZD) | Numeric, editable later |
| Notes | Optional free text for anything extra |

#### Job status lifecycle

Workers update status as the job progresses. Each status change is timestamped.

```
New → In progress → Waiting for part → Done → Collected
```

The **Waiting for part** status pauses the job visually — these tickets appear in a distinct section on the jobs list so they are never forgotten. When the part arrives, the worker marks it received, logs the part against the ticket, and resumes the job.

---

### 5.2 Inventory

Parts are organised in a four-level hierarchy that mirrors how clients browse and how workers search:

| Level | Example |
|-------|---------|
| Brand | Samsung, Apple, Xiaomi |
| Model | Galaxy A54, iPhone 14, Redmi Note 12 |
| Part type | Screen, battery, back cover, charging port |
| Part variant | Original OEM, Compatible (third-party), Refurbished |

Each part record contains:

- Brand → Model → Part type → Variant (the hierarchy above)
- Quantity in stock
- Unit cost price (DZD) — what the shop paid
- Unit sale price (DZD) — what the shop charges
- Stocked or on-demand flag

When a worker uses a part on a repair job, they select it from inventory and the quantity decrements automatically. On-demand parts are ordered per job — when ordered, the job status moves to Waiting for part; when they arrive and are received, the quantity is briefly incremented then immediately decremented as it is consumed by the job.

---

### 5.3 Customers

Lightweight by design. Most clients are infrequent, so the customer record is minimal.

| Field | Details |
|-------|---------|
| Name | Required |
| Phone number | Required. Primary key — used to look up existing customers when creating a new ticket. |
| WhatsApp | Optional |
| Telegram handle | Optional |
| Repair history | Auto-generated — all tickets linked to this customer |

When creating a repair ticket, the worker searches by phone number first. If found, the customer is linked automatically. If not, a new record is created on the spot. No separate customer management screen is required for workers.

---

### 5.4 Payments

Per-job payment tracking. All amounts in DZD.

| Field | Details |
|-------|---------|
| Quoted price | Set at ticket creation, editable at any time before collection |
| Amount paid | Recorded when the client collects their device |
| Balance owed | Calculated automatically (quoted price minus amount paid) |
| Payment status | Unpaid / Partially paid / Fully paid — auto-derived |

The owner dashboard shows all jobs with a pending balance at a glance. No complex accounting — just what is owed and by whom.

---

### 5.5 Printable Receipt

Every repair ticket generates a printable receipt. This is a V1 requirement, not a nice-to-have — the job code on the receipt is the client's only way to look up their repair status on the Telegram bot.

#### Unified ticket + customer creation flow

The receipt is generated as part of a single unified screen — there is no separate step to create a customer first. The worker fills in customer details and job details on the same form and submits once. The receipt is generated immediately.

When the worker enters the client's phone number, the system checks for an existing customer record. If found, the customer's name and contact details auto-fill. If not, a new customer record is created automatically on submission. The worker never needs to navigate away to a separate customer screen.

#### Receipt contents

- Shop name and contact
- Job code — large and readable (format: `REP-XXXX`)
- Date and time
- Device brand and model
- Problem description
- Estimated price (DZD)
- Assigned worker name
- A short line pointing the client to the Telegram bot for status updates

---

### 5.6 Owner Dashboard

The owner does not manage day-to-day operations. The dashboard is a read-only glance view, not an action screen.

It shows:

- Open jobs by status — how many in each stage right now
- Jobs completed today
- Unpaid or partially paid jobs — amount and client name
- Recent inventory changes — parts added or consumed today

Reporting beyond this glance view (monthly profit, worker performance, low stock alerts) is explicitly deferred to V2.

---

## 6. Telegram Bot — Client-Facing

The bot is read-only. It has two functions: browsing the parts catalog and checking repair status. No login, no registration, no data is written through the bot.

### 6.1 Parts Catalog

The bot mirrors the inventory hierarchy defined in the web app. Only parts marked as visible and with quantity > 0 are shown.

Client flow:

1. Client starts the bot → main menu appears
2. Client selects: Browse parts
3. Bot presents list of available brands
4. Client selects a brand → bot presents models for that brand
5. Client selects a model → bot presents available part types
6. Client selects a part type → bot shows available variants with sale price

The bot does not show stock quantities — only whether something is available. Pricing is shown as a reference; final price may vary.

### 6.2 Repair Status Lookup

Client flow:

1. Client selects: Check my repair
2. Bot asks for job code (format: `REP-XXXX`, printed on their receipt)
3. Client enters the code
4. Bot returns: device model, current status, and assigned worker name
5. If the code is not found, the bot returns a friendly error and suggests calling the shop

No phone number is used in the bot. The job code on the receipt is the only lookup key. This protects client privacy and avoids ambiguity when a client has multiple jobs.

---

## 7. UI & Language

| Item | Detail |
|------|--------|
| Web app language | Arabic — full RTL layout throughout |
| Bot language | Arabic — all menus and messages in Arabic |
| Platform | Responsive web app — works on mobile browser and desktop/laptop |
| Authentication | Username and password — one account per staff member |
| Receipt language | Arabic |

---

## 8. V2 Roadmap (Deferred)

The following features are confirmed as valuable but deliberately excluded from V1 to keep the initial build focused and shippable:

| Feature | Notes |
|---------|-------|
| Client notifications | Telegram message when repair is done. Requires bot onboarding flow — client messages the bot once to register their Telegram ID against their phone number. |
| Part sales (no repair) | Direct sale of a part to a client without a repair job. Needs a simple sales module and receipt flow. |
| Monthly reporting | Profit overview, revenue by period, cost breakdown. Owner to define exact requirements after using V1. |
| Worker performance | Jobs completed per worker by week/month. |
| Low stock alerts | Automatic flag when a stocked part falls below a defined threshold. |
| Offline / PWA mode | Allow workers to create and update tickets without internet. Deferred due to sync complexity. |

---

## 9. Technical & Operational Decisions

All open questions have been resolved:

| Item | Decision |
|------|----------|
| **Hosting** | MonsterASP free tier |
| **Database** | Microsoft SQL Server (included with MonsterASP free tier) |
| **Tech stack** | ASP.NET Core + Razor Pages + Entity Framework Core |
| **Bot visibility** | Public — anyone can find and use the Telegram bot |
| **Initial data** | System starts from scratch. No customer list to import. Staff will enter parts manually as needed — inventory will reflect reality from day one. |
| **Receipt printing** | To be confirmed with staff before development. Determines receipt layout: A4 or thermal 80mm. |

---

*End of document — Phone Repair Shop PRD V1.0*
