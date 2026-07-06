# Catalog Images and Future On-Demand Fetching

## Current Implementation

The catalog currently supports local, staff-managed image uploads for brands, phone models, and exact inventory parts.

Database-backed image fields:

- `Brand.ImageUrl`
- `Brand.ThumbnailUrl`
- `PhoneModel.ImageUrl`
- `PhoneModel.ThumbnailUrl`
- `InventoryPart.ImageUrl`
- `InventoryPart.ThumbnailUrl`

Image URL fields store public relative paths only, for example:

```text
/uploads/catalog/images/part-1-2-3-4-abc123.webp
/uploads/catalog/thumbs/part-1-2-3-4-abc123.webp
```

The image files are saved under:

```text
MimoShop/wwwroot/uploads/catalog/images/
MimoShop/wwwroot/uploads/catalog/thumbs/
```

Runtime uploads are ignored by Git through `.gitignore`.

## Image Processing Pipeline

All local uploads go through `MimoShop/Services/CatalogImageService.cs`.

The service:

- accepts JPG, JPEG, PNG, WEBP, HEIC, and HEIF extensions;
- rejects files over 8 MB;
- decodes the file with Magick.NET instead of trusting the extension alone;
- auto-orients using image metadata;
- strips metadata/EXIF before saving;
- resizes the main image to fit within `900x900`;
- creates a thumbnail that fits within `240x240`;
- writes both outputs as WebP;
- returns `CatalogImageResult` with `ImageUrl` and `ThumbnailUrl`;
- deletes old local catalog files when a replacement image is saved.

Magick.NET package:

```bash
dotnet add MimoShop/MimoShop.csproj package Magick.NET-Q16-AnyCPU
```

## Web UI Behavior

Category management:

- brand rows show the brand thumbnail when available;
- model rows show the model thumbnail when available;
- model rows fall back to the parent brand thumbnail;
- brand and model forms accept local image uploads.

Inventory:

- exact part photos are uploaded from the inventory add/edit form;
- inventory cards use this fallback order:
  1. part thumbnail
  2. model thumbnail
  3. brand thumbnail
  4. placeholder

## Telegram Behavior

Telegram catalog browsing remains read-only.

At the variant level, `PartsCatalogQueryService` returns a variant image path using this fallback order:

1. exact inventory part image
2. phone model image
3. brand image

`TelegramBotUpdateHandler` sends a photo message only when:

- a usable image path exists; and
- `Telegram:PublicBaseUrl` is configured.

Example config:

```json
"Telegram": {
  "BotToken": "...",
  "PublicBaseUrl": "https://example.com"
}
```

Telegram cannot fetch `localhost` or private network URLs. If `PublicBaseUrl` is empty or invalid, the bot stays text-only.

## Migration State

The schema change is named:

```bash
dotnet ef migrations add AddCatalogImageUrls --project MimoShop --startup-project MimoShop
```

If `MimoShop/Migrations/20260627173405_AddCatalogImageUrls.cs` is present, do not scaffold this migration again. Use the existing migration.

## Implemented: Staff-Reviewed Phone Model Fetching

Phone model image fetching is now implemented for GSMArena as a staff-triggered picker in `/Categories`.

Implemented behavior:

- model rows have a `جلب صورة` action;
- the modal searches GSMArena using `{BrandName} {ModelName}` by default;
- staff reviews candidate cards before saving;
- the selected GSMArena device page is posted back to the server;
- the server extracts the full image URL, downloads it, and passes the stream through `CatalogImageService`;
- `PhoneModel.ImageUrl` and `PhoneModel.ThumbnailUrl` are updated after successful processing;
- the model row thumbnail updates in place after save.

Implemented safety controls:

- named `HttpClient` with timeout and browser-like User-Agent;
- device URLs must be HTTPS and hosted by `gsmarena.com`;
- downloaded image URLs must be HTTPS and hosted by allow-listed GSMArena image hosts;
- downloads are capped at 8 MB before Magick.NET processing;
- remote originals are never persisted directly.

Package dependency:

```bash
dotnet restore MimoShop.slnx
```

The project file includes:

```xml
<PackageReference Include="HtmlAgilityPack" Version="1.12.4" />
```

