# iFixit Part Image Fetch Feature

Mirrors the existing "جلب صورة" (fetch image) flow for phone models — but applied to **inventory parts**, using iFixit's public REST API as the image source instead of GSMArena.

The query sent to iFixit is `{brandName} {modelName} {partTypeName}` (e.g. "Samsung Galaxy A54 screen"). iFixit returns ITEM wiki entries with product images, from which the user picks one — identical UX to the phone model flow.

## How the iFixit API Works

```
GET https://www.ifixit.com/api/2.0/search/{query}?doctypes=wiki&limit=8
```

Returns results with `dataType: "wiki"`, `namespace: "ITEM"`, and an `image` object:
```json
{
  "title": "iPhone 15 Screen",
  "url": "https://www.ifixit.com/Item/iPhone_15_Screen",
  "image": {
    "thumbnail": "https://cart-products.cdn.ifixit.com/cart-products/HtHViaVNk2o2nvdQ.thumbnail",
    "standard":  "https://cart-products.cdn.ifixit.com/cart-products/HtHViaVNk2o2nvdQ.size250",
    "medium":    "https://cart-products.cdn.ifixit.com/cart-products/HtHViaVNk2o2nvdQ.medium"
  }
}
```

- **No authentication required** — free public API.
- The `medium` variant is used as the full image to download & save; `thumbnail` is shown in the picker.
- We filter results to `namespace == "ITEM"` only (parts/products, not guide steps or tool articles).
- Allowed download domain: `cart-products.cdn.ifixit.com`.

> **Note — Design choice for `SourceUrl`:** To avoid a second HTTP round-trip (we already have the `medium` image URL from the search response), we store the `medium` URL directly as `SourceUrl` in the `CatalogImageCandidate`. `GetFullImageUrlAsync` simply validates the host and returns that URL as-is — no second fetch needed. This keeps the `ICatalogImageProvider` interface consistent while saving a network request.

> **Important:** The iFixit API is public but undocumented. A polite `User-Agent` header and a reasonable timeout should be set on the named `HttpClient`. No API key is needed.

---

## Proposed Changes

### 1. Service Layer

#### [NEW] `MimoShop/Services/IFixitCatalogImageProvider.cs`

New provider class implementing `ICatalogImageProvider`, following the same pattern as `GsmArenaCatalogImageProvider`.

**`SearchAsync(CatalogImageSearchRequest, CancellationToken)`**
- Builds query string from `request.Query` if provided, otherwise `"{BrandName} {ModelName} {PartTypeName}"`.
- Calls `GET https://www.ifixit.com/api/2.0/search/{encodedQuery}?doctypes=wiki&limit=8`.
- Parses the JSON `results` array; keeps only entries where `namespace == "ITEM"` and `image` is non-null.
- Returns up to 8 `CatalogImageCandidate` records:
  - `Title` = result `title`
  - `SourceName` = `"iFixit"`
  - `SourceUrl` = `image.medium` URL (used later as the download URL)
  - `ImageUrl` = `image.thumbnail` URL (shown in the picker grid)

**`GetFullImageUrlAsync(string sourceUrl, CancellationToken)`**
- Validates that `sourceUrl` is an absolute HTTPS URL on `cart-products.cdn.ifixit.com`.
- Returns the validated `Uri` as-is — no extra HTTP call.

**`DownloadImageAsync(Uri imageUrl, CancellationToken)`**
- Same stream / size-check / `MemoryStream` buffering pattern as `GsmArenaCatalogImageProvider.DownloadImageAsync`.
- Validates host is `cart-products.cdn.ifixit.com`.

---

#### [MODIFY] `MimoShop/Services/CatalogImageFetchService.cs`

Inject `IFixitCatalogImageProvider` alongside `GsmArenaCatalogImageProvider`.

Add two new public methods:

**`SearchPartImagesAsync(int inventoryPartId, string? query, CancellationToken)`**
- Loads `InventoryPart` with `Brand`, `PhoneModel`, and `PartType` via EF (`AsNoTracking().Include(...)`).
- Returns `[]` if the part is not found.
- Builds `CatalogImageSearchRequest("InventoryPart", brand.Name, phoneModel.Name, partType.Name, null, query)`.
- Delegates to `ifixitProvider.SearchAsync(...)`.

