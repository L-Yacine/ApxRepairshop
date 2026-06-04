# Session Handoff

## Current State

The repository is an ASP.NET Core MVC project for the Algerian phone repair shop PRD. Slices 1, 2, and 3 have passed owner manual checks. Slices 4 and 5 have been implemented together and need owner migration/build/run/manual validation.

Git has been initialized with a first commit, but earlier Slice 2 and Slice 3 work is still uncommitted/untracked in this workspace. Slice 4 and 5 changes are layered on top of that state.

## Last Completed

- Implemented Slice 4 inventory catalog and stock consumption:
  - Added inventory part model with brand, model, part type, variant, quantity, cost price, sale price, and stocked/on-demand flag.
  - Added inventory stock movement model for consumed and on-demand received activity.
  - Added `/Inventory` catalog list.
  - Added inventory create/edit form.
  - Linked inventory from the authenticated navbar and home page.
  - Added stocked part consumption from the repair ticket details page.
  - Stocked part consumption decrements inventory quantity automatically.
  - Added on-demand part request from the repair ticket details page.
  - On-demand part request moves the job to `Waiting for part`.
  - Added on-demand receive/consume action, which logs receive and consume stock movements and resumes the job to `In progress`.
- Implemented Slice 5 payment tracking:
  - Added `AmountPaid` to repair tickets.
  - Repair detail page now shows agreed price, amount paid, balance owed, and derived payment status.
  - Added payment update form on repair details.
  - Agreed price remains editable before collection and is locked after `Collected`.
  - Balance owed is calculated from agreed price minus amount paid.
  - Payment status is derived as unpaid, partially paid, or fully paid.
- Updated `TASKS.md`:
  - Slice 3 marked owner validated.
  - Slices 4 and 5 marked implemented pending owner manual validation.

## Next Steps

1. Owner scaffolds and applies a migration for Slice 4/5 schema changes.
2. Owner builds and runs the app.
3. Owner manually validates Slice 4 and Slice 5 checks below.
4. If validation passes, start Slice 6: Owner glance dashboard.

## Blockers

- No current coding blocker.
- A schema migration is required before running the updated app because new inventory/payment tables and fields were added.
- Before production, replace temporary seeded staff credentials with owner-managed password setup.

## Owner Commands Needed

Run these from the repo root:

```bash
dotnet ef migrations add AddInventoryAndPayments --project MimoShop --startup-project MimoShop
dotnet ef database update --project MimoShop --startup-project MimoShop
dotnet build MimoShop.slnx
dotnet run --project MimoShop
```

If dependencies are missing on a clean machine, run restore first:

```bash
dotnet restore MimoShop.slnx
```

## Manual Validation Needed

Open the local URL shown by `dotnet run --project MimoShop`.

### Slice 4 checks

1. Open inventory catalog:
   - Log in as `worker1` / `worker123`.
   - Open `/Inventory` from the navbar or home page.
   - Expected: Arabic RTL inventory page loads with an empty state or existing parts table.

2. Add a stocked part:
   - Click `إضافة قطعة`.
   - Enter brand/model matching an existing repair ticket device, for example `Samsung` / `Galaxy A54`.
   - Enter part type `Screen`, variant `Compatible`, quantity `2`, cost price `5000`, sale price `9000`, keep stocked checked.
   - Save.
   - Expected: part appears in `/Inventory` as `مخزنة` with quantity `2`.

3. Consume stocked part on a repair:
   - Create or open a repair ticket for the same brand/model.
   - Open the ticket details page.
   - Under `قطع الغيار`, select the stocked part, quantity `1`, and click `استهلاك`.
   - Expected: success message appears, part is listed under `قطع مستخدمة في الصيانة`, and `/Inventory` quantity decreases from `2` to `1`.

4. Add an on-demand part:
   - Open `/Inventory`, add another part for the same brand/model.
   - Use part type `Battery`, variant `Original`, quantity `0`, prices as desired, and uncheck stocked.
   - Expected: part appears as `عند الطلب`.

5. Request on-demand part:
   - Open the repair ticket details page.
   - Select the on-demand part, quantity `1`, and click `طلب القطعة`.
   - Expected: success message appears, ticket status becomes `بانتظار قطعة`, and the ticket appears in the waiting section on `/RepairTickets`.

6. Receive on-demand part:
   - On the same ticket details page, click `استلام واستهلاك` for the pending part.
   - Expected: part row changes to received/consumed, status changes to `قيد العمل`, status history gets a new timestamped row, and `/RepairTickets` no longer shows the ticket in the waiting section.

### Slice 5 checks

1. Initial payment state:
   - Open any ticket details page.
   - Expected: payment summary shows agreed price from intake, paid amount `0`, balance equal to agreed price, and status `غير مدفوع`.

2. Partial payment:
   - Set paid amount lower than agreed price and save.
   - Expected: paid amount updates, balance decreases, and status becomes `مدفوع جزئياً`.

3. Full payment:
   - Set paid amount equal to agreed price and save.
   - Expected: balance becomes `0` and status becomes `مدفوع بالكامل`.

4. Quote edit before collection:
   - While ticket status is not `تم التسليم`, change the agreed price and save.
   - Expected: agreed price updates and balance/payment status recalculates.

5. Quote locked after collection:
   - Set ticket status to `تم التسليم`.
   - Reopen details page.
   - Expected: agreed price field is read-only; saving payment updates paid amount but does not change agreed price.

6. Mobile layout:
   - Resize browser to a phone-sized viewport.
   - Expected: inventory table scrolls horizontally, repair detail payment/part forms stack cleanly, and no text overlaps.

## Important Rules

- Follow `AGENTS.md`.
- Do not run package installs or `dotnet` restore/build/run/test commands in the container.
- Do not add automated tests.
- For schema changes, ask the owner to scaffold migrations instead of hand-writing migrations.
