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

Status: Implemented - Owner Validated
Type: HITL  
Blocked by: None  
Business Value: Restricts the web app to shop staff and establishes the Arabic responsive interface required by the PRD.

Acceptance Criteria:
- [x] Owner and worker accounts can sign in with username and password.
- [x] Authenticated users see an Arabic RTL layout on desktop and mobile.
- [x] Workers cannot access owner-only dashboard data.
- [x] Unauthenticated users are redirected to sign in.

### 2. Create Repair Ticket With Customer Lookup and Receipt

Status: Implemented - Owner Validated
Type: HITL
Blocked by: Staff Sign In and Arabic RTL App Shell
Business Value: Replaces paper intake with a single flow that creates or links a customer, creates a repair job, and generates the printed job-code receipt.

Acceptance Criteria:
- [x] Worker enters customer phone first and existing details auto-fill when found.
- [x] New customer records are created during ticket submission when no phone match exists.
- [x] Ticket captures device brand, model, problem, assigned worker, estimated price, and notes.
- [x] New tickets receive a readable `REP-XXXX` job code.
- [x] Receipt shows shop details, job code, date/time, device, problem, estimate, worker, and Telegram status guidance.

### 3. Manage Repair Status Workflow

Status: Implemented - Owner Validated
Type: HITL  
Blocked by: Create Repair Ticket With Customer Lookup and Receipt  
Business Value: Lets workers track every job from intake to collection with timestamped status history.

Acceptance Criteria:
- [x] Jobs move through `New`, `In progress`, `Waiting for part`, `Done`, and `Collected`.
- [x] Each status change is timestamped.
- [x] Jobs waiting for parts appear in a distinct section.
- [x] Workers can resume a waiting job when the required part arrives.

### 4. Manage Inventory Catalog and Stock Consumption

Status: Implemented - Owner Validated
Type: HITL
Blocked by: Manage Repair Status Workflow
Business Value: Gives staff visibility into parts by brand, model, type, and variant, and keeps stock accurate when parts are used on repairs.

Acceptance Criteria:
- [x] Staff can manage brand, model, part type, variant, quantity, cost price, sale price, and stocked/on-demand flag.
- [x] Workers can attach a stocked part to a repair job.
- [x] Stock quantity decrements automatically when a part is consumed.
- [x] On-demand parts can move a job to `Waiting for part`, then be received and consumed.

### 4b. Inventory Hierarchy as Lookup Tables

Status: Implemented - Owner Validated
Type: HITL
Blocked by: Manage Inventory Catalog and Stock Consumption
Business Value: Replaces free-text brand/model/part-type/variant fields on inventory parts with curated lookup tables, eliminating duplicate parts caused by typos, and gives the staff a single place to manage the shop's catalog vocabulary.

Acceptance Criteria:
- [x] `Brand`, `PhoneModel`, `PartType`, `PartVariant` lookup tables with unique indexes on `Name`.
- [x] `InventoryPart` uses foreign keys to all four lookups instead of free-text strings.
- [x] Lookup values seed with the PRD examples (Samsung/Apple/Xiaomi/etc. brands; Screen/Battery/etc. types; OEM/Compatible/Refurbished variants).
- [x] `/Categories` admin page lets workers add, edit, and activate/deactivate any lookup row.
- [x] Inventory add/edit form uses dropdowns instead of text inputs, with cascading Brand to PhoneModel via a JSON endpoint.
- [x] Disabling a lookup row hides it from inventory and intake dropdowns without breaking existing part records.
- [x] `RepairPartUsage` keeps the snapshot string fields (renamed to `*Name`) for historical accuracy.
- [x] Repair ticket intake form now sources its brand/model dropdowns from the same lookup tables, removing the previously hardcoded list.

### 5. Record Job Payments and Balances

Status: Implemented - Owner Validated
Type: AFK
Blocked by: Create Repair Ticket With Customer Lookup and Receipt
Business Value: Tracks what each client owes and whether each repair is unpaid, partially paid, or fully paid.

Acceptance Criteria:
- [x] Quoted price is set at intake and editable before collection.
- [x] Amount paid is recorded when the device is collected.
- [x] Balance owed is calculated automatically.
- [x] Payment status is derived as unpaid, partially paid, or fully paid.

### 6. Show Owner Glance Dashboard

Status: Implemented - Owner Validated
Type: AFK
Blocked by: Manage Repair Status Workflow; Record Job Payments and Balances; Manage Inventory Catalog and Stock Consumption
Business Value: Gives the owner a read-only snapshot of shop activity without entering day-to-day workflows.

Acceptance Criteria:
- [x] Dashboard shows open job counts by status.
- [x] Dashboard shows jobs completed today.
- [x] Dashboard shows unpaid or partially paid jobs with client and amount owed.
- [x] Dashboard shows recent inventory changes from today.

### 7. Browse Available Parts in Telegram

Status: Implemented - Owner Validated
Type: HITL
Blocked by: Manage Inventory Catalog and Stock Consumption
Business Value: Lets clients browse currently available parts by brand and model without contacting staff.