No new migration is required for this feature because phone model image URL fields already exist.

## Future Feature: Broader On-Demand Image Fetching

Future work can extend the same approach to brands, inventory parts, and other approved providers. This should continue to reuse the existing `CatalogImageService` processing pipeline so remote images are never stored raw.

### Recommended User Flow

Add an action in category and inventory screens:

- "Fetch missing image" for a brand
- "Fetch missing image" for a phone model
- "Fetch missing image" for an inventory part

The first version should be staff-triggered, not automatic background scraping.

Suggested flow:

1. Staff clicks fetch.
2. Server searches trusted image providers.
3. Server shows 3-6 candidate images with source name and source URL.
4. Staff chooses one candidate.
5. Server downloads the selected image.
6. Server processes it with `CatalogImageService`.
7. Server stores `ImageUrl` and `ThumbnailUrl` on the selected entity.

Do not auto-save the first result without review. Incorrect phone part photos would damage trust in the catalog.

### Suggested Architecture

Add provider abstractions:

```csharp
public interface ICatalogImageProvider
{
    Task<IReadOnlyList<CatalogImageCandidate>> SearchAsync(
        CatalogImageSearchRequest request,
        CancellationToken cancellationToken);
}

public sealed record CatalogImageSearchRequest(
    string EntityType,
    string BrandName,
    string? ModelName,
    string? PartTypeName,
    string? VariantName);

public sealed record CatalogImageCandidate(
    string Title,
    string SourceName,
    string SourceUrl,
    string ImageUrl,
    int? Width,
    int? Height);
```

Add an orchestration service:

```csharp
public sealed class CatalogImageFetchService
{
    // Search providers, dedupe candidates, download selected candidate,
    // then pass the downloaded stream into CatalogImageService.
}
```

`CatalogImageService` may need an overload such as:

```csharp
Task<CatalogImageResult> SaveAsync(
    Stream imageStream,
    string originalFileName,
    string entityType,
    string displayName);
```

This keeps upload and cloud-fetch processing identical.

### Source Reliability Rules

Use an allow-list. Do not scrape arbitrary web search result pages.

Reliable source examples to evaluate later:

- manufacturer product pages;
- official brand media/product APIs if available;
- authorized distributor catalogs;
- shop-owned cloud storage;
- manually curated internal image library.

Avoid sources that are likely unreliable or legally risky:

- random marketplace seller images;
- social media posts;
- unrelated blog images;
- unlicensed image search hotlinks;
- watermarked images;
- images with unclear product compatibility.

Every candidate should carry:

- source name;
- source URL;
- image URL;
- fetch timestamp;
- provider name.

Future database fields worth adding:

- `ImageSourceName`
- `ImageSourceUrl`
- `ImageProvider`
- `ImageFetchedAt`
- `ImageReviewedByUsername`

Do not add those fields until the fetch feature is actually implemented.

### Matching Strategy

Search terms should be built from the existing catalog hierarchy:

- brand image: `{BrandName} logo official`
- model image: `{BrandName} {ModelName} official`
- part image: `{BrandName} {ModelName} {PartTypeName} {VariantName} replacement part`

For exact inventory parts, prefer product/part photos over phone glamour photos. A screen replacement listing should show the screen part, not only the phone model.

### Download Safety

Before downloading a remote image:

- require `https`;
- enforce provider allow-list;
- set timeout;
- cap response size before buffering;
- verify content type when present;
- decode with Magick.NET;
- reject animated or multi-frame images unless explicitly supported;
- strip metadata;
- save as WebP through `CatalogImageService`;
- never persist the remote original directly.

### UI and Bot Expectations

Once a fetched image is accepted, the existing UI and Telegram paths should work without special handling because they already read `ImageUrl` and `ThumbnailUrl`.

The same fallback rules should remain:

- inventory UI: part → model → brand → placeholder
- Telegram variant level: part → model → brand → text-only

### Open Questions for the Future Developer

- Which image providers are approved by the shop owner?
- Are official brand logos allowed, or should logos be avoided unless the shop has usage rights?
- Should fetched images require owner approval, or can workers approve them?
- Should old fetched images be retained for audit, or deleted on replacement?
- Should there be a scheduled "find missing images" dashboard, or only per-row fetch buttons?