**`ApplyPartImageAsync(int inventoryPartId, string sourceUrl, CancellationToken)`**
- Loads `InventoryPart` with navigations (tracked, for update).
- Returns `null` if not found.
- Calls `ifixitProvider.GetFullImageUrlAsync(sourceUrl, ...)` — throws `CatalogImageException` if invalid.
- Calls `ifixitProvider.DownloadImageAsync(fullUrl, ...)`.
- Saves via `catalogImageService.SaveAsync(stream, fileName, "part", "{brand}-{model}-{partType}")`.
- Deletes old stored images via `catalogImageService.DeleteStoredImage(...)`.
- Updates `part.ImageUrl` and `part.ThumbnailUrl`, calls `dbContext.SaveChangesAsync(...)`.
- Returns `new AppliedCatalogImageResult(image.ImageUrl, image.ThumbnailUrl)`.

---

### 2. Controller

#### [MODIFY] `MimoShop/Controllers/InventoryController.cs`

Inject `CatalogImageFetchService` (constructor parameter, stored as a field).

Add two new action methods:

**`GET /Inventory/SearchPartImages/{id}?query=...`**
```csharp
[HttpGet]
public async Task<IActionResult> SearchPartImages(int id, string? query, CancellationToken cancellationToken)
{
    var candidates = await catalogImageFetchService.SearchPartImagesAsync(id, query, cancellationToken);
    return Json(candidates);
}
```

**`POST /Inventory/ApplyPartImage`**
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> ApplyPartImage(int id, string sourceUrl, CancellationToken cancellationToken)
```
- Returns `400` with `{ message }` if `sourceUrl` is empty.
- Returns `404` with `{ message }` if part not found.
- Returns `{ message, ImageUrl, ThumbnailUrl }` on success.
- Catches `CatalogImageException` → `400`, `HttpRequestException` → `502`, `TaskCanceledException` → `504` — same pattern as `CategoriesController.ApplyPhoneModelImage`.

---

### 3. View

#### [MODIFY] `MimoShop/Views/Inventory/Index.cshtml`

**HTML — per card button:**
Add a "جلب صورة" button in `inv-card__foot`, before the existing "تعديل" link:
```html
<button type="button" class="btn btn-ghost btn--sm"
        data-part-image-fetch-btn
        data-id="@part.Id"
        data-brand-name="@part.Brand"
        data-model-name="@part.Model"
        data-part-type-name="@part.PartType">جلب صورة</button>
```

**HTML — modal:**
Add the `image-fetch-modal` structure (same CSS classes already used in `Categories/Index.cshtml`), with the eyebrow label changed to `"iFixit"` and the title to `"جلب صورة قطعة"`.

**JavaScript:**
Self-contained IIFE in `@section Scripts`, implementing:
- `openPartImageFetchModal(button)` — reads `data-*` attributes, pre-fills query as `"{brand} {model} {partType}"`, clears results, opens modal, auto-triggers search.
- `searchPartImages()` — `GET /Inventory/SearchPartImages/{id}?query=...`, renders candidates.
- `renderImageCandidates(candidates)` — same card-building logic as the models modal.
- `applyPartImage(sourceUrl, selectedCard)` — `POST /Inventory/ApplyPartImage` with CSRF token + `id` + `sourceUrl`; on success updates the `inv-card__media` thumbnail in-DOM and closes modal after 700 ms.

---

### 4. DI Registration

#### [MODIFY] `MimoShop/Program.cs`

Register `IFixitCatalogImageProvider` as a scoped service:
```csharp
builder.Services.AddScoped<IFixitCatalogImageProvider>();
```

Add a named `HttpClient`:
```csharp
builder.Services.AddHttpClient(IFixitCatalogImageProvider.HttpClientName, client =>
{
    client.BaseAddress = new Uri("https://www.ifixit.com/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("MimoShop/1.0 (repair shop management; contact@example.com)");
    client.Timeout = TimeSpan.FromSeconds(15);
});
```

---

## Verification Plan

### Owner Commands
```bash
dotnet build MimoShop.slnx
dotnet run --project MimoShop
```

### Manual Checks
1. Navigate to `/Inventory` — each part card shows a **"جلب صورة"** button in the footer.
2. Click the button → modal opens, title shows "جلب صورة قطعة", eyebrow shows "iFixit", query pre-filled as `{brand} {model} {partType}`.
3. Results load — iFixit part images appear in the picker grid with titles.
4. Select an image → thumbnail updates live on the card, modal closes after ~700 ms.
5. Refresh `/Inventory` → the part card still displays the saved thumbnail.
6. **Edge case:** Search a nonsense query → modal shows "لا توجد نتائج لهذا البحث."
7. **Error case:** Simulate network failure (disable Wi-Fi) → modal shows "تعذر البحث الآن. جرّب مرة أخرى."
8. Verify the phone model fetch flow in `/Categories` still works normally (no regression).
