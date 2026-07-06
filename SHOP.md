# MimoShop Public Parts Storefront — Product & Architecture Specification

**V1.0 · July 2026**

---

## 1. Overview

A public-facing Arabic-language e-commerce website for selling phone spare parts via **cash on delivery (COD)**. No online payments, no billing, no user accounts. Customers browse the catalog, fill a cart, submit an order with their delivery details, and pay when the package arrives.

The storefront integrates into the existing MimoShop project as an **ASP.NET Core Area** called `Public`, sharing the same codebase, database, and service layer as the staff portal. The staff manage orders from the existing authenticated back-office.

---

## 2. Goals

### V1 goals

- Let anyone browse available parts by brand, model, and type with prices and images
- Let anonymous visitors build a cart and place a COD order
- Let staff view, confirm, and ship orders from the existing staff portal
- Let staff manage the wilaya and commune delivery zones (add, edit, remove, set shipping fees) so the data stays accurate without a developer
- Keep the public storefront visually and functionally separate from the staff back-office

### Out of scope for V1

- Online payments or payment gateway integration
- Customer accounts, registration, or login
- Order tracking page for customers (order lookup via Telegram bot is deferred)
- Inventory reservation (placing an order does not lock stock)
- Product search by keyword (only structured drill-down browsing)
- Reviews, ratings, wishlists, or recommendations
- Promo codes or discounts

---

## 3. Users & Roles

| Role | Description |
|------|-------------|
| **Visitor (anonymous)** | Browses the catalog, fills a session cart, submits a COD order. No account needed. |
| **Staff (existing)** | Views incoming orders, confirms them, marks as shipped or delivered. Manages wilaya/commune delivery zones. Uses the existing staff portal. |
| **Owner (existing)** | Same as staff, plus sees order counts on the dashboard. Sets per-wilaya shipping fees. |

---

## 4. Architecture

### 4.1 Same project, same database, Area-based separation

The storefront lives inside the MimoShop project as an **Area** named `Public`. This means:

- **One deployment** — no separate hosting, no API calls between projects
- **Shared database** — the storefront reads `InventoryPart`, `Brand`, `PhoneModel`, `PartType`, `PartVariant` directly
- **Shared services** — `PartsCatalogQueryService` is reused for catalog browsing, `ShopSettingsService` for shop contact info
- **Separate layout** — the public storefront has its own `_PublicLayout.cshtml` with a light, customer-facing design
- **Separate auth** — public controllers use `[AllowAnonymous]`; staff controllers stay auth-required

### 4.2 Route structure

| URL pattern | Area | Auth | Purpose |
|-------------|------|------|---------|
| `/Public/` | Public | Anonymous | Storefront landing page |
| `/Public/Catalog/...` | Public | Anonymous | Browse parts |
| `/Public/Cart/...` | Public | Anonymous | View/manage cart |
| `/Public/Checkout/...` | Public | Anonymous | Place order |
| `/DeliveryZones/...` | (default) | Authenticated | Staff wilaya/commune management |
| `/ShopOrders/...` | (default) | Authenticated | Staff order management |
| `/{controller}/{action}/{id?}` | (default) | Authenticated | Existing staff portal (unchanged) |

### 4.3 Technology choices

| Layer | Technology | Notes |
|-------|-----------|-------|
| Framework | ASP.NET Core MVC (Areas) | Existing project, no new frameworks |
| Database | Shared `MimoShopDbContext` (SQL Server) | New entities added to existing context |
| Cart storage | Server-side session (`IDistributedCache` in-memory) | Lost on session expiry. Acceptable for anonymous shoppers. |
| UI | Razor Views, Bootstrap 5 RTL, custom CSS | Light theme, separate from staff portal |
| Images | Existing `wwwroot/uploads/catalog/` | Reuse images already attached to brands, models, parts |
| Delivery zones | `Wilaya` + `Commune` tables, staff-managed | Admin CRUD, seeded initially, then maintained by staff |

---

## 5. Data Model — New Entities

### 5.1 Wilaya

Algeria has 58 wilayas (administrative provinces). Each has a numeric code (01–58) and Arabic + French names.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `Id` | `int` | PK, identity | |
| `Code` | `string(2)` | Required, unique | Zero-padded numeric code "01"–"58" |
| `NameAr` | `string(100)` | Required, unique | Arabic name |
| `NameFr` | `string(100)` | Required, unique | French name |
| `ShippingFee` | `decimal(18,2)` | Required, default 0 | Delivery fee in DZD |
| `IsActive` | `bool` | Required, default true | Deactivated wilayas hidden from checkout dropdown |

**Unique index:** `Code`, `NameAr`, `NameFr`

**Seed data:** All 58 wilayas with codes, Arabic names, French names. Initial `ShippingFee` = 0. Owner adjusts fees via the Delivery Zones management page.

Because wilaya/commune data sources are often inaccurate or outdated, the seed data is a starting point only. Staff can add, edit, rename, merge, or delete communes at any time through the admin UI (Slice 20).

### 5.2 Commune

Each wilaya contains multiple communes (municipalities). The customer selects a wilaya first, then a commune within it.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `Id` | `int` | PK, identity | |
| `WilayaId` | `int` | FK → Wilaya, Restrict delete | |
| `NameAr` | `string(100)` | Required | Arabic name |
| `NameFr` | `string(100)` | Required | French name — required by Yalidine and most delivery APIs which expect commune names in French (Arabic can fail) |
| `IsActive` | `bool` | Required, default true | Deactivated communes hidden from checkout dropdown |

**Unique index:** `{WilayaId, NameAr}`

**Seed data:** Communes for all 58 wilayas (best-effort sourcing from official Algerian administrative data). Staff can correct, add, or remove communes through the admin UI at any time.

### 5.3 ShopOrder