Acceptance Criteria:
- [x] Bot main menu offers a parts browsing option in Arabic.
- [x] Client can browse brand, model, part type, and variant menus.
- [x] Bot only shows visible parts with quantity greater than zero.
- [x] Bot shows sale price but not stock quantity.
- [x] Bot does not write to the database.
- [x] Bot attaches the relevant catalog thumbnail (brand/model/type/variant) as a photo on every menu step when an image is available, uploaded by streaming the stored file from wwwroot.
- [x] Chat stays clean: only one active menu message per chat at any time; stray text typed by the user is deleted silently instead of spawning duplicate menus.

### 8. Check Repair Status in Telegram

Status: Implemented - Needs Owner Manual Validation
Type: HITL  
Blocked by: Manage Repair Status Workflow  
Business Value: Lets clients use the receipt job code to check repair progress without exposing private customer lookup data.

Acceptance Criteria:
- [x] Bot asks for job code in `REP-XXXX` format.
- [x] Valid job code returns device model, current status, and assigned worker name in Arabic.
- [x] Unknown job code returns a friendly error and suggests calling the shop.
- [x] Bot does not use phone number for lookup.
- [x] Bot does not write to the database.
- [x] The user's typed job code message is deleted right after submission so the status result sits cleanly below the prompt.

### 17. Telegram Bot Image Rendering and Flow Cleanup

Status: Superseded by task #18
Type: HITL
Blocked by: Browse Available Parts in Telegram
Business Value: Makes the Telegram bot visually richer (catalog images at every menu step) and keeps the chat tidy (one menu message at a time, no stray duplicate menus when users type text).

Acceptance Criteria:
- [x] Catalog thumbnails are uploaded to Telegram via `InputFile.FromStream` reading the stored file under `wwwroot/uploads/catalog`; no public base URL is required.
- [x] Brands, models, part types, and variants listings each attempt to attach the first available image/thumbnail for that listing; fallbacks walk model/brand images when the immediate entity lacks one.
- [x] When no image is available, the menu is sent as a plain text message (existing behavior).
- [x] Navigation between menu levels uses a delete-then-resend pattern so at most one menu message per chat exists.
- [x] A per-chat semaphore serializes updates to prevent duplicate menus under rapid taps.
- [x] `/start` deletes the previous active menu message before sending a fresh welcome menu.
- [x] Stray non-`/start` text (with no pending status input) is deleted silently; no new menu is spawned.
- [x] Status input flow deletes the user's typed job code message before editing the prompt into the result.
- [x] `ChatMenuState` singleton tracks the active menu message id per chat and provides per-chat locks.
- [x] `LookupItem` and `VariantItem` DTOs now carry `ImageUrl`/`ThumbnailUrl`; catalog query services populate them from the corresponding entities.

### 18. Telegram Bot Menu Professionalization

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: Telegram Bot Image Rendering and Flow Cleanup
Business Value: Turns the bot from confusing/random/slow into a clean industry-standard shop menu: deterministic per-screen images, inline variant price list, one-time stray-text hint, shop contact action, and consolidated DB queries.

Acceptance Criteria:
- [x] Welcome menu shows shop name (from DB-backed `ShopSettingsService`) plus a 3-bullet value prop and three buttons: تصفح / تتبع / تواصل معنا.
- [x] Brands step is text-only (no random brand-logo photo attached to the listing).
- [x] Models step attaches the selected brand's own image (fallback: text-only when brand has no image).
- [x] Part types step attaches the selected model's own image (fallback: brand's image; then text-only).
- [x] Variants step attaches the part image (existing part→model→brand fallback in the query) and renders all variants inline in the caption as `✓ {name} — 💰 {price} د.ج`; no per-variant buttons.
- [x] Variants caption ends with the price-estimate note + shop phone contact line from `ShopSettingsService`.
- [x] Breadcrumb uses Arabic shop name from DB settings: `📍 <b>{shopName}</b> › {brand} › {model} › {type}`.
- [x] Back buttons are screen-specific: 🔙 العلامات / 🔙 الموديلات / 🔙 الأنواع; 🏠 الرئيسية on every list screen.
- [x] "📞 تواصل معنا" welcome button opens a Telegram alert popup with phone + conditional WhatsApp (hidden when empty) + address from `ShopSettingsService`.
- [x] Stray non-`/start` text: user's message is deleted; a one-line hint is replied once per navigation cycle; subsequent strays are deleted silently; any `/start` or navigation button clears the hint flag so the next stray prompts again.
- [x] DB queries consolidated: Models/PartTypes/Variants navigation each fires a single DB round-trip (parent name + parent image projected in the same query); removed the extra `GetBrandAsync`/`GetModelAsync`/`GetPartTypeAsync` lookups from the handler path.
- [x] `ModelLookupItem`, `PartTypeLookupItem`, and extended `VariantItem` DTOs carry parent name + image fields; `LookupItem` (brands) drops its now-unused image fields from the handler's concerns.
- [x] `ChatMenuState` gains `IsHinted`/`MarkHinted`/`ClearHint`; `Clear(chatId)` also clears the hint flag.
- [x] `TelegramBotCallback.Contact` added; `TelegramBotCallback.PriceNote` removed (no per-variant buttons).
- [x] Bot remains read-only and never writes to the database.

