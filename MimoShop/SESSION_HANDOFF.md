# Session Handoff — 2026-07-04

## Completed: UI/UX overhaul (SKILL.md-informed) + Tom Select search dropdowns

### Changes summary

#### 1. Design tokens (`site.css` :root)
- **Borders** darkened: `--color-line` #e2e8f0→#cbd5e1, `--color-line-soft` #f1f5f9→#e2e8f0
- **Text contrast**: `--color-text` #334155→#1e293b, `--color-text-soft` #64748b→#475569
- **Canvas** differentiated from surface: `--color-canvas` #f8fafc→#f1f5f9
- **Shadows** stronger: card shadow opacity doubled, elevated shadow for modals
- **Pill border tokens** added for all status/payment variants

#### 2. Focus ring unification (`site.css`)
- Removed all `outline: 0` kills (form controls, model picker, bottom nav, filter search, primary-action input)
- Global `:focus-visible` outline now applies consistently
- Focus box-shadow bumped from 0.18→0.22 opacity

#### 3. App header bar (`_Layout.cshtml` + `site.css`)
- New sticky `<header class="app-header">` with white bg, visible bottom border, brand-colored left accent
- Breadcrumb moved from `<main>` into the header
- Header CSS uses `var(--shadow-header)` and `position: sticky; z-index: 45`

#### 4. Status pill borders (`site.css`)
- Added `border: 1px solid transparent` to `.pill` base class
- Each `.pill--*` variant now has a `border-color` with 18-25% opacity of its text color

#### 5. Button touch targets (`site.css`)
- `.btn` `min-height` 42px→44px (meets Apple HIG 44pt minimum)

#### 6. Modal styling (`site.css`)
- `.modal-content`: border, radius, elevated shadow
- `.modal-header`: brand-tint background, bottom border
- `.modal-footer`: top border

#### 7. Tom Select search-selects on large dropdowns
- **Vendor directory created**: `wwwroot/lib/tom-select/` (empty — see below)
- **Assets referenced** in `_Layout.cshtml`: `tom-select.default.min.css` after Bootstrap CSS, `tom-select.complete.min.js` after Bootstrap JS
- **Global init** in `site.js`: auto-enhances all `<select class="ts-select">`
- **CSS theme** in `site.css`: `.ts-control`, `.ts-dropdown`, `.option`, `.create` themed to design tokens
- **Applied to**:
  - `Inventory/Form.cshtml:50` — `PhoneModelId` (with brand-filter re-sync logic)
  - `RepairTickets/Details.cshtml:488` — `InventoryPartId` (stocked parts)
  - `RepairTickets/Details.cshtml:527` — `InventoryPartId` (on-demand parts)

### ⚠️ Owner commands REQUIRED

#### Download Tom Select files
```
curl -L -o wwwroot/lib/tom-select/tom-select.complete.min.js https://cdn.jsdelivr.net/npm/tom-select@2.4.3/dist/js/tom-select.complete.min.js
curl -L -o wwwroot/lib/tom-select/tom-select.default.min.css https://cdn.jsdelivr.net/npm/tom-select@2.4.3/dist/css/tom-select.default.min.css
```

Or manually from: https://github.com/oir/orchid-js-tom-select/releases/tag/v2.4.3

#### Build and run
```
dotnet build MimoShop.slnx
dotnet run --project MimoShop
```

### Manual validation checklist

**Overall look**
- [ ] Every page: visible card borders and table cell separators (no more ghost borders)
- [ ] Breadcrumb appears in the new sticky header bar with brand accent line
- [ ] Status pills have visible borders (status: جديد, قيد الإصلاح, منتهي / payment: مدفوع, غير مدفوع)
- [ ] Modals have teal-tinted header bar with clear bottom border
- [ ] Buttons are 44px tall minimum (touchable)
- [ ] Page background is slightly darker (#f1f5f9), differentiating from card surfaces

**Focus/accessibility**
- [ ] Tab through page: every focusable element shows a visible ring (keyboard nav)
- [ ] Click on form inputs: teal glow ring appears
- [ ] No element loses focus ring (the `outline:0` bug is fixed)

**Tom Select — Inventory Form**
- [ ] Open Inventory → Add part (or Edit)
- [ ] Click model dropdown: Tom Select dropdown appears with search field
- [ ] Type "1" or letters: options filter live
- [ ] Pick a model, click a brand tab → model dropdown repopulates and Tom Select re-syncs
- [ ] Submit the form: PhoneModelId posts correctly

**Tom Select — Repair Ticket Details**
- [ ] Open a ticket → consume stocked part modal
- [ ] Click part dropdown: Tom Select with search
- [ ] Type a few letters, select a part, submit → correct InventoryPartId posts
- [ ] Same for on-demand part request modal

**Mobile**
- [ ] All touch targets feel comfortable (44px buttons, .form-select height)
- [ ] Tom Select dropdown dismisses on outside tap, keyboard opens on search
- [ ] Bottom nav, hamburger, drawer all work unchanged

**Edge cases**
- [ ] Home page (`/`) has no breadcrumb → header bar still shows (empty but present)
- [ ] Login page uses `_AuthLayout` → no header bar, no sidebar. Design tokens still apply.
- [ ] Inventory/Form opened from Index page (modal auto-open) → header bar behind modal still visible
- [ ] Staff/Create and Edit → small role `<select>` is NOT Tom Select (no `.ts-select` class) — native dropdown still works

### Files changed
| File | Change |
|---|---|
| `wwwroot/css/site.css` | Design tokens, focus, header, pills, buttons, modals, Tom Select theme |
| `wwwroot/js/site.js` | Tom Select global init on `.ts-select` |
| `Views/Shared/_Layout.cshtml` | Header bar HTML, TS CSS/JS references |
| `Views/Inventory/Form.cshtml` | `.ts-select` class + brand-filter re-sync |
| `Views/RepairTickets/Details.cshtml` | `.ts-select` on 2 InventoryPartId selects |
| `wwwroot/lib/tom-select/` | New directory (must download files) |
| `SESSION_HANDOFF.md` | This file |

### Next steps / deferred
- Apply Tom Select to the device-model bottom-sheet in `RepairTickets/Create.cshtml` (currently a custom searchable panel — could migrate to TS for consistency)
- Add a `--color-line-canvas` usage (token defined but not yet applied)
- Consider unified `.section-title` class to replace 7+ different title selectors
