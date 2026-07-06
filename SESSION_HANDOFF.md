# Session Handoff

## Latest Session — Inventory Index Filter Bar + Card Layout

**Problem reported by owner:** "in /Inventory index the filters section looks chaotic both on desktop and mobile, we need to rearrange it and overall improve the page for both desktop and mobile."

**Root cause:** The previous filter bar was a single `.filter-bar` that tried to do everything at once:
- Three dropdowns (`العلامة`, `نوع القطعة`, `النوعية`) on a `.form-grid--3` row wrapped in an outer `.form-grid` (single column) with inline `style="grid-template-columns: 1fr;"`.
- Below that, a `.cat-toolbar` that grouped the **search input**, the **"إضافة قطعة" button**, and the **stock tabs** (`الكل` / `مخزنة` / `عند الطلب`) in two ad-hoc flex children. The button + tabs were shoved into a right-side column with `align-items: flex-end` next to the search, which made the visual weight lopsided and the button feel like a sub-element of the tabs instead of a primary action.
- The search input had a redundant `<label class="form-label">بحث</label>` above it but no icon, so the row felt under-designed.
- On mobile (`≤575px`), `.cat-toolbar` stacks the two children vertically, but each child then broke awkwardly — the left child was the search input alone (with its label), the right child was the button stacked on top of the tabs with `flex-direction: column` and `align-items: flex-end`, so the tabs ended up right-aligned and small under the button.
- The per-card footer was also fragile: inline `style="display: flex; gap: 8px; align-items: center;"` for the action group + an inline-styled date on the same row, with no `flex-wrap`. On a 360-380px viewport the "جلب صورة" + "تعديل ←" + Arabic date string all crowded together.

**Fix:** Restructured the page to mirror the cleaner `.filter-bar` pattern used by `/RepairTickets/Index`, and gave the card footer real classes + responsive wrapping.

### `MimoShop/Views/Inventory/Index.cshtml`
- Replaced the nested-grid chaos with a flat `.filter-bar.inv-filter` containing three logical rows:
  1. `.filter-tabs` (الكل / مخزنة / عند الطلب) on top — same component as RepairTickets, no inline overrides, `role="tablist"` + `aria-label`.
  2. `.cat-toolbar.inv-filter__toolbar` with the search input on the left and the "إضافة قطعة" primary button on the right.
  3. `.form-grid--3.inv-filter__refine` with brand / type / variant selects, visually separated from the toolbar by a top border + padding (so they read as "refinements" rather than competing with the main search).
- Search input now has a real magnifying-glass icon (`.filter-search__icon` + SVG), matching the RepairTickets search; the redundant "بحث" label is gone, replaced with `aria-label="بحث في المخزون"` on the input.
- The three selects keep their descriptive labels (`العلامة`, `نوع القطعة`, `النوعية`) so the form-field structure stays consistent with the form modal patterns elsewhere in the app.
- Card footer: inline `style="color: ..."` and `style="display: flex; gap: 8px; align-items: center;"` removed in favour of `.inv-card__date`, `.inv-card__actions`, and `.inv-card__action[--primary]` classes. The "تعديل ←" action is now brand-coloured and bolded to read as the primary card action; "جلب صورة" is muted text-soft.

### `MimoShop/wwwroot/css/site.css`
- Added `.filter-search__icon` (absolutely positioned, RTL-aware `inset-inline-end: 12px`, pointer-events none, vertically centered). Reusable by any future page that needs the same search affordance.
- Improved `.inv-card__foot` to wrap (`flex-wrap: wrap` + row/column gap), added `.inv-card__date`, `.inv-card__actions`, `.inv-card__action`, `.inv-card__action--primary`.
- New `@media (max-width: 380px)` rule on the foot: switches to `flex-direction: column; align-items: stretch` and the actions go `space-between` so the two buttons sit on opposite ends of a full-width row beneath the date.
- New `.inv-filter` block: column-flex with `gap: var(--space-3)`, plus `.inv-filter__toolbar` (kills the default `.cat-toolbar { margin-bottom: var(--space-3) }` so the wrapper controls spacing), `.inv-filter__search` (flex: 1, min-width: 0 so it shrinks cleanly), and `.inv-filter__refine` (top border + padding to separate refinements from the search row, with a small bump at `≥992px`).