### 9. Replace Top Bar With Modern Dark Sidebar

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: Staff Sign In and Arabic RTL App Shell
Business Value: Replaces the sticky top bar with a persistent dark editorial sidebar (mobile: drawer with hamburger) so navigation, brand, user info, and logout are always one glance away. Makes the app feel more modern and easier to use for the counter worker.

Acceptance Criteria:
- [x] Sidebar is persistent on desktop (≥768px) and pinned to the start side in RTL.
- [x] Brand block, primary nav, secondary nav, and user/logout block all live inside the sidebar — no top bar on desktop.
- [x] Active link is highlighted with a tinted teal background, teal text and icon, and a 3px teal stripe on the start edge.
- [x] Waiting-count badge on the الحالات item is preserved and still tracks live data from the workflow service.
- [x] Owner-only لوحة المالك item is still gated by `User.IsInRole("Owner")`.
- [x] Below 768px the sidebar becomes a drawer toggled by a hamburger button; closes on backdrop, link, and Escape.
- [x] Hamburger animates into an X when the drawer is open.
- [x] Print stylesheet hides the sidebar, hamburger, backdrop, and footer so the receipt prints cleanly.
- [x] Login screen is unaffected (still uses `_AuthLayout`, no sidebar).
- [x] No controller, service, model, or schema changes. No new NuGet packages.
- [x] Sidebar is **completely hidden** on mobile when the drawer is closed (no vertical strip, no shadow fringe). Implemented via `visibility: hidden` on the base rule plus a delayed `transition: visibility 0s var(--t-drawer)` so the slide-out animation finishes before the element is removed from rendering, and `overflow-x: hidden` on `html, body` inside `@media (max-width: 767.98px)` as a belt-and-suspenders guard.
- [x] **Exactly one** nav item is highlighted at a time across both the sidebar and the bottom nav. `الحالات` highlights only on `RepairTickets/Index`, `Details`, and `Receipt`; `استقبال صيانة` highlights only on `RepairTickets/Create`. Implemented via an explicit allow-list in the `isRepairArea` helper at the top of `_Layout.cshtml`.

### 10. Add Mobile Icon-Only Bottom Nav

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: Replace Top Bar With Modern Dark Sidebar
Business Value: Gives mobile users one-thumb access to the four main sections via a floating icon-only pill, on top of the existing drawer. Reduces friction on the counter worker's phone.

Acceptance Criteria:
- [x] Bottom nav shows on screens <768px and hides on desktop.
- [x] Contains exactly 4 icon-only items: استقبال صيانة, الحالات, المخزون, التصنيفات.
- [x] لوحة المالك (owner-only) is reachable only via the drawer, not the bottom nav.
- [x] Each item is a 48×48 touch target with a 22×22 Lucide-style icon.
- [x] Active item is highlighted with a tinted teal background, teal border, and soft teal glow that matches the sidebar's active treatment.
- [x] Bottom nav hides (fades and slides down) when the mobile drawer is open.
- [x] `padding-bottom: 96px` reserved on `.app-content` on mobile so content does not slide under the pill.
- [x] `env(safe-area-inset-bottom)` margin keeps the pill above the home indicator on notched phones.
- [x] Print stylesheet hides the bottom nav and resets the content padding.
- [x] No controller, service, model, or schema changes. No new packages.

### 11. Redesign Footer and Polish Layout Deadspace

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: Add Mobile Icon-Only Bottom Nav
Business Value: Adds a professional, industrial-brutalist system status footer for shop staff, displaying dynamic diagnostics (Telegram Bot status, Database connection) and dynamic year. Solves the layout deadspace on short pages and the mobile bottom margin overlap.

Acceptance Criteria:
- [x] Simple placeholder footer replaced with a technical Option A (Industrial Brutalist) specification sheet markup.
- [x] Includes dynamic year copyright, support links, active database connection indicator, and pulsing green Telegram Bot status indicator.
- [x] On desktop/low content height, page container stretches to fill available viewport height, anchoring the footer at the bottom.
- [x] On mobile, the 96px bottom nav buffer is shifted from the content wrapper to the footer itself, eliminating the canvas-colored gap while keeping text visible above the bottom navigation bar.
- [x] All styling is responsive, stacking sections vertically on mobile screens with clean dashed borders.
- [x] **Dark theme conversion**: Styled the footer with dark slate backgrounds (`--sidebar-bg-deep` and `--sidebar-bg`) and glowing teal accents (`--sidebar-accent`) to complement the dark editorial sidebar and mobile bottom nav.
- [x] No controller, service, model, or database schema changes.

### 12. Shop Settings Page

Status: Implemented - Needs Owner Manual Validation  
Type: HITL  
Blocked by: None  
Business Value: Replaces hardcoded and appsettings.json shop info with a database-backed settings page that all staff can update, reflected across the sidebar brand, login page, receipt, and system title.

Acceptance Criteria:
- [x] New `ShopSetting` single-row table stores Name, LatinName, Phone, WhatsApp, Address, TelegramHandle, OpeningHours.
- [x] Seed data matches the values previously in `appsettings.json`.
- [x] `/ShopSettings` page lets any authenticated staff edit all fields.
- [x] Sidebar brand name reads dynamically from settings (Arabic name + Latin initial).
- [x] Login page brand name reads dynamically from settings.
- [x] Receipt view uses settings from service instead of `IConfiguration` / hardcoded strings.
- [x] Page title reads `- {LatinName}` dynamically.
- [x] Settings nav item ("الإعدادات") appears in the sidebar under "الإدارة", accessible to all roles.