The main order entity. COD only — no payment fields.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `Id` | `int` | PK, identity | |
| `OrderCode` | `string(12)` | Required, unique | Human-readable code, format `ORD-XXXX` |
| `CustomerName` | `string(120)` | Required | |
| `CustomerPhone` | `string(40)` | Required | For delivery contact |
| `CustomerWhatsApp` | `string(40)` | Optional | Alternative contact |
| `WilayaId` | `int` | FK → Wilaya, Restrict delete | |
| `CommuneId` | `int` | FK → Commune, Restrict delete | |
| `WilayaName` | `string(100)` | Required | Snapshot at order time |
| `CommuneName` | `string(100)` | Required | Snapshot of Arabic commune name |
| `CommuneNameFr` | `string(100)` | Required | Snapshot of French commune name (required for delivery API calls) |
| `Address` | `string(300)` | Required | Street / neighborhood / landmark |
| `ShipmentId` | `int?` | FK → Shipment, Set null | Set when staff creates a shipment via delivery provider API — links the order to its shipment record |
| `Notes` | `string(500)` | Optional | Customer instructions |
| `Status` | `string(30)` | Required, default "New" | Lifecycle: see §5.6 |
| `Subtotal` | `decimal(18,2)` | Required | Sum of line totals before shipping |
| `ShippingFee` | `decimal(18,2)` | Required | Snapshot of wilaya fee at order time |
| `TotalAmount` | `decimal(18,2)` | Required | Subtotal + ShippingFee |
| `CreatedAt` | `DateTime` | Required, default now | |
| `ConfirmedAt` | `DateTime?` | | Set when staff confirms the order |
| `ShippedAt` | `DateTime?` | | Set when staff marks as shipped |
| `DeliveredAt` | `DateTime?` | | Set when staff marks as delivered |
| `ReturnedAt` | `DateTime?` | | Set when staff marks order as returned (refused or uncollected) |
| `ReturnReason` | `string(300)` | Optional | Why the order was returned (e.g. refused delivery, customer didn't collect) |
| `CancelledAt` | `DateTime?` | | Set if staff cancels the order |
| `CancelledReason` | `string(300)` | Optional | Why the order was cancelled |

`WilayaName`, `CommuneName`, and `CommuneNameFr` are snapshotted alongside the foreign keys so that order details remain readable even if a wilaya or commune is renamed or deactivated later.

### 5.4 ShopOrderLine

One row per part in the order. Prices and names are **snapshotted** at order time — changes to inventory prices later do not affect existing orders.

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `Id` | `int` | PK, identity | |
| `ShopOrderId` | `int` | FK → ShopOrder, Cascade delete | |
| `InventoryPartId` | `int` | FK → InventoryPart, Set null | Set null if the part is deleted later |
| `BrandName` | `string(60)` | Required | Snapshot |
| `ModelName` | `string(80)` | Required | Snapshot |
| `PartTypeName` | `string(80)` | Required | Snapshot |
| `VariantName` | `string(80)` | Required | Snapshot |
| `PartDisplayName` | `string(300)` | Required | Computed: "Samsung Galaxy A54 Screen OEM" |
| `Quantity` | `int` | Required, ≥ 1 | |
| `UnitPrice` | `decimal(18,2)` | Required | Sale price at order time |
| `ImageUrl` | `string(500)` | Optional | Snapshot of part thumbnail at order time |

### 5.5 Admin action log (optional enhancement)

If future audit needs arise, a `ShopOrderStatusChange` table can track who changed each status and when — similar to `RepairStatusHistory`. For V1, the `*At` timestamp columns on `ShopOrder` are sufficient.

### 5.6 Order Status Lifecycle

```
New → Confirmed → Shipped → Delivered
                       └→ Returned
  └→ Cancelled
```

| Status | Who | Meaning | Stock effect |
|--------|-----|---------|-------------|
| **New** | System | Order just placed by customer. Awaiting staff review. | None |
| **Confirmed** | Staff | Staff verified the order has all items in stock and confirmed it for shipping. | Stock decremented |
| **Shipped** | Staff | Order sent with delivery service. | None (already decremented) |
| **Delivered** | Staff | Package arrived and customer paid (COD). | None |
| **Returned** | Staff | Customer refused the package at delivery, or didn't collect it from the pickup point. No payment collected. Stock returns to inventory. | Stock restored |
| **Cancelled** | Staff | Order cancelled before shipping (out of stock, unreachable customer, etc.). | Stock restored if was confirmed |

Each status transition timestamps the corresponding `*At` column. Allowed transitions:

- **New** → Confirmed, Cancelled
- **Confirmed** → Shipped, Cancelled
- **Shipped** → Delivered, Returned
- All other transitions are blocked.

The `ReturnReason` field is required when marking an order as Returned. Common reasons: "رفض الاستلام" (customer refused delivery), "عدم التسليم" (customer didn't collect from pickup point). The `CancelledReason` field is required when cancelling a confirmed order, and optional when cancelling a new order (since the order was never committed to stock).

---

## 6. Cart — Session-Based, Anonymous

### 6.1 Storage

The cart is stored in **server-side session** using ASP.NET Core's `IDistributedCache` backed by in-memory store.

- `AddDistributedMemoryCache()` + `AddSession()` in `Program.cs`
- `app.UseSession()` in middleware pipeline (after routing, before auth)
- Session timeout: 30 minutes of inactivity (configurable via `SessionOptions.IdleTimeout`)

A `CartService` serializes/deserializes a `List<CartItem>` to/from session as JSON.

### 6.2 CartItem model (in-memory only, no database table)

```csharp
public sealed record CartItem(
    int InventoryPartId,
    string BrandName,
    string ModelName,
    string PartTypeName,
    string VariantName,
    string PartDisplayName,
    decimal UnitPrice,
    string ThumbnailUrl,
    int Quantity
);
```

### 6.3 CartService API

| Method | Description |
|--------|-------------|
| `GetCartAsync(ISession)` | Returns current `List<CartItem>` from session |
| `AddItemAsync(ISession, CartItem)` | Adds one item. If same `InventoryPartId` exists, increments quantity |
| `UpdateQuantityAsync(ISession, int inventoryPartId, int quantity)` | Sets quantity for a specific item. If quantity = 0, removes the item |
| `RemoveItemAsync(ISession, int inventoryPartId)` | Removes an item from cart |
| `ClearCartAsync(ISession)` | Empties the cart (called after order placement) |
| `GetCartSummaryAsync(ISession)` | Returns total item count and total price |

### 6.4 Stock validation

Stock is **not** reserved when items are added to the cart. Before checkout, the `CheckoutService` re-validates that all cart items still have `Quantity > 0` in `InventoryPart`. If any item is out of stock, the checkout page shows a warning and the customer can remove or adjust it.

---

## 7. Wilaya & Commune Management — Staff Admin

### 7.1 Why staff-managed (not seed-only)

Wilaya and commune data from public sources is often inaccurate — missing newly created communes, outdated names after administrative reorganizations, or spelling inconsistencies. Rather than requiring a developer to re-seed the database, staff can maintain the delivery zones directly from the staff portal.

### 7.2 DeliveryZonesController (default area, auth-required)

| Action | URL | Auth | Description |
|--------|-----|------|-------------|
| `Index` | `/DeliveryZones` | Any staff | Lists all wilayas with commune count, shipping fee, active status. Two tabs: Wilayas list, per-wilaya communes (shown when a wilaya is selected). |
| `CreateWilaya` | `/DeliveryZones/CreateWilaya` (GET/POST) | Any staff | Form: code, Arabic name, French name, shipping fee, active toggle. |
| `EditWilaya(int id)` | `/DeliveryZones/EditWilaya/5` (GET/POST) | Any staff | Edit wilaya details. Changing the shipping fee only affects new orders — existing orders keep their snapshotted fee. |
| `ToggleWilaya(int id)` | `/DeliveryZones/ToggleWilaya/5` (POST) | Any staff | Activate/deactivate a wilaya. Deactivated wilayas are hidden from the checkout dropdown but remain in the database for existing order references. |
| `Communes(int wilayaId)` | `/DeliveryZones/Communes/5` | Any staff | Lists all communes for a given wilaya with active status. Shown inline on the Index page or as a detail view. |
| `CreateCommune(int wilayaId)` | `/DeliveryZones/CreateCommune/5` (GET/POST) | Any staff | Form: Arabic name, French name, active toggle. Wilaya is pre-selected from the URL. |
| `EditCommune(int id)` | `/DeliveryZones/EditCommune/12` (GET/POST) | Any staff | Edit commune details. |
| `ToggleCommune(int id)` | `/DeliveryZones/ToggleCommune/12` (POST) | Any staff | Activate/deactivate a commune. Deactivated communes are hidden from the checkout dropdown. |
| `DeleteCommune(int id)` | `/DeliveryZones/DeleteCommune/12` (POST) | Any staff | Delete a commune. Only allowed if no `ShopOrder` rows reference it. Shows a confirmation prompt. |
| `DeleteWilaya(int id)` | `/DeliveryZones/DeleteWilaya/5` (POST) | Owner only | Delete a wilaya and all its communes. Only allowed if no `ShopOrder` rows reference the wilaya. Requires confirmation. Owner-only to prevent accidental deletion. |

### 7.3 Sidebar navigation

Add a new sidebar nav item: **مناطق التوصيل** (Delivery Zones) under "الإدارة" (Administration), alongside categories and shop settings. Accessible to all staff for viewing/editing; delete wilaya restricted to owner.

### 7.4 Bulk import (V2 consideration)

For initial setup or corrections, a future bulk import (CSV/JSON upload) can be added. V1 relies on seed data + manual staff edits via the CRUD pages above.

---

## 8. Public Area — Pages & Flows

### 8.1 Landing Page (`PublicHomeController.Index`)

The storefront homepage. Shows:

- Shop name, contact info (from `ShopSetting`)
- Featured brands grid (all active brands with images, linked to catalog)
- A search/filter entry point
- Link to Telegram bot for repair status

### 8.2 Catalog Browsing (`PublicCatalogController`)

Hierarchical drill-down, same pattern as the Telegram bot but rendered as web pages:

| Action | URL | Description |
|--------|-----|-------------|
| `Brands` | `/Public/Catalog/Brands` | Grid of active brands with stock, each card shows brand image + Arabic name |
| `Models(int brandId)` | `/Public/Catalog/Models/5` | Grid of phone models for a brand with stock |
| `PartTypes(int brandId, int modelId)` | `/Public/Catalog/PartTypes/5/12` | Grid of available part types |
| `Variants(int brandId, int modelId, int partTypeId)` | `/Public/Catalog/Variants/5/12/3` | List of available variants with sale price, image, "Add to cart" button |
| `PartDetail(int inventoryPartId)` | `/Public/Catalog/PartDetail/42` | Full part page: large image, full name, price, stock availability badge, "أضف إلى السلة" button |

**Navigation breadcrumb:** Brand → Model → Part Type → Variants, shown on every catalog page.

**Reuses:** `PartsCatalogQueryService` (or a new `PublicCatalogQueryService` that extends it with `DisplayNameAr` and `ThumbnailUrl`).

### 8.3 Cart Page (`PublicCartController`)

| Action | URL | Description |
|--------|-----|-------------|
| `Index` | `/Public/Cart` | Shows all cart items with image, name, price, quantity input, line total, remove button. Shows subtotal. "Proceed to checkout" button. |
| `Add(int inventoryPartId)` | `/Public/Cart/Add/42` (POST) | Adds item (or increments if already in cart). Redirects back to catalog or cart. |
| `UpdateQuantity(int inventoryPartId, int quantity)` | `/Public/Cart/UpdateQuantity` (POST) | Updates quantity; removes if 0. |
| `Remove(int inventoryPartId)` | `/Public/Cart/Remove/42` (POST) | Removes item from cart. |

### 8.4 Checkout Page (`PublicCheckoutController`)

| Action | URL | Description |
|--------|-----|-------------|
| `Index` | `/Public/Checkout` | Form: customer name, phone, WhatsApp (optional), wilaya dropdown (only active wilayas), commune dropdown (cascading, only active), address, notes. Shows order summary with line items, subtotal, shipping fee (from selected wilaya), total. |
| `GetCommunes(int wilayaId)` | `/Public/Checkout/GetCommunes/5` (JSON) | Returns active communes for a wilaya. Called by JavaScript on wilaya selection change. |
| `GetShippingFee(int wilayaId)` | `/Public/Checkout/GetShippingFee/5` (JSON) | Returns the shipping fee for a wilaya. Called by JavaScript to update the total on wilaya change. |
| `PlaceOrder` | `/Public/Checkout/PlaceOrder` (POST) | Validates all fields, re-checks stock, creates `ShopOrder` + `ShopOrderLine` rows, generates `OrderCode`, clears cart, redirects to confirmation. |
| `Confirmation(string orderCode)` | `/Public/Checkout/Confirmation/ORD-0042` | Shows order code, summary, and a message: "Your order has been placed. We will contact you to confirm." |

**Wilaya → Commune cascading:** When the customer selects a wilaya, a JavaScript call fetches the communes and the shipping fee. The commune dropdown populates, and the total price updates immediately.

**Only active wilayas and communes** appear in the checkout dropdowns. Staff-controlled visibility via the Delivery Zones admin page.

---

## 9. Staff Portal — Order Management

### 9.1 New Controller: `ShopOrdersController` (in default area, auth-required)

| Action | Auth | Description |
|--------|------|-------------|
| `Index` | Any staff | List of all orders, filterable by status (New, Confirmed, Shipped, Delivered, Returned, Cancelled). Sorted newest first. |
| `Details(int id)` | Any staff | Full order detail: customer info, delivery address (wilaya + commune + address), line items with images, status timeline, action buttons. |
| `Confirm(int id)` | Any staff (POST) | Moves order from New → Confirmed. Sets `ConfirmedAt`. Decrements inventory quantities. |
| `Ship(int id)` | Any staff (POST) | Moves order from Confirmed → Shipped. Sets `ShippedAt`. |
| `Deliver(int id)` | Any staff (POST) | Moves order from Shipped → Delivered. Sets `DeliveredAt`. |
| `Return(int id)` | Any staff (POST) | Moves order from Shipped → Returned. Requires a reason. Sets `ReturnedAt` + `ReturnReason`. Reverses inventory decrements (stock returns to shelves). |
| `Cancel(int id)` | Any staff (POST) | Moves order from New/Confirmed → Cancelled. Requires a reason if order was confirmed. Sets `CancelledAt` + `CancelledReason`. If order was confirmed, reverses inventory decrements. |

### 9.2 Sidebar Navigation Update

Add a new sidebar nav item: **الطلبات** (Orders) linking to `/ShopOrders`. Badge shows count of "New" orders.

### 9.3 Dashboard Update

Add to the owner dashboard:

- New orders count (orders with status = "New")
- Orders shipped today
- Revenue from delivered orders today (DZD)

### 9.4 Inventory Integration

When staff confirms an order (`Confirm` action), the system decrements `InventoryPart.Quantity` for each line item and records `InventoryStockMovement` entries (same pattern as repair part consumption, `MovementType = "ShopOrder"`).

If stock is insufficient for any line item, the confirm action fails with a clear error showing which parts are short.

If a confirmed order is cancelled, the stock decrements are reversed with `InventoryStockMovement` entries (`MovementType = "ShopOrderCancel"`).

If a shipped order is returned (refused or uncollected), the stock decrements are reversed with `InventoryStockMovement` entries (`MovementType = "ShopOrderReturn"`). This restores all items to inventory since returns are whole-order only — no partial returns in V1.

---

## 10. Layout & Design

### 10.1 Separate visual identity

The public storefront uses a **light, modern e-commerce theme** — completely different from the dark admin sidebar portal.

| Element | Staff Portal | Public Storefront |
|---------|-------------|------------------|
| **Layout** | `_Layout.cshtml` (dark sidebar, bottom nav) | `_PublicLayout.cshtml` (light header, clean) |
| **Theme** | Dark editorial (slate backgrounds, teal accents) | Light/white background, brand teal for accents |
| **Navigation** | Sidebar + bottom nav pill | Top navbar + mobile hamburger |
| **Footer** | Technical spec sheet with diagnostics | Customer-facing: shop contact, links, copyright |
| **Fonts** | Cairo + Tajawal (shared) | Cairo + Tajawal (shared, same CDN) |
| **Direction** | RTL, Arabic | RTL, Arabic |
| **Auth** | Cookie auth, login wall | Anonymous, no login |
| **CSS** | `site.css` (existing 3800-line design system) | `public-site.css` (new, storefront-only, can import shared tokens) |

### 10.2 _PublicLayout.cshtml structure

```
┌────────────────────────────────────────────┐
│  Header: Logo | Search | Cart (badge)     │
│  Mobile: Logo | Cart | ☰                  │
├────────────────────────────────────────────┤
│  Breadcrumb (on catalog pages)            │
│                                           │
│  Page Content                             │
│                                           │
├────────────────────────────────────────────┤
│  Footer: Shop info | Contact | Telegram   │
└────────────────────────────────────────────┘
```

### 10.3 Shared CSS tokens

The storefront CSS (`public-site.css`) can import the CSS custom properties from the existing design system (brand colors, fonts, radii, shadows) but defines its own layout classes (header, product grid, cart table, checkout form). This keeps the visual identity consistent without coupling the two layouts.

### 10.4 Responsive design

- Mobile-first (most shoppers will browse on phones)
- Product cards: 2 columns on mobile, 4 on desktop
- Cart: full-width stacked on mobile, table on desktop
- Checkout: single-column on mobile, two-column on desktop (form left, summary right)

---

## 11. Program.cs Wiring Changes

### 11.1 New service registrations

```csharp
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddScoped<CartService>();
builder.Services.AddScoped<PublicCatalogQueryService>();
builder.Services.AddScoped<ShopOrderService>();
builder.Services.AddScoped<DeliveryZoneService>();
```

### 11.2 Middleware

```csharp
app.UseSession();  // After UseRouting, before UseAuthentication
```

### 11.3 Area routes

```csharp
app.MapAreaControllerRoute(
    name: "public",
    areaName: "Public",
    pattern: "Public/{controller=Catalog}/{action=Brands}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
```

The public area route is registered **before** the default route so `/Public/*` URLs are matched first.

---

## 12. Implementation Slices

### Slice 16: Area Setup + Public Layout + Landing Page

**Blocked by:** None (builds on existing project)

**Business value:** Establishes the Area structure, the public layout, and a landing page — the foundation for all storefront features.

**Acceptance criteria:**
- [ ] `Areas/Public/` directory structure created with Controllers, Views, ViewModels folders
- [ ] `Program.cs` updated: Area route, session middleware, service registrations
- [ ] `_PublicLayout.cshtml` created with light theme header, footer, responsive shell
- [ ] `public-site.css` created with shared CSS tokens and storefront-specific styles
- [ ] `PublicHomeController` with `Index` action, `[AllowAnonymous]`
- [ ] Landing page shows shop name (from `ShopSettingsService`), brand grid, Telegram bot link
- [ ] Global auth still protects all staff controllers — only `Public` area is anonymous
- [ ] Mobile-responsive: works on phone and desktop

**Owner commands needed:**
- `dotnet build MimoShop.slnx`

**Manual validation:**
- Navigate to `/Public/` while not logged in — should see the landing page
- Navigate to `/Home/Index` while not logged in — should redirect to login
- Check mobile and desktop layouts

---

### Slice 17: Public Catalog Browsing

**Blocked by:** Slice 16 (Area Setup + Public Layout + Landing Page)

**Business value:** Lets visitors browse all available parts with images and prices — the core storefront feature.

**Acceptance criteria:**
- [ ] `PublicCatalogController` with Brands, Models, PartTypes, Variants, PartDetail actions, `[AllowAnonymous]`
- [ ] Brand → Model → Part Type → Variant drill-down with breadcrumb navigation
- [ ] Each level shows product cards with images, Arabic `DisplayNameAr`, and prices (DZD)
- [ ] Only in-stock, active items are shown (reuses stock filtering logic)
- [ ] PartDetail page shows large image, full name, price, availability badge, "أضف إلى السلة" button (non-functional placeholder until Slice 18)
- [ ] All pages work without login

**Owner commands needed:**
- `dotnet build MimoShop.slnx`

**Manual validation:**
- Browse a brand → model → part type → variant flow
- Verify only stocked items appear
- Verify Arabic names and prices are correct
- Check mobile layout
- Verify `/Public/Catalog/Brands` is accessible without login

---

### Slice 18: Session Cart

**Blocked by:** Slice 17 (Public Catalog Browsing)

**Business value:** Lets visitors collect parts before placing an order.

**Acceptance criteria:**
- [ ] `CartService` stores `List<CartItem>` in session as JSON
- [ ] "Add to cart" button on PartDetail and Variants pages (POST, `[AllowAnonymous]`)
- [ ] Cart page (`/Public/Cart`) shows all items with image, name, unit price, quantity input, line total, remove button
- [ ] Cart header badge shows total item count (read from session on each request)
- [ ] Quantity can be updated; setting to 0 removes the item
- [ ] Subtotal displayed at bottom of cart page
- [ ] "Proceed to checkout" button links to checkout page (placeholder until Slice 21)
- [ ] Cart persists across page navigations within the session
- [ ] Cart is lost when session expires (acceptable V1 behavior)

**Owner commands needed:**
- `dotnet build MimoShop.slnx`

**Manual validation:**
- Add 3 different parts to cart
- Change quantities on cart page
- Remove one item
- Navigate away and return — cart persists
- Verify cart badge in header updates
- Verify mobile cart layout

---

### Slice 19: Wilaya + Commune Seed Data

**Blocked by:** None (no UI dependency, pure data layer)

**Business value:** Populates the database with the 58 wilayas and their communes so that the delivery zone management and checkout pages have data to work with.

**Acceptance criteria:**
- [ ] `Wilaya` and `Commune` entities added to `MimoShopDbContext` with entity configuration (unique indexes, max-length, FK constraints, `IsActive` defaults to `true`)
- [ ] `ShippingFee` column on `Wilaya` defaults to 0
- [ ] Seed data in `OnModelCreating`: all 58 wilayas with `Code`, `NameAr`, `NameFr`
- [ ] Seed data: communes for all 58 wilayas with `NameAr`, `NameFr`, linked to their wilaya
- [ ] Existing entities, migrations, and data unchanged
- [ ] Migration created and buildable

**Owner commands needed:**
- `dotnet ef migrations add AddWilayaCommune --project MimoShop --startup-project MimoShop`

**Manual validation:**
- After migration applied, verify `Wilayas` table has 58 rows
- Verify `Communes` table has data for at least one wilaya
- Verify `IsActive` defaults to 1 on both tables

---

### Slice 20: Wilaya + Commune Staff Management

**Blocked by:** Slice 19 (Wilaya + Commune Seed Data)

**Business value:** Lets staff maintain delivery zone data directly — add missing communes, fix spelling errors, deactivate wrong entries, set shipping fees per wilaya. Eliminates dependency on a developer for data corrections.

**Acceptance criteria:**
- [ ] `DeliveryZonesController` in default area (auth-required) with all CRUD actions per §7.2
- [ ] Index page: list of wilayas showing code, Arabic name, French name, shipping fee (DZD), commune count, active status toggle
- [ ] Click a wilaya to expand/show its communes with Arabic name, French name, active status toggle
- [ ] Create/Edit wilaya form: code, Arabic name, French name, shipping fee, active toggle
- [ ] Create/Edit commune form: Arabic name, French name, active toggle (wilaya pre-selected)
- [ ] Toggle active/inactive on wilaya or commune (single-click, no confirm needed)
- [ ] Inactive wilayas/communes clearly marked but still visible in the admin list
- [ ] Delete commune: allowed only if no `ShopOrder` references it. Blocked with error otherwise.
- [ ] Delete wilaya (owner-only): allowed only if no `ShopOrder` references it. Blocked with error otherwise. Cascade-deletes all communes under that wilaya.
- [ ] Sidebar nav item "مناطق التوصيل" added under "الإدارة"
- [ ] All Arabic RTL layout, consistent with existing admin pages (Categories management pattern)
- [ ] `DeliveryZoneService` handles business logic (uniqueness checks, referential integrity guards, CRUD operations)

**Owner commands needed:**
- `dotnet build MimoShop.slnx`

**Manual validation:**
- Add a new commune to an existing wilaya — verify it appears in the list
- Edit a wilaya shipping fee — verify it updates
- Toggle a commune inactive — verify it stays in the list but marked inactive
- Try to delete a wilaya that has no orders — should succeed
- Try to delete a wilaya that has orders — should be blocked with error
- Create a wilaya with a duplicate code — should be rejected

---

### Slice 21: COD Checkout

**Blocked by:** Slice 18 (Session Cart), Slice 19 (Wilaya + Commune Seed Data), Slice 20 (Wilaya + Commune Staff Management)

**Business value:** Completes the purchase flow — customers can place orders for delivery.

**Acceptance criteria:**
- [ ] `ShopOrder` and `ShopOrderLine` entities added to `MimoShopDbContext`
- [ ] Migration created for ShopOrder + ShopOrderLine tables
- [ ] Checkout form: customer name, phone, WhatsApp (optional), wilaya dropdown (only active), commune dropdown (cascading via JSON, only active), address, notes
- [ ] Order summary shows line items, subtotal, shipping fee (from selected wilaya's `ShippingFee`), total
- [ ] Shipping fee and commune list update dynamically when wilaya changes (JavaScript)
- [ ] Stock re-validation on submit — out-of-stock items flagged with error
- [ ] Order code generated in `ORD-XXXX` format (sequential, unique)
- [ ] Wilaya name and commune name snapshotted on the order alongside FK IDs
- [ ] Order saved to database, cart cleared, customer redirected to confirmation page
- [ ] Confirmation page shows order code, order summary, "We will contact you" message
- [ ] Empty cart checkout rejected with redirect to cart page
- [ ] All checkout pages work without login (`[AllowAnonymous]`)

**Owner commands needed:**
- `dotnet ef migrations add AddShopOrderTables --project MimoShop --startup-project MimoShop`

**Manual validation:**
- Fill checkout form — select a wilaya, verify communes load and shipping fee updates
- Submit order with valid data — verify order appears in database with correct line items
- Verify cart is empty after order
- Verify snapshotted wilaya/commune names on the order
- Try to checkout with an empty cart — should redirect to cart
- Change a wilaya shipping fee in admin, then checkout — verify new order uses the new fee
- Submit with a part that just went out of stock — should show error

---

### Slice 22: Staff Order Management

**Blocked by:** Slice 21 (COD Checkout)

**Business value:** Lets staff process incoming orders — the back-office side of the storefront.

**Acceptance criteria:**
- [ ] `ShopOrdersController` in default area (auth-required) with Index, Details, Confirm, Ship, Deliver, Return, Cancel actions
- [ ] Order list page shows orders grouped/filtered by status (New, Confirmed, Shipped, Delivered, Returned, Cancelled), sorted newest first
- [ ] Order detail page shows customer info, delivery address (wilaya + commune + address), line items with images, status timeline, action buttons
- [ ] Confirm action decrements inventory quantities and creates `InventoryStockMovement` records (`MovementType = "ShopOrder"`)
- [ ] Insufficient stock on confirm shows error with which parts are short
- [ ] Return action (Shipped → Returned): requires a reason, sets `ReturnedAt` + `ReturnReason`, reverses inventory decrements (`MovementType = "ShopOrderReturn"`)
- [ ] Cancel on a confirmed order reverses inventory decrements (`MovementType = "ShopOrderCancel"`)
- [ ] Cancel action requires a reason if order was confirmed (optional if still New, since stock was never decremented)
- [ ] Cancel from New (not yet confirmed) does not affect inventory
- [ ] Sidebar nav item "الطلبات" added with badge showing new-order count
- [ ] Allowed transitions enforced: New→Confirmed, New→Cancelled, Confirmed→Shipped, Confirmed→Cancelled, Shipped→Delivered, Shipped→Returned. All other transitions blocked.
- [ ] Order detail shows snapshotted wilaya/commune names even if the wilaya/commune was later deactivated or renamed

**Owner commands needed:**
- `dotnet build MimoShop.slnx`

**Manual validation:**
- View the order placed in Slice 21 validation
- Confirm the order — verify inventory quantities decremented
- Ship the order
- Mark as delivered
- Place another order, ship it, then mark as Returned with reason "رفض الاستلام" — verify inventory restored
- Try to return a New order (not shipped) — should be rejected
- Try to cancel a delivered order — should be rejected
- Place a third order and cancel it before confirming — verify inventory unchanged
- Place a fourth order, confirm it, then cancel — verify inventory restored
- Try to cancel a confirmed order without entering a reason — should be rejected
- Try to return a shipped order without entering a reason — should be rejected

---

### Slice 23: Dashboard Order Metrics

**Blocked by:** Slice 22 (Staff Order Management)

**Business value:** Gives the owner visibility into order activity at a glance.

**Acceptance criteria:**
- [ ] Owner dashboard shows new orders count (status = "New")
- [ ] Owner dashboard shows orders shipped today
- [ ] Owner dashboard shows revenue from delivered orders today (DZD)
- [ ] Owner dashboard shows returned orders today (refused or uncollected)
- [ ] Existing dashboard metrics (repair jobs, payments, inventory) unchanged

**Owner commands needed:**
- `dotnet build MimoShop.slnx`

**Manual validation:**
- Log in as owner, view dashboard
- Verify order metrics appear alongside existing repair metrics
- Place an order via the public site, confirm it in the staff portal — verify dashboard updates

---

### Slice 24: Multi-Provider Delivery Setup (Provider config table + Yalidine driver)

**Blocked by:** Slice 22 (Staff Order Management)

**Business value:** Lays the technical foundation for shipping orders through real delivery companies without locking the shop into one provider. The provider-agnostic abstraction (`IDeliveryProvider`) lets staff switch between Yalidine, ZR Express, or Maystro later by adding a code module — no other code changes.

**Acceptance criteria:**
- [ ] `DeliveryProvider` and `Shipment` entities added to `MimoShopDbContext` with entity configurations (unique indexes, FKs, max-length, encryption support)
- [ ] `ShopOrder.ShipmentId` FK column added (nullable)
- [ ] `ShopOrder.CommuneNameFr` snapshot column added
- [ ] One Yalidine row seeded in `DeliveryProvider` with `ApiBaseUrl = "https://api.yalidine.app/v1/"`, `IsActive = false`, credentials empty
- [ ] `IDeliveryProvider` interface defined in `Services/Delivery/` with all methods from §13.3
- [ ] `ShipmentRequest`, `ShipmentResult`, `TrackingInfo` DTOs defined as records
- [ ] `CapabilityFlags` enum defined
- [ ] `DeliveryProviderManager` registered (Scoped) with factory pattern from §13.6
- [ ] `YalidineDriver` class implements `IDeliveryProvider` with full field mapping from §13.5
- [ ] `ApiId` and `ApiToken` encrypted/decrypted via `IDataProtector`
- [ ] Named `HttpClient "yalidine-client"` registered via `AddHttpClient`
- [ ] Existing entities and migrations untouched
- [ ] Migration buildable

**Owner commands needed:**
- `dotnet ef migrations add AddDeliveryProviders --project MimoShop --startup-project MimoShop`

**Manual validation:**
- Verify migration applies cleanly
- Verify `DeliveryProviders` table has the Yalidine seed row
- Verify `Shipments` and `ShopOrder.CommuneNameFr` / `ShopOrder.ShipmentId` columns exist
- (No runtime behavior yet — that's Slice 25 and 26)

---

### Slice 25: Yalidine Shipment Creation on Order Ship

**Blocked by:** Slice 22 (Staff Order Management), Slice 24 (Multi-Provider Delivery Setup)

**Business value:** Replaces the old "mark as shipped" button with a real API call to the delivery company. Staff click "Ship with Yalidine", the system calls Yalidine's API, gets back a tracking code, saves it, and auto-transitions the order to Shipped. No more typing the tracking code manually.

**Acceptance criteria:**
- [ ] On the order detail page, "Ship" button becomes "Ship with Yalidine" (or default provider) when a provider is configured and active
- [ ] Provider dropdown shown when multiple active providers exist
- [ ] Server builds `ShipmentRequest` from the order using snapshotted `CommuneNameFr` (never Arabic-only)
- [ ] Server calls `DeliveryProviderManager.GetDriver("yalidine").CreateShipmentAsync()`
- [ ] **On success:** `Shipment` row created, `ShopOrder.ShipmentId` populated, order auto-transitions Confirmed → Shipped, `ShippedAt` timestamped, success message shows tracking code
- [ ] **On failure:** Order stays Confirmed, error shown with provider's HTTP status + message (e.g. "HTTP 401 — check your API credentials")
- [ ] API credentials never appear in the response or view source
- [ ] Rate limit (HTTP 429) handled gracefully — staff sees the message, no retry storm
- [ ] If API fails, no partial state — no `Shipment` row with empty tracking

**Owner commands needed:**
- `dotnet build MimoShop.slnx`

**Manual validation:**
- Configure Yalidine provider in admin (Slice 26), enter test API credentials
- Confirm an order, click "Ship with Yalidine"
- Verify tracking code appears on order detail
- Verify Yalidine dashboard shows the parcel
- Try with wrong API credentials — verify clean error message, order stays Confirmed
- Try with valid credentials but inactive parcel capacity — verify graceful error

---

### Slice 26: Delivery Provider Admin Page

**Blocked by:** Slice 24 (Multi-Provider Delivery Setup)

**Business value:** Lets staff add, configure, and activate delivery providers from the staff portal. Without a UI, the multi-provider design is unusable — staff can't enter their Yalidine key without going through code.

**Acceptance criteria:**
- [ ] `/DeliveryProviders` (auth-required, default area) lists all configured providers with code, name, API base URL, default indicator, active status
- [ ] Create/Edit form: code, name, API base URL, API ID, API token, extra config JSON, active toggle, default toggle
- [ ] API credentials masked in the UI (show last 4 chars only: `****5678`)
- [ ] "Test Connection" button per provider — calls `driver.TestConnectionAsync()` and shows green check on success, red error with status code on failure
- [ ] Setting one provider as Default automatically unsets any other default (only one default at a time)
- [ ] Delete provider blocked if any `Shipment` references it; clear error message explains why
- [ ] Sidebar nav item "موفرو التوصيل" linked under "الإدارة"
- [ ] All Arabic RTL layout, consistent with existing admin pages (Categories / DeliveryZones pattern)
- [ ] `DeliveryProviderService` handles encryption/decryption and CRUD

**Owner commands needed:**
- `dotnet build MimoShop.slnx`

**Manual validation:**
- Add a new Yalidine provider row — paste test credentials
- Click "Test Connection" with wrong credentials — verify red error
- Click "Test Connection" with correct credentials — verify green check
- Toggle provider inactive — verify it disappears from "Ship with..." dropdown on order detail
- Set a second provider as default — verify first provider loses default
- Try to delete a provider with shipments — verify blocked

---

### Slice 27 (V2 — Pre-planned): Tracking Sync + Additional Providers

**Blocked by:** Slice 25 (Yalidine Shipment Creation), Slice 26 (Provider Admin)

**Business value:** Removes the manual "Mark as Delivered" / "Mark as Returned" steps. The system polls providers for tracking updates and auto-transitions orders. Also adds ZR Express and Maystro as alternative carriers.

**Acceptance criteria:**
- [ ] `TrackingSyncHostedService` registered as `IHostedService`, polls every 30 minutes
- [ ] Polls `Shipment` rows where `Status` in (Created, InTransit)
- [ ] Updates `Shipment.Status` to one of 5 canonical labels, sets `LastSyncedAt`
- [ ] Auto-transitions `ShopOrder` to Delivered when provider reports Delivered
- [ ] Marks `Shipment` as Failed when provider reports refused/uncollected
- [ ] Polling stops on terminal status (Delivered, Failed, Returned)
- [ ] `ZrExpressDriver` and `MaystroDriver` classes implementing `IDeliveryProvider` with their own field mappings
- [ ] Provider admin (Slice 26) supports adding ZR Express and Maystro rows
- [ ] HMAC webhook verification for Yalidine (`X-Yalidine-Signature` header) — bonus

**Owner commands needed:**
- `dotnet build MimoShop.slnx`

**Manual validation:**
- Wait for a real shipment to be delivered — verify dashboard updates `ShipOrder.Delivered` automatically within 30 minutes
- Verify `Shipment.Status` matches provider's dashboard
- Verify polling stops for terminal shipments
- Switch an order's provider from Yalidine to ZR Express — verify both drivers work

---

## 13. Delivery API Integration (Multi-Provider)

### 13.1 Overview

The staff "Ship" action is augmented with a **multi-provider delivery API layer**. Instead of manually recording that an order was shipped, staff pick a delivery provider (e.g. Yalidine) and the system calls that provider's API to create a real shipment, retrieve a tracking code, and optionally download a shipping label.

The architecture follows the **Strategy + Factory pattern** proven by open-source Algerian delivery SDKs (`shipping-dz`, `vargo`, `CourierDZ`):

```
IDeliveryProvider
  + CreateShipment(request) -> ShipmentResult(tracking, labelUrl)
  + GetTracking(tracking)    -> TrackingInfo(status, events)
  + GetLabel(tracking)       -> string
  + TestConnection()         -> bool
  + GetCapabilities()        -> CapabilityFlags
        |
  +-----+------+----------+-------+
  |     |      |          |       |
Yalidine  ZR   Maystro  EcoTrack
Driver  Express Driver  Driver
```

All drivers implement the same interface, so the staff portal can swap providers without code changes. Provider-specific field mappings (credential formats, request schemas) live inside each driver class.

This pattern is modelled directly from **tkawen/shipping-dz** (the cleanest Algerian multi-carrier abstraction on GitHub) with capability-checking added from **KaziSTM/vargo** (90+ providers, 113 capability flags).

### 13.2 New Data Entities

#### DeliveryProvider

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `Id` | `int` | PK, identity | |
| `Code` | `string(30)` | Required, unique | Internal driver code: "yalidine", "zr_express", "maystro", "ecotrack" |
| `Name` | `string(60)` | Required | Display name: "Yalidine Express", "ZR Express" |
| `ApiBaseUrl` | `string(200)` | Required | Provider's API root URL |
| `ApiId` | `string(200)` | Required, encrypted | Provider's API ID / access token |
| `ApiToken` | `string(300)` | Required, encrypted | Provider's API token / secret / tenant ID |
| `ExtraConfigJson` | `string(1000)` | Optional | Provider-specific extras: tenant_id, store_id, from_wilaya_name |
| `IsActive` | `bool` | Required, default true | Inactive providers hidden from "Ship with..." dropdown |
| `IsDefault` | `bool` | Required, default false | Default provider pre-selected |

**Encryption:** `ApiId` and `ApiToken` are encrypted at rest using ASP.NET Core `IDataProtector`. Decrypted at runtime only when the driver makes API calls. Plaintext values never reach the browser. Yalidine explicitly bans accounts that expose credentials to clients.

**Provider-specific credential patterns:**

| Provider Code | ID Field | Token Field | Extra config needed |
|---------------|----------|-------------|---------------------|
| `yalidine` | `api_id` | `api_token` | `from_wilaya_name` (origin wilaya in French) |
| `zr_express` | `api_key` | `tenant_id` | `store_id` |
| `maystro` | `access_token` | `store_id` | - |
| `ecotrack` | `api_token` | - (use ExtraConfigJson) | `base_url` |

**Seed data:** One row for Yalidine (`ApiBaseUrl = "https://api.yalidine.app/v1/"`), inactive until staff configure credentials.

#### Shipment

| Column | Type | Constraints | Notes |
|--------|------|-------------|-------|
| `Id` | `int` | PK, identity | |
| `ShopOrderId` | `int` | FK -> ShopOrder, Cascade delete | |
| `DeliveryProviderId` | `int` | FK -> DeliveryProvider, Restrict delete | |
| `ExternalShipmentId` | `string(100)` | Required | Provider's internal shipment ID |
| `TrackingCode` | `string(100)` | Required, unique | Public tracking code |
| `LabelUrl` | `string(500)` | Optional | URL to downloadable shipping label PDF |
| `Status` | `string(30)` | Required, default "Created" | See normalized statuses below |
| `CreatedAt` | `DateTime` | Required, default now | |
| `LastSyncedAt` | `DateTime?` | | Last tracking poll |

#### Normalized shipment statuses (5 canonical labels)

| Canonical | Meaning | Yalidine raw |
|-----------|---------|--------------|
| `Created` | Registered, not yet picked up | "en preparation" |
| `InTransit` | In transit to destination | "en cours de livraison" |
| `Delivered` | Customer received and paid | "livre" |
| `Failed` | Delivery failed (refused, uncollected) | "refuse", "non livre" |
| `Returned` | Returned to sender | "retourne au vendeur" |

Each driver maps the provider's raw status strings to these 5 canonical values.

### 13.3 IDeliveryProvider Interface (C#)

```csharp
public interface IDeliveryProvider
{
    string Code { get; }
    string Name { get; }

    Task<bool> TestConnectionAsync(DeliveryProvider config);
    Task<ShipmentResult> CreateShipmentAsync(DeliveryProvider config, ShipmentRequest request);
    Task<TrackingInfo> GetTrackingAsync(DeliveryProvider config, string trackingCode);
    Task<string?> GetLabelAsync(DeliveryProvider config, string trackingCode);
    CapabilityFlags GetCapabilities();
}

[Flags]
public enum CapabilityFlags
{
    CreateShipment = 1 << 0,
    GetTracking    = 1 << 1,
    GetLabel       = 1 << 2,
    StopDesk       = 1 << 3,
    HomeDelivery   = 1 << 4,
    Insurance      = 1 << 5,
    Exchange       = 1 << 6,
    DeleteShipment = 1 << 7,
    Webhook        = 1 << 8,
    RateQuery      = 1 << 9,
}
```

### 13.4 Unified DTOs

```csharp
public sealed record ShipmentRequest(
    string OrderCode,
    string CustomerName,
    string CustomerPhone,
    int WilayaId,
    string WilayaNameFr,
    string CommuneNameFr,
    string Address,
    decimal Subtotal,
    string? PhoneAlt = null,
    string? Note = null,
    string ProductList = "Pieces de telephone",
    int WeightGrams = 1000,
    bool FreeShipping = false,
    bool HasExchange = false,
    bool DoInsurance = false,
    decimal? DeclaredValue = null,
    bool IsStopDesk = false,
    string? StopDeskId = null
);

public sealed record ShipmentResult(
    string TrackingCode,
    string ExternalShipmentId,
    string? LabelUrl = null,
    Dictionary<string, object>? ProviderRaw = null
);

public sealed record TrackingInfo(
    string TrackingCode,
    string Status,
    DateTime? LastUpdated,
    Dictionary<string, object>? ProviderRaw = null
);
```

### 13.5 YalidineDriver Field Mapping

| ShipmentRequest field | Yalidine field |
|------------------------|----------------|
| `OrderCode` | `order_id` |
| `CustomerName` | `firstname` + `familyname` (split on last space) |
| `CustomerPhone` | `contact_phone` |
| `WilayaNameFr` | `to_wilaya_name` |
| `CommuneNameFr` | `to_commune_name` (**must be French** - Arabic fails) |
| `Address` | `address` |
| `ExtraConfig.from_wilaya_name` | `from_wilaya_name` |
| `Subtotal` | `price` |
| `ProductList` | `product_list` |
| `WeightGrams` | `weight` (kg), with default `height/width/length` |
| `FreeShipping` | `freeshipping` |
| `IsStopDesk` / `StopDeskId` | `is_stopdesk` / `stopdesk_id` |
| `DoInsurance` + `DeclaredValue` | `do_insurance` + `declared_value` |

Response from `POST /parcels/`:
```json
{ "success": true, "order_id": "ORD-0042", "tracking": "yal-12345A", "import_id": 234 }
```

Mapped to `ShipmentResult(TrackingCode = "yal-12345A", ExternalShipmentId = "234", LabelUrl = "https://...")`.

**Driver constraints discovered from real integrations:**

- **Server-side only** - Yalidine credentials must never reach the browser (account ban risk)
- **Commune names in French** - Arabic names cause `POST /parcels/` to fail
- **Rate limiting** - Yalidine returns quota headers; HTTP 429 followed by potential permanent ban
- **CORS blocked** - Server-side HTTP client required, never browser
- **Stop-desk stopdesk_id** - Mandatory when `is_stopdesk = true`
- **Parcel deletion** - Only possible while status = "en preparation"

### 13.6 DeliveryProviderManager (Factory)

```csharp
public sealed class DeliveryProviderManager
{
    private readonly Dictionary<string, IDeliveryProvider> _drivers;
    public DeliveryProviderManager(IEnumerable<IDeliveryProvider> drivers)
        => _drivers = drivers.ToDictionary(d => d.Code);
    public IDeliveryProvider GetDriver(string code) { ... }
}
```

Registered in DI:
```csharp
builder.Services.AddScoped<DeliveryProviderManager>();
builder.Services.AddSingleton<IDeliveryProvider, YalidineDriver>();
builder.Services.AddHttpClient("yalidine-client");
```

### 13.7 Staff Flow - Ship with Provider

1. Staff opens order detail -> clicks **"Ship"** -> modal shows active providers
2. Staff picks Yalidine (or default) -> clicks **"Ship with Yalidine"**
3. System calls `manager.GetDriver("yalidine").CreateShipmentAsync(config, request)`
4. **On success:** `Shipment` row created, `ShopOrder.ShipmentId` set, order auto-transitions to **Shipped**, tracking visible on order detail
5. **On failure:** Order stays **Confirmed**, error shown: "Yalidine returned error: HTTP 401 - check your API credentials"

### 13.8 Future Tracking Sync (Deferred)

`TrackingSyncHostedService` (background `IHostedService`) polls every 30 minutes:

- Queries `Shipment` rows where `Status` in (Created, InTransit)
- Calls `driver.GetTrackingAsync()` for each
- Updates `Shipment.Status`, sets `LastSyncedAt`
- Auto-transitions `ShopOrder` to Delivered if provider reports Delivered
- Stops polling on terminal status (Delivered, Failed, Returned)

### 13.9 Staff Admin - Delivery Providers Page

- `/DeliveryProviders` (auth-required) - list providers with code, name, status, default flag
- Create/Edit form: code, name, API base URL, API ID, API token, extra config JSON, active/default toggle
- **"Test Connection"** button calls `driver.TestConnectionAsync()` and shows green/red indicator
- API credentials masked in UI (last 4 chars only: `****5678`)
- Delete blocked if Shipment rows reference the provider
- Sidebar nav: **موفرو التوصيل** (Delivery Providers) under "الإدارة"
- Only one provider can have `IsDefault = true` at a time

---

## 14. Future Considerations (Post-V1)

**Note:** §13 (Delivery API Integration) is the preceding section. Several delivery-related deferrals below belong to that area.

| Feature | Notes |
|---------|-------|
| Order lookup via Telegram bot | Let customers check order status by order code, similar to repair status lookup |
| Inventory reservation | Lock stock when item is added to cart or when order is placed (not just on confirm) |
| Keyword search | Full-text search across brand, model, part type, variant names |
| Wilaya/commune bulk import | CSV/JSON upload for mass corrections to delivery zone data |
| Delivery fee per commune | Finer-grained shipping fees at the commune level instead of per-wilaya |
| Order notifications | Telegram message to customer when order is confirmed/shipped |
| Product availability notifications | Alert customer when an out-of-stock part is back in stock |
| Discount codes | Promo codes for special offers |
| Related parts | "Customers also bought" or "Compatible with" suggestions |
| WhatsApp order confirmation | Send order details via WhatsApp API |
| Delivery API — additional providers | ZR Express (Procolis), Maystro, EcoTrack drivers added alongside Yalidine |
| Delivery API — tracking webhooks | Real-time tracking updates from providers instead of polling |
| Delivery API — label printing | Download and print shipping labels directly from the app |
| Delivery API — rate comparison | Compare delivery fees across providers and auto-select cheapest |

---

## 15. Key Design Decisions

| Decision | Rationale |
|----------|-----------|
| **Merge into existing project (Areas)** | Single deployment, shared DB, shared services. Separating would add hosting/config complexity for no benefit at this scale. |
| **Separate `ShopOrder` entity (not merged into `RepairTicket`)** | Orders and repairs are fundamentally different workflows with different lifecycles, data, and actors. Mixing them would pollute both domains. |
| **Separate customer entity (no reuse of repair `Customer`)** | Repair customers are identified by phone for repair tracking. Order customers are anonymous one-time contacts with delivery addresses. Coupling them adds complexity without benefit in V1. |
| **Session-based cart (no database table)** | Simpler, no cleanup needed, no entity overhead. Cart loss on session timeout is acceptable for a parts shop. |
| **Snapshotted prices, names, and delivery zones on order lines** | Protects historical orders from price changes, catalog edits, and wilaya/commune renames. Same pattern used in `RepairPartUsage`. Wilaya/commune names snapshotted on `ShopOrder` so order details remain readable even after admin edits. |
| **COD only (no online payment)** | Trusted in the Algerian market. Removes payment gateway complexity, PCI compliance, and refund flows. |
| **Wilaya + Commune structured selector** | Prevents address typos, enables per-zone delivery fees, and matches Algerian e-commerce conventions (Yassir, Jumia, etc. all use this pattern). |
| **Staff-managed wilaya/commune data (not seed-only)** | External data sources for Algerian administrative divisions are often inaccurate. Giving staff a CRUD interface eliminates the need for a developer to re-seed whenever a commune is added, renamed, or removed. |
| **IsActive toggle instead of hard delete** | Deactivating a wilaya/commune hides it from the checkout dropdown but preserves referential integrity for existing orders. Hard delete is only available when no orders reference the record. |
| **Stock decrement on confirm (not on order)** | An order placed is not yet committed — staff confirms only after verifying stock. This avoids phantom stock depletion from fake/accidental orders. |
| **No customer accounts** | Reduces friction — most parts buyers are one-time or infrequent. Phone number is the contact key, not a login. |
| **Returned status (Shipped → Returned)** | COD orders in Algeria frequently fail at delivery — customer refuses the package or doesn't collect from the pickup point. No payment is collected in either case, so no refund logic is needed. The whole order is returned and all items restocked. Partial returns are deferred to V2 to keep the flow simple. |
| **Multi-provider delivery API abstraction** | Following the Strategy + Factory pattern proven by open-source Algerian carriers (vargo, shipping-dz, CourierDZ). The `IDeliveryProvider` interface lets the shop swap carriers — Yalidine today, ZR Express or Maystro tomorrow — without code changes. Adding a new driver means implementing one C# interface, no other changes. |
| **Encrypted provider credentials at rest** | Delivery provider API keys are encrypted via ASP.NET Core `IDataProtector`. Plaintext credentials never leave the server. Yalidine explicitly bans accounts whose credentials reach the browser, so this is both a security and operational requirement, not just a nicety. |
| **Commune.NameFr required for delivery integration** | Yalidine and most Algerian delivery APIs require commune names in French — Arabic names cause the create-shipment call to fail. The existing `Commune` table already had `NameFr` planned in §5.2; the delivery integration *depends* on staff filling it in correctly via the Delivery Zones admin. |

---

*End of document — MimoShop Public Parts Storefront Specification V1.0*