No controller, service, model, JS, schema, or package changes. The existing IIFE in the view already targets `.filter-tab[data-stock]`, `[data-filter='search']`, `[data-filter='brand']`, etc. — those selectors are unchanged, so the filter logic still works without any JS edit.

### Owner commands
```bash
dotnet build MimoShop.slnx
dotnet run --project MimoShop
```

### Manual validation
1. Open `/Inventory` on a desktop viewport (≥992px):
   - The filter bar now reads top-to-bottom as: **stock tabs** (الكل / مخزنة / عند الطلب) → **search + "إضافة قطعة" button** in a single row → **brand / type / variant selects** in a 3-column row, visually separated by a hairline border.
   - The search input shows the magnifying-glass icon on its inline-end edge.
   - "إضافة قطعة" is the only thing on the right of the search row — the stock tabs are no longer crammed next to it.
2. Click each stock tab — the active state moves and the grid filters as before. Type in the search box — results narrow live. Change any of the 3 selects — results narrow live. They still AND together.
3. Open `/Inventory` on a tablet viewport (576–991px):
   - The refine row collapses to 2 columns (one of the three selects wraps to a second row).
4. Open `/Inventory` on a narrow phone viewport (≤575px, no DevTools device frame, just a 360px window):
   - The toolbar stacks: search on top, "إضافة قطعة" button below it (full width).
   - The three refine selects stack to 1 column.
   - The stock tabs wrap to a second row if needed (they were already `flex-wrap: wrap`).
5. In a card, the footer shows the date on the left and the two action buttons on the right with comfortable gap. Resize the window to ~360–380px: the date stays on its own line and the two action buttons go full-width with `space-between` (جلب صورة on the inline-start side, تعديل ← on the inline-end side).
6. Confirm the per-card "جلب صورة" still opens the iFixit modal, "تعديل ←" still opens the inventory form modal, and the empty-state ("لا توجد قطع تطابق البحث") still appears when filters return zero results.
7. Confirm the empty-inventory case (delete the only part and reload) still shows the "لا توجد قطع في المخزون بعد" card with its "إضافة قطعة" CTA.

---

## Previous Session — Wire Up Dynamic Modal Scripts (brand pills, phone lookup, model picker, quick tags, etc.)

**Problem reported by owner:** "nope, actually we cant even chose a brand. its not functional."

**Root cause:** Inline `<script>` tags inside dynamically-injected modals have never executed. `injectHtml(root, html)` in `wwwroot/js/site.js` does `DOMParser.parseFromString` → `document.importNode` → `root.appendChild`, and browsers deliberately do NOT execute scripts inserted that way. Every dynamic modal partial (`_CreateModalPartial.cshtml`, `_FormModalPartial.cshtml`, `_ImportPreviewModalPartial.cshtml`) relies on a bottom-of-partial `<script>` IIFE to attach its click handlers — so brand pills, phone lookup, model picker, quick tags, and the import preview checkboxes have all been dead on arrival since the inline-modal system was built. The static inline modals on `RepairTickets/Details` and `Categories/Index` work because their scripts were in the original page HTML.

The previous two fixes (CSS scroll on desktop, quick-tag click-to-insert) were both correct — they just never ran.

**Fix:** In `wwwroot/js/site.js` → `injectHtml`, after each `root.appendChild(imported)`, detect an inline script element and replace it with a fresh `document.createElement("script")` that copies the `textContent`. The browser then executes it like any other newly-created script tag.

```js
if (imported.nodeType === 1 && imported.tagName === "SCRIPT" && !imported.src) {
  var fresh = document.createElement("script");
  fresh.textContent = imported.textContent;
  root.replaceChild(fresh, imported);
}
```

The modal `<div>` is always appended *before* its sibling `<script>` in the partial's source order, so by the time the script runs, `document.getElementById("repairCreateModal")` resolves correctly. Scripts are not run again on subsequent `openModal` calls because `openModal` short-circuits when the modal already has `data-modal-key` and just re-shows it.