### 13. Catalog Photos for Brands, Models, and Parts

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: Inventory Hierarchy as Lookup Tables; Browse Available Parts in Telegram
Business Value: Makes the catalog easier to scan visually for staff and clients by attaching optimized local photos to brands, models, and exact inventory parts, with Telegram using the same images when the app is publicly reachable.

Acceptance Criteria:
- [x] Brands can store optimized local image and thumbnail paths.
- [x] Phone models can store optimized local image and thumbnail paths.
- [x] Inventory parts can store optimized local image and thumbnail paths.
- [x] Uploads are processed through Magick.NET, auto-oriented, stripped of metadata, resized, and saved as WebP.
- [x] Category management shows brand/model thumbnails and allows replacing photos.
- [x] Inventory add/edit allows uploading an exact part photo.
- [x] Inventory cards show part photo first, then model fallback, then brand fallback, then a placeholder.
- [x] Telegram variant results use the first available part/model/brand image when `Telegram:PublicBaseUrl` is configured.
- [x] Bot remains read-only and falls back to text-only when images or public URL are missing.
- [x] Runtime uploaded catalog files are ignored by Git.

### 14. Staff-Reviewed Phone Model Image Fetching

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: Catalog Photos for Brands, Models, and Parts
Business Value: Lets staff quickly fill missing phone-model photos from GSMArena while keeping humans in control of image correctness.

Acceptance Criteria:
- [x] Staff can open an image picker from the phone model list.
- [x] The picker searches GSMArena using brand and model names by default.
- [x] Search results show multiple candidate images with source names.
- [x] Staff must choose a candidate before any catalog image is saved.
- [x] The selected remote image is downloaded server-side and processed through `CatalogImageService`.
- [x] Saved images update `PhoneModel.ImageUrl` and `PhoneModel.ThumbnailUrl`.
- [x] The model list refreshes or updates so staff can see the new thumbnail.
- [x] Remote image downloads require HTTPS, GSMArena allow-listed hosts, request timeout, and size limits.
- [x] Bulk import remains out of scope for the first version.

### 15. Staff-Reviewed Part Image Fetching (iFixit)

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: Staff-Reviewed Phone Model Image Fetching
Business Value: Lets staff quickly assign images to inventory parts by searching and selecting pictures from iFixit's catalog, saving time and keeping catalog imagery complete.

Acceptance Criteria:
- [x] Staff can open an image picker from the inventory parts page.
- [x] The picker searches iFixit using brand, model, and part type names by default.
- [x] Search results show multiple candidate images with source names from iFixit.
- [x] Staff can choose a candidate before any part image is saved.
- [x] The selected remote image is downloaded server-side and processed through `CatalogImageService`.
- [x] Saved images update `InventoryPart.ImageUrl` and `InventoryPart.ThumbnailUrl`.
- [x] The inventory part card refreshes/updates so staff can see the new thumbnail.
- [x] Remote image downloads require HTTPS, iFixit allow-listed hosts, request timeout, and size limits.

### 16. Retractable Desktop Sidebar (Icons-Only Rail)

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: Replace Top Bar With Modern Dark Sidebar
Business Value: Lets desktop users collapse the sidebar into a slim 64px icon-only rail, reclaiming screen space for content while keeping navigation one click away. Collapse state persists across sessions via localStorage.

Acceptance Criteria:
- [x] Desktop (≥768px) sidebar can collapse to a 64px icons-only rail.
- [x] Collapse toggle button lives inside the top-inside edge of the sidebar rail.
- [x] Toggle uses a chevron icon that reverses direction depending on collapsed state.
- [x] Collapsed rail shows only: brand mark, nav icons, user avatar, profile/logout icon buttons.
- [x] Section labels ("العمليات", "الإدارة") hidden when collapsed.
- [x] Nav item labels visually hidden (screen-reader accessible via title attr).
- [x] Active nav item teal accent bar stays visible at the rail edge when collapsed.
- [x] Waiting-count badge on الحالات collapses to a small amber dot at the icon edge.
- [x] Zero-count badge hidden entirely when collapsed.
- [x] Sidebar scrollbar hidden when collapsed.
- [x] Collapse state persists in localStorage across page reloads.
- [x] Nav items receive title attributes for hover tooltips (auto-applied via JS).
- [x] Mobile drawer (<768px) unchanged — collapse toggle hidden; is-sidebar-collapsed class forced off.
- [x] Resizing back to desktop re-applies stored collapsed preference.
- [x] Brand block collapses to logo-mark only; brand text (Latin name, Arabic sub) hidden.
- [x] No controller, service, model, or database schema changes. Pure client-side CSS + JS + markup.

