# Task List

This file tracks PRD-driven vertical functional slices. Each slice should produce manually usable business value, not just technical setup.

## Slice Review

- [x] Owner confirms slice granularity feels right.
- [x] Owner confirms dependencies are correct.
- [x] Owner approves implementation order.

## Implementation Order

1. Staff authentication and Arabic RTL shell
2. Repair intake, customer lookup, and receipt
3. Job status workflow
4. Inventory catalog and stock consumption
5. Payment tracking
6. Owner dashboard
7. Telegram parts catalog
8. Telegram repair status lookup

## Functional Slices

### 1. Staff Sign In and Arabic RTL App Shell

Status: Implemented - Needs Owner Manual Validation  
Type: HITL  
Blocked by: None  
Business Value: Restricts the web app to shop staff and establishes the Arabic responsive interface required by the PRD.

Acceptance Criteria:
- [x] Owner and worker accounts can sign in with username and password.
- [x] Authenticated users see an Arabic RTL layout on desktop and mobile.
- [x] Workers cannot access owner-only dashboard data.
- [x] Unauthenticated users are redirected to sign in.

### 2. Create Repair Ticket With Customer Lookup and Receipt

Status: Not Started  
Type: HITL  
Blocked by: Staff Sign In and Arabic RTL App Shell  
Business Value: Replaces paper intake with a single flow that creates or links a customer, creates a repair job, and generates the printed job-code receipt.

Acceptance Criteria:
- [ ] Worker enters customer phone first and existing details auto-fill when found.
- [ ] New customer records are created during ticket submission when no phone match exists.
- [ ] Ticket captures device brand, model, problem, assigned worker, estimated price, and notes.
- [ ] New tickets receive a readable `REP-XXXX` job code.
- [ ] Receipt shows shop details, job code, date/time, device, problem, estimate, worker, and Telegram status guidance.

### 3. Manage Repair Status Workflow

Status: Not Started  
Type: HITL  
Blocked by: Create Repair Ticket With Customer Lookup and Receipt  
Business Value: Lets workers track every job from intake to collection with timestamped status history.

Acceptance Criteria:
- [ ] Jobs move through `New`, `In progress`, `Waiting for part`, `Done`, and `Collected`.
- [ ] Each status change is timestamped.
- [ ] Jobs waiting for parts appear in a distinct section.
- [ ] Workers can resume a waiting job when the required part arrives.

### 4. Manage Inventory Catalog and Stock Consumption

Status: Not Started  
Type: HITL  
Blocked by: Manage Repair Status Workflow  
Business Value: Gives staff visibility into parts by brand, model, type, and variant, and keeps stock accurate when parts are used on repairs.

Acceptance Criteria:
- [ ] Staff can manage brand, model, part type, variant, quantity, cost price, sale price, and stocked/on-demand flag.
- [ ] Workers can attach a stocked part to a repair job.
- [ ] Stock quantity decrements automatically when a part is consumed.
- [ ] On-demand parts can move a job to `Waiting for part`, then be received and consumed.

### 5. Record Job Payments and Balances

Status: Not Started  
Type: AFK  
Blocked by: Create Repair Ticket With Customer Lookup and Receipt  
Business Value: Tracks what each client owes and whether each repair is unpaid, partially paid, or fully paid.

Acceptance Criteria:
- [ ] Quoted price is set at intake and editable before collection.
- [ ] Amount paid is recorded when the device is collected.
- [ ] Balance owed is calculated automatically.
- [ ] Payment status is derived as unpaid, partially paid, or fully paid.

### 6. Show Owner Glance Dashboard

Status: Not Started  
Type: AFK  
Blocked by: Manage Repair Status Workflow; Record Job Payments and Balances; Manage Inventory Catalog and Stock Consumption  
Business Value: Gives the owner a read-only snapshot of shop activity without entering day-to-day workflows.

Acceptance Criteria:
- [ ] Dashboard shows open job counts by status.
- [ ] Dashboard shows jobs completed today.
- [ ] Dashboard shows unpaid or partially paid jobs with client and amount owed.
- [ ] Dashboard shows recent inventory changes from today.

### 7. Browse Available Parts in Telegram

Status: Not Started  
Type: HITL  
Blocked by: Manage Inventory Catalog and Stock Consumption  
Business Value: Lets clients browse currently available parts by brand and model without contacting staff.

Acceptance Criteria:
- [ ] Bot main menu offers a parts browsing option in Arabic.
- [ ] Client can browse brand, model, part type, and variant menus.
- [ ] Bot only shows visible parts with quantity greater than zero.
- [ ] Bot shows sale price but not stock quantity.
- [ ] Bot does not write to the database.

### 8. Check Repair Status in Telegram

Status: Not Started  
Type: HITL  
Blocked by: Manage Repair Status Workflow  
Business Value: Lets clients use the receipt job code to check repair progress without exposing private customer lookup data.

Acceptance Criteria:
- [ ] Bot asks for job code in `REP-XXXX` format.
- [ ] Valid job code returns device model, current status, and assigned worker name in Arabic.
- [ ] Unknown job code returns a friendly error and suggests calling the shop.
- [ ] Bot does not use phone number for lookup.
- [ ] Bot does not write to the database.

## Manual Validation Needed

Manual validation steps must be added to the session handoff after each implemented slice. Include pages or bot flows to open, input data to use, expected results, and PRD edge cases checked.