### Files changed
- `wwwroot/js/site.js` — 5-line addition to `injectHtml` to re-activate inline scripts.

No CSS, view, controller, service, model, schema, or package changes.

### Owner commands
```bash
dotnet build MimoShop.slnx
dotnet run --project MimoShop
```

### Manual validation
1. **بطاقة صيانة جديدة** (Home primary action or `/RepairTickets/Index`):
   - Brand pills highlight on click; the hidden `DeviceBrand` input updates; the model picker trigger becomes enabled with "ابحث واختر الموديل".
   - Click the model picker trigger → modal opens with the brand's model list; search filters; selecting a model closes the picker and updates the trigger label.
   - Type a phone number, press بحث → status chip turns to "تم العثور على العميل" or "رقم جديد...".
   - Click any quick tag → its text appears in the المشكلة textarea.
   - Submit empty → inline validation errors; submit valid → modal closes, navigates to the receipt (or reloads in place with "حفظ ومتابعة").
2. **إضافة قطعة / تعديل قطعة** (Inventory): brand pills highlight and load models; TomSelect on PhoneModel is searchable; the photo upload and step navigation work.
3. **استيراد من الكتالوج** (Categories, SuperAdmin/Owner only): the checkbox counter and the "استيراد المحدد" button work; the "تحديد/إلغاء الكل" button works.
4. **Static inline modals** (no regression):
   - `/RepairTickets/Details` — payment, consume part, request part.
   - `/Categories/Index` — catBrandModal, catModelModal, catTypeModal, catVariantModal + image-fetch.