### 27. Wire Up Dynamic Modal Scripts (brand pills, phone lookup, model picker, quick tags)

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: True Inline Modals (Replace Dedicated "Modal Page" Views)
Business Value: Makes the dynamic modal system actually functional. Inline `<script>` tags inside `_CreateModalPartial.cshtml`, `_FormModalPartial.cshtml`, and `_ImportPreviewModalPartial.cshtml` have never executed because `injectHtml` uses `DOMParser` + `importNode` + `appendChild`, and browsers do not execute scripts inserted that way. As a result, every click handler (brand pills, phone lookup, model picker, quick tags, import checkboxes) was dead on arrival. After the fix, all dynamic modals behave as designed.

Acceptance Criteria:
- [x] Brand pills highlight on click and load the matching phone models in the model picker.
- [x] Phone-number search via the بحث button or Enter key fills customer fields and updates the status chip.
- [x] Model picker trigger opens the searchable bottom-sheet/dialog and selecting a model updates the form.
- [x] Quick tag clicks insert the tag text into the المشكلة textarea.
- [x] Inventory form's brand pill selection, model TomSelect, and step navigation work.
- [x] Categories import preview's checkbox counter and "select all" button work.
- [x] Fix added in `wwwroot/js/site.js` → `injectHtml`: after each `root.appendChild(imported)`, inline `<script>` elements without `src` are replaced with a fresh `document.createElement("script")` carrying the same `textContent`, which the browser then executes.
- [x] No regression on the static inline modals on `RepairTickets/Details` and `Categories/Index` (their scripts live in the page HTML, not the modal host).
- [x] No controller, service, model, database schema, package, or migration changes.

### 26. Fix Modal Body Scrolling on Desktop

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: True Inline Modals (Replace Dedicated "Modal Page" Views)
Business Value: Restores the expected scroll-within-the-modal behavior on desktop for all inline and inline-style Bootstrap modals that wrap their content in a `<form>`, so long forms (intake, inventory, staff, settings, payment, etc.) are reachable end-to-end instead of being clipped by the viewport.

Acceptance Criteria:
- [x] All `.modal-dialog-scrollable` modals scroll inside the modal body on desktop, not the page behind.
- [x] No regression on mobile: the existing full-screen sheet layout (header fixed, body scrolls, footer fixed) is unchanged.
- [x] One CSS rule added in `wwwroot/css/site.css` makes `.modal-dialog-scrollable .modal-content > form` a flex-column container with `max-height: 100%` and `overflow: hidden`, restoring the flex chain that the `<form>` wrapper was breaking.
- [x] No controller, service, model, database schema, package, or migration changes.

### 20. Redesign Repair Intake (Create) UX

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: Create Repair Ticket With Customer Lookup and Receipt
Business Value: Rebuilds `/RepairTickets/Create` as a single-column intake slip with a prominent phone-search hero, an ever-present live summary card, two submit actions, and cleaner quick-tag/worker/brand interactions — so counter staff can fill tickets faster and with fewer mistakes.

Acceptance Criteria:
- [x] Page is a single-column intake sheet grouped into four labelled blocks (العميل / الجهاز / المشكلة / التسليم والتسعير); the old fake numbered "steps" are gone.
- [x] Phone field is a hero input with an explicit "بحث" button and Enter-to-search; the passive blur/change auto-lookup is removed.
- [x] Customer lookup status shows as a compact inline status chip (idle/loading/found/new/error) replacing the full-width banner.
- [x] Device brand still uses pills, plus a search filter input that appears only when more than 8 brands exist.
- [x] Quick tags toggle on/off (active set joined with "، ") instead of appending comma-separated text; free text typed after a tag set is preserved.
- [x] Worker assignment is a plain required `<select>`; the badge/"تغيير" reveal dance is removed.
- [x] Estimated price input has a "دج" unit suffix.
- [x] Notes is a normal optional textarea, no longer buried in a `<details>`.
- [x] A live summary card (sticky on >=992px, hidden on mobile) shows customer / device / problem / worker / price as the operator types.
- [x] Two submit actions: "حفظ وطباعة" (default, redirects to Receipt) and "حفظ ومتابعة" (saves, returns to empty Create with a success toast keyed by `TempData["IntakeMessage"]`).
- [x] No EF migration, model annotation, or new NuGet packages. Controller gains one optional `action` parameter on POST Create.
- [ ] Owner builds and runs the manual validation steps below.

## Manual Validation Needed

Manual validation steps must be added to the session handoff after each implemented slice. Include pages or bot flows to open, input data to use, expected results, and PRD edge cases checked.

### 19. Manageable Shop Logo from Shop Settings

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: Shop Settings Page
Business Value: Lets the owner upload, replace, and remove the shop logo from the `/ShopSettings` page. The logo then appears across the staff app brand surfaces (sidebar, footer, login page), on the printed repair receipt, and on the Telegram bot main menu, so the shop identity is consistent everywhere without touching code or static files.