5. **Desktop scroll** (Task #26 regression check): open any of the long modals on a tall enough viewport to clip the body — body scrolls, header/footer pinned.
6. **Mobile scroll** (Task #26 regression check): same modals on <768px — full-screen sheet, header fixed, body scrolls, footer fixed.
7. **Confirm modal** still intercepts Staff activate/deactivate, Categories activate, sidebar logout, RepairTickets status transitions.

---

## Previous Session — Fix Quick Tags Click-to-Insert on Intake

**Problem reported by owner:** "in repair ticket create, when we click on an item in المشكلة it does not copy it to text field like before."

**Root cause:** Task #20 changed quick tags from a simple "click → append to textarea" pattern to a toggle Set where active tags were joined with "، " and the joined text was written into the textarea. That behavior was unintuitive: it rewrote the whole value on every click, lost manual edits when a tag was re-clicked, and didn't match the original "click copies the tag into the field" mental model.

**Fix:** In `MimoShop/Views/RepairTickets/_CreateModalPartial.cshtml` (script block at the bottom), replaced the `activeTags` Set + `syncTagsToInput()` + `input` handler that tracked free text with a single `insertQuickTag(tag)` function:

```js
function insertQuickTag(tag) {
    if (!problemInput || !tag) return;
    var current = problemInput.value || "";
    if (current.indexOf(tag) !== -1) return;
    var needsSeparator = current.length > 0 && !/[\s،]$/.test(current);
    problemInput.value = current + (needsSeparator ? "، " : current ? " " : "") + tag;
    problemInput.focus();
}
```

Behavior:
- Click a quick tag → its text is appended to the textarea, separated by "، " if the existing text doesn't already end with whitespace or an Arabic comma.
- Tag is not added again if it's already present in the textarea (prevents accidental duplicates).
- Textarea is focused after insertion so the operator can keep typing.
- No visual `is-active` toggle, no Set, no re-render — the textarea is the single source of truth.

### Files changed
- `MimoShop/Views/RepairTickets/_CreateModalPartial.cshtml` — replaced the toggle Set + syncTagsToInput + input handler with `insertQuickTag`.

No CSS, controller, service, model, schema, or package changes.

### Owner commands
```bash
dotnet build MimoShop.slnx
dotnet run --project MimoShop
```

### Manual validation
1. Open بطاقة صيانة جديدة from Home or /RepairTickets/Index.
2. Click "شاشة مكسورة" → its text appears in the المشكلة textarea, with focus returned to the textarea.
3. Type a few words, click "بطارية ضعيفة" → result is `... typed words، بطارية ضعيفة`.
4. Click "شاشة مكسورة" again → no duplicate; the textarea is unchanged.
5. Mix: click 2–3 tags, then type free text after the joined tags — the free text is preserved naturally because we only append.

---

## Previous Session — Fix Modal Body Scrolling on Desktop (Task #26)

**Problem reported by owner:** "the modals are not scrolling on desktop."

**Root cause:** Every form modal wraps `.modal-header` / `.modal-body` / `.modal-footer` in a `<form>` inside `.modal-content`. Bootstrap's `.modal-dialog-scrollable` works by making `.modal-content` a flex-column with `max-height: 100%; overflow: hidden`, then letting `.modal-body` `overflow-y: auto` inside that constrained container. The extra `<form>` is a block element, so the flex chain stops at the form and `.modal-body` never gets a height constraint — `overflow-y: auto` has no effect and content is clipped by `.modal-content`'s `overflow: hidden`. The mobile media-query patch in `site.css` (lines 3545–3621) explicitly makes the form a flex column on screens <768px, which is why mobile worked and desktop did not.

**Fix:** One CSS rule added in `wwwroot/css/site.css` (between the existing `.modal-footer` rule and the mobile `@media` block), outside any media query:

```css
.modal-dialog-scrollable .modal-content > form {
  display: flex;
  flex-direction: column;
  max-height: 100%;
  overflow: hidden;
}
```

This restores the flex chain through the form on all viewports, so `.modal-body` is properly constrained and scrolls inside the modal.

### Files changed
- `wwwroot/css/site.css` — added the four-line rule above.
- `TASKS.md` — new Task #26 entry.

No controller, view, JS, schema, or package changes.

### Owner commands
```bash
dotnet build MimoShop.slnx
dotnet run --project MimoShop
```

### Manual validation
1. Open any form modal on a desktop viewport (≥768px) tall enough to clip the content, for example:
   - بطاقة صيانة جديدة (Home primary action or RepairTickets header).
   - إضافة قطعة / تعديل قطعة (Inventory).
   - إضافة مستخدم / تعديل المستخدم / إعادة كلمة المرور (Staff).
   - تغيير كلمة المرور (Account/Profile).
   - الإعدادات (sidebar).
   - استيراد من الكتالوج (Categories).
   - From `/RepairTickets/Details` — تعديل السعر والدفع, استهلاك قطعة, طلب قطعة.
   - From `/Categories/Index` — إضافة علامة / موديل / نوع / متغير.
2. Confirm that the **modal body** (not the page behind) scrolls, and the header and footer stay pinned to the top and bottom of the modal.
3. Resize the browser to <768px: the existing full-screen sheet layout should be unchanged.
4. Tab/Shift-Tab: focus should still cycle through all form fields without escaping into the page behind the backdrop.

---

## Previous Session — True Inline Modals (Task #24)

**Strategy:** Every previously-full-page form is now an *inline* Bootstrap modal opened over whichever page triggered it. Triggers carry `data-open-modal="key"`; the form is fetched from a new `*Modal` controller action into a shared host (`#mimoModalHost` in `_Layout`), shown via Bootstrap's modal API, and submitted via `fetch` with `X-Requested-With: XMLHttpRequest`. POST actions return `Json({ ok, redirectUrl | replace | reload, message })` on success and a `PartialView` of the same modal on validation failure, which the JS swaps in place. No full-page navigation, no `data-modal-autoopen` / `data-modal-cancel-url` plumbing, no separate "modal page" view files.

**Trigger-to-key map** (all in `wwwroot/js/site.js → MODAL_URLS`):
- `repairCreate` — Home primary action, RepairTickets Index header & empty-state.
- `staffCreate` / `staffEdit{arg}` / `staffResetPassword{arg}` — Staff Index.
- `inventoryForm{arg|}` (empty arg = create) — Inventory Index header, empty-state, row "تعديل".
- `changePassword` — Account/Profile footer.
- `shopSettings` — sidebar "الإعدادات" link (now a button).
- `categoriesImport` — Categories Index header (SuperAdmin/Owner only).

**Notable details:**
- The submitter is read from `event.submitter` and appended to `FormData` so the two-button RepairTickets intake still distinguishes `action=print` (redirect to receipt) from `action=continue` (reload modal body in place + success toast).
- `Categories/ImportPreviewModal` returns `204 No Content` when there is nothing to import; the JS shows a page-level info toast and does **not** open a modal.
- Modal HTML is parsed via `innerHTML` and appended to the host, which auto-executes any inline `<script>` tags — this is how per-partial wiring (phone lookup, brand pills, TomSelect, model picker, import counter) runs after injection.
- All confirm-modal intercepts (`data-confirm-modal` on Staff activate/deactivate, RepairTickets status transitions + ReceiveOnDemandPart, Categories activate, sidebar logout) are unchanged.
- The inline `RepairTickets/Details` modals (payment, consume part, request part) and the inline `Categories/Index` entity modals (`catBrandModal` etc.) are also unchanged.

### Files added (partials)
- `MimoShop/Views/Shared/_ModalHost.cshtml`
- `MimoShop/Views/RepairTickets/_CreateModalPartial.cshtml`
- `MimoShop/Views/Staff/_CreateModalPartial.cshtml`, `_EditModalPartial.cshtml`, `_ResetPasswordModalPartial.cshtml`
- `MimoShop/Views/Inventory/_FormModalPartial.cshtml`
- `MimoShop/Views/Account/_ChangePasswordModalPartial.cshtml`
- `MimoShop/Views/ShopSettings/_SettingsModalPartial.cshtml`
- `MimoShop/Views/Categories/_ImportPreviewModalPartial.cshtml`

### Files deleted (no longer reachable)
- `MimoShop/Views/RepairTickets/Create.cshtml`
- `MimoShop/Views/Staff/Create.cshtml`, `Edit.cshtml`, `ResetPassword.cshtml`
- `MimoShop/Views/Inventory/Form.cshtml`
- `MimoShop/Views/Account/ChangePassword.cshtml`
- `MimoShop/Views/ShopSettings/Index.cshtml`
- `MimoShop/Views/Categories/ImportPreview.cshtml`

### Files changed (controllers, one new `*Modal` GET per area, AJAX branch on POSTs)
- `Controllers/RepairTicketsController.cs`, `StaffController.cs`, `InventoryController.cs`, `AccountController.cs`, `ShopSettingsController.cs`, `CategoriesController.cs`
- `Views/Shared/_Layout.cshtml` — adds `@await Html.PartialAsync("_ModalHost")`; sidebar "الإعدادات" converted to `<button data-open-modal="shopSettings">`.
- `Views/Home/Index.cshtml`, `RepairTickets/Index.cshtml`, `Staff/Index.cshtml`, `Inventory/Index.cshtml`, `Account/Profile.cshtml`, `Categories/Index.cshtml` — trigger buttons use `data-open-modal` (with `data-modal-arg` where needed).
- `wwwroot/js/site.js` — replaced `initAutoOpen` / `data-modal-autoopen` plumbing with a generic `MimoShop.modal.{openModal, closeModal, submitForm, reloadModal}`. `initTomSelect` is exported so dynamically inserted forms can initialise TomSelect.
- `wwwroot/css/site.css` — `button.nav-item` and `button.primary-action` resets so the new `<button>` triggers render like the previous `<a>` links.

### Owner commands
```bash
dotnet build MimoShop.slnx
dotnet run --project MimoShop
```
No migration, no NuGet changes, no schema changes. Agent ran `dotnet build` once to confirm zero errors; the running app was killed before handoff.

### Manual validation — Inline modals
1. **From `/` (Home),** click "استقبال جهاز جديد". Modal opens over the home page, **no navigation**. Phone input is focused. Submit empty → inline errors. Submit valid → modal closes, browser navigates to the receipt page for the new `REP-XXXX`. Submit valid with "حفظ ومتابعة" → modal stays, form resets, success toast at the top of the body.
2. **From `/RepairTickets/Index` header & empty-state** "بطاقة صيانة جديدة" buttons: same flow as #1.
3. **From `/Staff/Index`:**
   - "إضافة مستخدم" → modal opens over the list; submit empty → errors; submit valid → modal closes, list reloads with the new row.
   - Per-row "تعديل" → modal opens for that staff; change role/display name; submit → modal closes, list updates.
   - Per-row "إعادة كلمة المرور" → modal opens; submit empty → errors; submit valid → modal closes, list shows the success banner.
4. **From `/Inventory/Index`:**
   - Header "إضافة قطعة" → modal opens; brand pills, model TomSelect, type/variant selects, image upload, quantity/prices all work; submit → modal closes, list shows the new card.
   - Per-card "تعديل ←" → modal opens pre-filled; change anything; submit → modal closes, card updates.
5. **From `/Account/Profile`:** "تغيير كلمة المرور" → modal opens; submit empty → errors; submit valid → modal closes, profile reloads with success banner.
6. **From any page, sidebar "الإعدادات"** button: modal opens; change the shop name, latin name, hours, phone, etc. Save → modal closes, sidebar/footer/receipt logo and brand names refresh on reload. Upload a new logo (PNG/JPG/WEBP ≤ 2 MB) → modal closes, layout shows the new logo on the next reload. Remove the logo → it falls back to the text mark.
7. **From `/Categories/Index`** (SuperAdmin/Owner only): "استيراد من الكتالوج" → modal opens; if there is nothing to import the page shows an info toast and the modal does **not** open. Otherwise select models and click "استيراد المحدد" → modal closes, list reloads with the imported rows.
8. **Existing flows that should still work unchanged:**
   - `/RepairTickets/Details` — inline payment/parts modals, confirm-modal on status transitions, "Done→Collected" flow, receive on-demand part.
   - `/Categories/Index` — entity create/edit modals (`catBrandModal`, `catModelModal`, `catTypeModal`, `catVariantModal`) and the activate/deactivate confirm-modal intercepts.
   - `/Inventory/Index` — "جلب صورة" per-card image-fetch dialog, search and apply.
   - `/Staff/Index` — activate/deactivate confirm-modal intercepts.
   - Sidebar logout — confirm-modal intercept.
9. **Mobile (< 768px):** repeat #1–#7 on a narrow viewport. Each modal should cover the shell, backdrop dim the page, focus jump into the form, Escape/backdrop close the modal cleanly, and the page underneath should not scroll while the modal is open.
10. **Direct URLs** like `/RepairTickets/Create`, `/Staff/Create`, `/Inventory/Form`, `/ShopSettings/Index`, `/Categories/ImportPreview` are no longer reachable — they 404 or route to a non-existent action.

---

## Current State

- **Latest (Inventory Index Filter Bar + Card Layout):** implemented, awaiting owner manual validation (steps above).
- **Task #24:** True Inline Modals — implemented, awaiting owner manual validation (steps above).
- **Older tasks still pending owner validation:** #19 (Shop Logo), #20 (Repair Intake Redesign), #21 (Mobile UX/UI), #22 (Model Picker), #23 (UI/UX Overhaul), #25 (Price Input UX). Full validation steps live in `TASKS.md` under each task's "Manual Validation Needed" section. Task #19 also still needs the `AddShopLogoUrl` migration scaffolded before running.
- All other PRD slices are **Implemented - Owner Validated**.

## Owner Commands

```bash
dotnet build MimoShop.slnx
dotnet run --project MimoShop
```

For task #19 only (Shop Logo), the EF migration is also needed once:
```bash
dotnet ef migrations add AddShopLogoUrl --project MimoShop --startup-project MimoShop
```

## Next Steps

- Owner runs build + walks the latest Inventory Index manual validation above.
- Owner then walks the Task #24 manual validation and flips Task #24 in `TASKS.md` to "Implemented - Owner Validated".
- Owner then walks the remaining pending validations (#19, #20, #21, #22, #23, #25) using the steps in `TASKS.md`, flipping each in turn.

## Blockers

None.