Acceptance Criteria:
- [x] `ShopSetting` stores an optional `LogoUrl` (nvarchar 500).
- [x] `/ShopSettings` page offers a file upload for the logo with a live preview of the current logo and a "remove logo" option.
- [x] Uploads are validated to PNG/JPG/JPEG/WEBP/GIF, max 2 MB; SVG is rejected for Telegram compatibility.
- [x] Uploaded logo is saved to `wwwroot/images/logo{ext}` and the relative URL is stored in the database; the previous logo file is deleted on replace or remove.
- [x] Sidebar brand shows the logo image when set, otherwise falls back to the Latin-name initial mark.
- [x] Footer brand shows the logo image when set, otherwise falls back to the "M" mark.
- [x] Login page shows the logo image when set, otherwise falls back to the Latin-name initial mark.
- [x] Repair receipt shows the logo in its header when set, otherwise falls back to the `◆` mark.
- [x] Telegram bot `/start` (and "الرئيسية" navigation) sends the welcome menu as a photo with the shop logo caption when a logo is set, otherwise falls back to a text-only welcome (existing behavior).
- [x] Fallback path never breaks when no logo is set or the logo file is missing.
- [ ] Owner runs the EF migration `AddShopLogoUrl` and validates the flows above.

### 21. Mobile UX/UI Optimization

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: None
Business Value: Optimizes MimoShop to feel like a native mobile app by resolving safe-area bottom navbar overlap, converting lists into structured scan-friendly cards on mobile, forcing segmented equal-width controls on tabs (preventing wrapping), disabling default grey tap overlays, adding tactile `:active` state animations, styling search modals as Bottom Sheets, and adding horizontal scroll fade cues for tables.

Acceptance Criteria:
- [x] Padded `.app-container` on screens <768px by at least 80px + safe area inset to clear floating bottom navbar.
- [x] Refactored mobile `.job-row` via Grid Areas to place code and status inline at the top, device details in the middle, and customer info at the bottom with a separator.
- [x] Configured `.tabs__nav` to display as an equal-width grid control (`repeat(4, 1fr)`) with compact padding and font size on mobile to prevent wrapping.
- [x] Disabled browser-default `-webkit-tap-highlight-color` on all buttons, links, cards, and input tags.
- [x] Added instant visual active feedback (scale down to `0.97` and brightness reduction) to all primary buttons and interactive cards.
- [x] Redesigned `.image-fetch-modal` on mobile viewports as a Bottom Sheet sliding up from viewport bottom with safe top rounded corners and swipe handle bar.
- [x] Styled horizontal fade overlays at table ends on screens <768px via a radial/linear overlay on `.table-wrap` to indicate horizontal scrollability.
- [x] Phone, WhatsApp, quantity, sort-order, price, and payment amount inputs explicitly use LTR direction so early digits remain visible in RTL mobile layouts.
- [x] Intake price input positions the `دج` adornment away from the typed value and pads the field physically, preventing the unit from covering the first digits.
- [x] Mobile drawer side, edge gesture zone, drag progress, and swipe velocity are direction-aware so RTL opens/closes from the right consistently.
- [ ] Owner builds, runs, and validates the mobile improvements on a smartphone or mobile emulator window.

### 22. Searchable Device Model Picker

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: Redesign Repair Intake (Create) UX
Business Value: Replaces the poor native mobile dropdown experience for large device-model catalogs with a searchable picker, reducing scrolling and mis-selection during repair intake.

Acceptance Criteria:
- [x] `/RepairTickets/Create` no longer exposes the large device model list as the primary visible control.
- [x] The original `DeviceModel` select remains in the form as the MVC-bound source of truth, so posting and server validation continue to use the existing model property.
- [x] Staff select a brand first, then tap a full-width model picker trigger.
- [x] The picker opens as a dialog on desktop and as a bottom sheet on mobile.
- [x] The picker includes a search field that filters only the currently selected brand's visible models.
- [x] Selecting a model updates the hidden select, closes the picker, updates the trigger label, and refreshes the live intake summary.
- [x] Changing brand clears a now-invalid model choice and resets the picker prompt.
- [x] Empty search results show a clear empty state.
- [x] No controller, service, model, database schema, package, or automated test changes.
- [ ] Owner builds and validates on a mobile browser or emulator.

### 25. Price Input UX (Select-on-Focus, Clear Button, Friendly Hint)

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: None
Business Value: Eliminates the friction of clearing a placeholder "0" or an existing value before typing a new price on intake, inventory, and payment screens. Selects the existing value on focus, swaps the misleading "0" placeholder for an Arabic example hint, and adds a one-tap inline ✕ clear button inside each price input. Works identically on desktop click and mobile tap.

Acceptance Criteria:
- [x] EstimatedPrice, UnitCostPrice, UnitSalePrice, and the payment modal's estimated/paid inputs all share the `.price-input` wrapper with a `دج` unit adornment and an inline ✕ clear button.
- [x] Each price input uses `type="text" inputmode="decimal" dir="ltr"` and a friendly `placeholder="مثال: 1500"` (or per-field equivalent) instead of the previous "0".
- [x] A delegated `focusin` handler in `site.js` selects the existing value of any `[data-price-input]` on focus so the next keystroke replaces it in one go.
- [x] A delegated `click` handler clears the field and keeps focus when the ✕ button is pressed.
- [x] An `input` handler strips non-numeric characters (keeps digits and a single `.` or `,`) so pasted text like "1 500 دج" still binds cleanly.
- [x] A `MutationObserver` re-binds new price inputs added to the DOM (covers the dynamic intake + payment modals).
- [x] CSS hides the clear button via `:placeholder-shown` until the field has a value, and bumps its size to 32×32 on screens <576px.
- [x] No controller, service, model, database schema, package, or migration changes.
- [ ] Owner builds and validates the manual steps in the handoff.

### 24. True Inline Modals (Replace Dedicated "Modal Page" Views)

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: Redesign Repair Intake (Create) UX; Manageable Shop Logo from Shop Settings
Business Value: Eliminates the dedicated "modal page" pattern (a full Razor view whose only content is a Bootstrap modal that auto-opens on load). Every form is now fetched and opened as a true inline modal over the page the user is on — no navigation, no full-page flash, no `data-modal-autoopen`/`data-modal-cancel-url` plumbing. Form submission uses `fetch` + `X-Requested-With`; the controller returns `Json({ ok, redirectUrl | replace | reload, message })` on success and the same partial on validation failure, which the JS swaps in place.

Acceptance Criteria:
- [x] Single shared `#mimoModalHost` rendered by `_Layout.cshtml`; modals are appended to it on demand.
- [x] Generic `MimoShop.modal.{openModal, closeModal, submitForm, reloadModal}` in `site.js` handles fetch, validation, success, and error cases.
- [x] Click triggers use a `data-open-modal="key"` attribute; the key→URL map and the optional `data-modal-arg` template live in `site.js`.
- [x] Forms rendered by modals carry `data-modal-form`; the global delegated `submit` handler posts via `fetch` with the antiforgery token.
- [x] Validation errors re-render the modal body in place (no page navigation, no flash).
- [x] Submitter (`event.submitter`) is read and re-appended to `FormData`, so the two-button RepairTickets intake still distinguishes `action=print` from `action=continue`.
- [x] "Save & continue" on the intake reloads the modal partial in place and shows a success toast — the modal stays open.
- [x] ShopSettings save, Staff create/edit/reset, Inventory create/edit, Account/ChangePassword, and Categories/ImportPreview success cases close the modal and reload the host page so updated data (e.g. logo) appears.
- [x] Categories/ImportPreview returns `204 No Content` when there is nothing to import; JS shows a page-level info toast without opening a modal.
- [x] All confirm-modal intercepts (`data-confirm-modal` on Staff/Index activate/deactivate, RepairTickets/Details status/ReceiveOnDemandPart, Categories/Index activate, sidebar logout) remain intact.
- [x] The inline `RepairTickets/Details` modals (payment, consume part, request part) and the inline `Categories/Index` entity modals (catBrandModal etc.) are unaffected.
- [x] All previously-existing "modal page" Razor views are removed: `RepairTickets/Create`, `Staff/Create`, `Staff/Edit`, `Staff/ResetPassword`, `Inventory/Form`, `Account/ChangePassword`, `ShopSettings/Index`, `Categories/ImportPreview`.
- [x] Direct URLs to those pages (e.g. `/RepairTickets/Create`) are no longer reachable (the corresponding GET actions were replaced with `*Modal` actions).
- [x] `button.nav-item` and `button.primary-action` resets in `site.css` keep the new `<button>` triggers looking identical to the previous `<a>` links.
- [x] No controller signature changes for POST actions — only added an AJAX branch via `X-Requested-With` detection.
- [x] No new NuGet packages, no EF migration, no schema change.
- [ ] Owner builds, runs, and validates the manual steps in the handoff.

### 23. UI/UX Design-Intelligence Overhaul (Contrast, Borders, Search-Selects)

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: Searchable Device Model Picker
Business Value: Applies SKILL.md design intelligence to fix contrast, border visibility, focus accessibility, and touch targets. Replaces large native `<select>` dropdowns with Tom Select search-comboboxes for inventory and parts selection. Adds a sticky app-header bar with breadcrumb context.

Acceptance Criteria:
- [x] Design tokens updated: borders darker (#e2e8f0→#cbd5e1), text deeper (#334155→#1e293b), page background differentiated from card surfaces.
- [x] Shadow depth doubled on cards and modals for better surface separation.
- [x] Status/payment pills now have visible borders (previously bg-color only).
- [x] Focus rings unified: all `outline: 0` kills removed; `:focus-visible` outline applies consistently across form controls, model picker, bottom nav, filter search.
- [x] Buttons meet Apple HIG minimum touch target (42px→44px).
- [x] Sticky app-header bar with white background, visible bottom border, and 3px brand-teal accent on the start edge; breadcrumb moved into header.
- [x] Bootstrap modals now have styled `.modal-header` (brand-tint bg, bottom border) and `.modal-content` (border + elevated shadow).
- [x] Tom Select vendor directory created (`wwwroot/lib/tom-select/`); CSS/JS referenced in `_Layout.cshtml`.
- [x] `Inventory/Form.cshtml` PhoneModelId `<select>` converted to Tom Select searchable combobox with brand-filter re-sync.
- [x] `RepairTickets/Details.cshtml` two `InventoryPartId` `<select>`s (stocked + on-demand) converted to Tom Select.
- [x] Tom Select CSS themed to design tokens (control border, dropdown shadow, option hover, focus ring).
- [x] Small native `<select>`s (roles, brands, part types, variants) left as-is — not Tom Select.
- [x] Auth layout (`_AuthLayout.cshtml`) unaffected by header/sidebar/TS changes.
- [x] No controller, service, model, database schema, package, or automated test changes.
- [ ] Owner downloads Tom Select dist files to `wwwroot/lib/tom-select/`.
- [ ] Owner builds, runs, and validates the changes below.

### Manual Validation — Task 23

**Prerequisites**
Download Tom Select 2.4.3 dist files:
```
curl -L -o wwwroot/lib/tom-select/tom-select.complete.min.js https://cdn.jsdelivr.net/npm/tom-select@2.4.3/dist/js/tom-select.complete.min.js
curl -L -o wwwroot/lib/tom-select/tom-select.default.min.css https://cdn.jsdelivr.net/npm/tom-select@2.4.3/dist/css/tom-select.default.min.css
```

Then:
```
dotnet build MimoShop.slnx
dotnet run --project MimoShop
```

**Visual checks**
- [ ] Every page: card borders and table cell separators are clearly visible (not ghost-borders).
- [ ] Sticky header bar visible at top with breadcrumb + brand-teal accent line.
- [ ] Status pills (جديد, قيد الإصلاح, منتهي, مدفوع, غير مدفوع) have visible borders.
- [ ] Modals have a light-teal header bar with clear bottom border.
- [ ] Buttons are 44px tall minimum.
- [ ] Page background (#f1f5f9) differentiates from card surfaces (white).
- [ ] Tab through page: every focusable element shows a visible ring.

**Tom Select — Inventory Form (Add/Edit Part)**
- [ ] Open Inventory → Add Part (or Edit).
- [ ] Click model dropdown: search field appears.
- [ ] Type letters: options filter live.
- [ ] Change brand tab: model dropdown repopulates and search still works.
- [ ] Pick a model and submit: correct PhoneModelId posts.

**Tom Select — Repair Ticket Details**
- [ ] Open a ticket → "استهلاك قطعة من المخزون" modal.
- [ ] Click part dropdown: Tom Select with search.
- [ ] Type letters, pick a part, submit: correct InventoryPartId posts.
- [ ] Same for "طلب قطعة عند الحاجة" modal.

**Mobile**
- [ ] Touch targets comfortable (44px buttons, form controls).
- [ ] Tom Select dropdown dismisses on outside tap; keyboard opens on search.
- [ ] Bottom nav, hamburger, drawer unchanged.
- [ ] Home page (no breadcrumb): header bar still shows (empty, not broken).
- [ ] Staff/Create: role `<select>` is still native (not Tom Select).

### 26. Inventory Index Filter Bar + Card Layout Cleanup

Status: Implemented - Needs Owner Manual Validation
Type: HITL
Blocked by: None
Business Value: Cleans up the `/Inventory` index page so the filter section is no longer chaotic on desktop or mobile. Reorganizes the filter bar into three clearly-scoped rows that mirror the `/RepairTickets/Index` pattern (stock tabs on top, search + primary action in the middle, dropdown refinements at the bottom), and makes each inventory card's footer wrap gracefully on small screens so the date and action buttons no longer crowd each other.

Acceptance Criteria:
- [x] `/Inventory/Index.cshtml` `.filter-bar` restructured to a flat `.filter-bar.inv-filter` with three logical rows: `.filter-tabs` (الكل / مخزنة / عند الطلب), `.cat-toolbar` (search + "إضافة قطعة"), and `.form-grid--3.inv-filter__refine` (brand / type / variant selects), each separated by hairline borders + consistent spacing.
- [x] The "إضافة قطعة" primary action now lives alone on the right of the search row, no longer grouped visually with the stock tabs.
- [x] Search input has a real magnifying-glass icon (`.filter-search__icon` + inline SVG) matching the RepairTickets search affordance, with `aria-label="بحث في المخزون"`; the redundant `<label class="form-label">بحث</label>` is removed.
- [x] Stock tabs are real `.filter-tab[data-stock]` buttons (existing JS contract) with `role="tablist"` and `aria-label` for screen readers.
- [x] Card footer: inline `style="color:..."` and `style="display: flex; gap: 8px; align-items: center;"` removed in favour of `.inv-card__date`, `.inv-card__actions`, and `.inv-card__action[--primary]` classes.
- [x] The "تعديل ←" action is brand-coloured + bolded so it reads as the primary card action; "جلب صورة" is muted text-soft.
- [x] `.inv-card__foot` now has `flex-wrap: wrap` + row/column gap, with a `@media (max-width: 380px)` rule that switches to a stacked column layout and full-width action row.
- [x] New CSS block: `.inv-filter` (column flex + gap), `.inv-filter__toolbar` (kills the default `.cat-toolbar { margin-bottom }` so the wrapper owns spacing), `.inv-filter__search` (flex: 1, min-width: 0), `.inv-filter__refine` (top border + padding to separate the dropdowns from the search row).
- [x] `.filter-search__icon` is a reusable RTL-aware class (uses `inset-inline-end` so it sits on the correct side in both LTR and RTL builds).
- [x] The existing filter IIFE in `Inventory/Index.cshtml` still binds to `.filter-tab[data-stock]`, `[data-filter='search']`, `[data-filter='brand']`, `[data-filter='type']`, `[data-filter='variant']`, and `data-active-stock` — no JS edit needed.
- [x] No controller, service, model, database schema, package, or migration changes.
- [ ] Owner builds, runs, and validates the manual steps in the handoff.
