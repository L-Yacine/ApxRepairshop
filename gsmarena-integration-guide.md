# GSMArena Image Integration Guide

## Overview

This document outlines the implementation of GSMArena phone image fetching for the repair shop management app. The feature supports two modes:

- **Bulk import** — scrape entire brand catalogs to seed existing phone inventory
- **Search picker** — search and pick an image for individual phones on demand

---

## Architecture

```
User triggers import / search
        ↓
Backend scrapes gsmarena.com (HttpClient + HtmlAgilityPack)
        ↓
Parse results → extract image URLs
        ↓
Download & save images to /uploads/products/
        ↓
Auto-match to existing Product records (fuzzy name match)
        ↓
Return report: matched / unmatched / failed
```

---

## NuGet Dependencies

```
HtmlAgilityPack
```

---

## 1. HttpClient Registration (`Program.cs`)

Register a named `HttpClient` with a realistic User-Agent to avoid blocks:

```csharp
builder.Services.AddHttpClient("gsmarena", c =>
{
    c.DefaultRequestHeaders.UserAgent.ParseAdd(
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/124.0 Safari/537.36");
    c.Timeout = TimeSpan.FromSeconds(10);
});

builder.Services.AddScoped<GsmArenaService>();
builder.Services.AddScoped<GsmArenaBulkImporter>();
```

---

## 2. Core Scraping Service (`GsmArenaService.cs`)

Handles search, device page scraping, and image download.

```csharp
public class GsmArenaService
{
    private readonly HttpClient _http;

    public GsmArenaService(IHttpClientFactory factory)
    {
        _http = factory.CreateClient("gsmarena");
    }

    public async Task<string> FetchHtml(string url)
        => await _http.GetStringAsync(url);

    public async Task<List<GsmArenaSearchResult>> SearchPhones(string query)
    {
        var url = $"https://www.gsmarena.com/search.php3?sQuickSearch={Uri.EscapeDataString(query)}";
        var html = await _http.GetStringAsync(url);
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var results = new List<GsmArenaSearchResult>();
        var items = doc.DocumentNode.SelectNodes("//div[@class='makers']//li");
        if (items == null) return results;

        foreach (var item in items.Take(12))
        {
            var link = item.SelectSingleNode(".//a");
            var img  = item.SelectSingleNode(".//img");
            var name = item.SelectSingleNode(".//span");
            if (link == null) continue;

            results.Add(new GsmArenaSearchResult
            {
                Name      = name?.InnerText.Trim() ?? "",
                Thumbnail = img?.GetAttributeValue("src", "") ?? "",
                DeviceUrl = "https://www.gsmarena.com/" + link.GetAttributeValue("href", ""),
            });
        }

        return results;
    }

    public async Task<string?> GetFullImageUrl(string deviceUrl)
    {
        var html = await _http.GetStringAsync(deviceUrl);
        var doc  = new HtmlDocument();
        doc.LoadHtml(html);

        var imgNode = doc.DocumentNode
            .SelectSingleNode("//div[contains(@class,'specs-photo-main')]//img");

        return imgNode?.GetAttributeValue("src", null);
    }

    public async Task<byte[]> DownloadImage(string imageUrl)
        => await _http.GetByteArrayAsync(imageUrl);
}

public class GsmArenaSearchResult
{
    public string Name      { get; set; } = "";
    public string Thumbnail { get; set; } = "";
    public string DeviceUrl { get; set; } = "";
}
```

---

## 3. Bulk Import Service (`GsmArenaBulkImporter.cs`)

Scrapes full brand catalogs, downloads images, and fuzzy-matches to existing `Product` records.

### Brand Slugs

Add or remove brands as needed. These map to GSMArena's brand listing URLs.

```csharp
private static readonly Dictionary<string, string> BrandSlugs = new()
{
    ["samsung"]  = "samsung-phones-9.php",
    ["apple"]    = "apple-phones-48.php",
    ["xiaomi"]   = "xiaomi-phones-80.php",
    ["huawei"]   = "huawei-phones-58.php",
    ["oppo"]     = "oppo-phones-82.php",
    ["vivo"]     = "vivo-phones-98.php",
    ["oneplus"]  = "oneplus-phones-145.php",
    ["nokia"]    = "nokia-phones-1.php",
    ["motorola"] = "motorola-phones-4.php",
    ["sony"]     = "sony-phones-7.php",
    ["lg"]       = "lg-phones-20.php",
    ["realme"]   = "realme-phones-118.php",
};
```

### Full Service

```csharp
public class GsmArenaBulkImporter
{
    private readonly GsmArenaService _gsm;
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<GsmArenaBulkImporter> _logger;

    public GsmArenaBulkImporter(GsmArenaService gsm, AppDbContext db,
        IWebHostEnvironment env, ILogger<GsmArenaBulkImporter> logger)
    {
        _gsm = gsm; _db = db; _env = env; _logger = logger;
    }

    public async Task<BulkImportReport> ImportBrandAsync(
        string brand,
        IProgress<BulkImportProgress>? progress = null,
        CancellationToken ct = default)
    {
        var report = new BulkImportReport { Brand = brand };

        if (!BrandSlugs.TryGetValue(brand.ToLower(), out var slug))
        {
            report.Errors.Add($"Unknown brand: {brand}");
            return report;
        }

        var listings = await ScrapeBrandListingsAsync(slug, ct);
        report.TotalFound = listings.Count;
        _logger.LogInformation("Found {Count} phones for {Brand}", listings.Count, brand);

        for (int i = 0; i < listings.Count; i++)
        {
            if (ct.IsCancellationRequested) break;

            var listing = listings[i];
            progress?.Report(new BulkImportProgress
            {
                Brand       = brand,
                Current     = i + 1,
                Total       = listings.Count,
                CurrentName = listing.Name
            });

            try
            {
                await ProcessPhoneAsync(listing, report, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed processing {Name}", listing.Name);
                report.Errors.Add($"{listing.Name}: {ex.Message}");
            }

            // Polite delay between requests
            await Task.Delay(Random.Shared.Next(400, 900), ct);
        }

        return report;
    }

    private async Task ProcessPhoneAsync(
        GsmArenaListing listing,
        BulkImportReport report,
        CancellationToken ct)
    {
        var product = FindMatchingProduct(listing.Name);

        // Skip if already has an image
        if (product != null && !string.IsNullOrEmpty(product.ImagePath))
        {
            report.Skipped++;
            return;
        }

        var imageUrl = await _gsm.GetFullImageUrl(listing.DeviceUrl);
        if (imageUrl == null)
        {
            report.Errors.Add($"{listing.Name}: no image found");
            return;
        }

        var fileName = SanitizeFileName($"gsm_{listing.GsmArenaId}.jpg");
        var savePath = Path.Combine(_env.WebRootPath, "uploads", "products", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(savePath)!);

        // Don't re-download if already cached on disk
        if (!File.Exists(savePath))
        {
            var bytes = await _gsm.DownloadImage(imageUrl);
            await File.WriteAllBytesAsync(savePath, bytes, ct);
        }

        if (product != null)
        {
            product.ImagePath = $"/uploads/products/{fileName}";
            await _db.SaveChangesAsync(ct);
            report.Matched++;
        }
        else
        {
            report.Unmatched.Add(new UnmatchedPhone
            {
                GsmArenaName = listing.Name,
                ImagePath    = $"/uploads/products/{fileName}",
                DeviceUrl    = listing.DeviceUrl
            });
        }
    }

    private async Task<List<GsmArenaListing>> ScrapeBrandListingsAsync(
        string slug, CancellationToken ct)
    {
        var all  = new List<GsmArenaListing>();
        var page = 1;

        while (true)
        {
            var url = page == 1
                ? $"https://www.gsmarena.com/{slug}"
                : $"https://www.gsmarena.com/{slug.Replace(".php", $"-p{page}.php")}";

            var html = await _gsm.FetchHtml(url);
            var doc  = new HtmlDocument();
            doc.LoadHtml(html);

            var items = doc.DocumentNode.SelectNodes("//div[@class='makers']//li");
            if (items == null || items.Count == 0) break;

            foreach (var item in items)
            {
                var link = item.SelectSingleNode(".//a");
                var img  = item.SelectSingleNode(".//img");
                var name = item.SelectSingleNode(".//span");
                if (link == null) continue;

                var href  = link.GetAttributeValue("href", "");
                var gsmId = Regex.Match(href, @"-(\d+)\.php$").Groups[1].Value;

                all.Add(new GsmArenaListing
                {
                    Name       = name?.InnerText.Trim() ?? "",
                    Thumbnail  = img?.GetAttributeValue("src", "") ?? "",
                    DeviceUrl  = $"https://www.gsmarena.com/{href}",
                    GsmArenaId = gsmId
                });
            }

            var nextPage = doc.DocumentNode
                .SelectSingleNode("//a[@class='prevnextbutton' and contains(text(),'Next')]");
            if (nextPage == null) break;

            page++;
            await Task.Delay(Random.Shared.Next(600, 1200), ct);
        }

        return all;
    }

    private Product? FindMatchingProduct(string gsmArenaName)
    {
        var normalized = NormalizeName(gsmArenaName);
        return _db.Products
            .AsEnumerable()
            .FirstOrDefault(p =>
                NormalizeName(p.Name) == normalized ||
                NormalizeName(p.Name).Contains(normalized) ||
                normalized.Contains(NormalizeName(p.Name)));
    }

    private static string NormalizeName(string name) =>
        name.ToLowerInvariant()
            .Replace("samsung", "").Replace("apple", "")
            .Replace("  ", " ").Trim();

    private static string SanitizeFileName(string name) =>
        string.Concat(name.Select(c =>
            Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
}
```

### Supporting Models

```csharp
public class GsmArenaListing
{
    public string Name       { get; set; } = "";
    public string Thumbnail  { get; set; } = "";
    public string DeviceUrl  { get; set; } = "";
    public string GsmArenaId { get; set; } = "";
}

public class BulkImportReport
{
    public string Brand    { get; set; } = "";
    public int TotalFound  { get; set; }
    public int Matched     { get; set; }
    public int Skipped     { get; set; }
    public List<string> Errors            { get; set; } = new();
    public List<UnmatchedPhone> Unmatched { get; set; } = new();
}

public class UnmatchedPhone
{
    public string GsmArenaName { get; set; } = "";
    public string ImagePath    { get; set; } = "";
    public string DeviceUrl    { get; set; } = "";
}

public record BulkImportProgress(
    string Brand, int Current, int Total, string CurrentName);
```

---

## 4. API Controller (`GsmArenaController.cs`)

Uses **Server-Sent Events (SSE)** to stream live progress to the frontend — no SignalR needed.

```csharp
[ApiController]
[Route("api/gsmarena")]
public class GsmArenaController : ControllerBase
{
    private readonly GsmArenaService _gsm;
    private readonly GsmArenaBulkImporter _importer;
    private readonly IWebHostEnvironment _env;

    public GsmArenaController(GsmArenaService gsm,
        GsmArenaBulkImporter importer, IWebHostEnvironment env)
    {
        _gsm = gsm; _importer = importer; _env = env;
    }

    // --- Single phone search (for the picker UI) ---
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q)) return BadRequest();
        var results = await _gsm.SearchPhones(q);
        return Ok(results);
    }

    // --- Apply a picked image to a product ---
    [HttpPost("use-image")]
    public async Task<IActionResult> UseImage([FromBody] UseImageRequest req)
    {
        var imageUrl = await _gsm.GetFullImageUrl(req.DeviceUrl);
        if (imageUrl == null) return NotFound("Could not find image");

        var bytes    = await _gsm.DownloadImage(imageUrl);
        var fileName = $"gsm_{Guid.NewGuid()}.jpg";
        var savePath = Path.Combine(_env.WebRootPath, "uploads", "products", fileName);
        await File.WriteAllBytesAsync(savePath, bytes);

        // TODO: call your product service to update ImagePath on the product
        // await _productService.SetImage(req.ProductId, fileName);

        return Ok(new { fileName, url = $"/uploads/products/{fileName}" });
    }

    // --- Bulk import one brand with SSE progress stream ---
    [HttpGet("bulk-import/{brand}")]
    public async Task BulkImport(string brand, CancellationToken ct)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        var progress = new Progress<BulkImportProgress>(async p =>
        {
            var json = JsonSerializer.Serialize(p);
            await Response.WriteAsync($"data: {json}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        });

        var report = await _importer.ImportBrandAsync(brand, progress, ct);

        var finalJson = JsonSerializer.Serialize(new { done = true, report });
        await Response.WriteAsync($"data: {finalJson}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }

    // --- Bulk import multiple brands with SSE progress stream ---
    [HttpPost("bulk-import-brands")]
    public async Task BulkImportBrands(
        [FromBody] List<string> brands, CancellationToken ct)
    {
        Response.Headers.Append("Content-Type", "text/event-stream");
        Response.Headers.Append("Cache-Control", "no-cache");
        Response.Headers.Append("X-Accel-Buffering", "no");

        foreach (var brand in brands)
        {
            var progress = new Progress<BulkImportProgress>(async p =>
            {
                var json = JsonSerializer.Serialize(p);
                await Response.WriteAsync($"data: {json}\n\n", ct);
                await Response.Body.FlushAsync(ct);
            });

            var report = await _importer.ImportBrandAsync(brand, progress, ct);
            var reportJson = JsonSerializer.Serialize(new { brandDone = true, report });
            await Response.WriteAsync($"data: {reportJson}\n\n", ct);
            await Response.Body.FlushAsync(ct);
        }

        await Response.WriteAsync("data: {\"allDone\":true}\n\n", ct);
        await Response.Body.FlushAsync(ct);
    }
}

public record UseImageRequest(int ProductId, string DeviceUrl);
```

---

## 5. Admin Bulk Import Page

A self-contained Razor/HTML page with live progress bar and log.

```html
<h2>Bulk Import Phone Images from GSMArena</h2>

<div id="brandPicker">
  <label><input type="checkbox" value="samsung" checked> Samsung</label>
  <label><input type="checkbox" value="apple"   checked> Apple</label>
  <label><input type="checkbox" value="xiaomi">  Xiaomi</label>
  <label><input type="checkbox" value="huawei">  Huawei</label>
  <label><input type="checkbox" value="nokia">   Nokia</label>
  <label><input type="checkbox" value="motorola"> Motorola</label>
  <label><input type="checkbox" value="oneplus">  OnePlus</label>
  <label><input type="checkbox" value="sony">    Sony</label>
  <label><input type="checkbox" value="realme">  Realme</label>
</div>

<button onclick="startBulkImport()" id="startBtn">▶ Start Import</button>
<button onclick="cancelImport()"    id="cancelBtn" style="display:none">✖ Cancel</button>

<div id="progressBar" style="display:none; border:1px solid #ccc; border-radius:4px; overflow:hidden; margin-top:8px">
  <div id="barFill" style="width:0%; background:#4caf50; height:20px; transition:width .3s"></div>
</div>

<div id="currentPhone" style="font-size:13px; color:#555; margin-top:4px"></div>

<div id="log" style="
  height: 300px;
  overflow-y: auto;
  font-family: monospace;
  font-size: 12px;
  background: #1e1e1e;
  color: #d4d4d4;
  padding: 8px;
  margin-top: 8px;
  border-radius: 4px;
"></div>

<script>
async function startBulkImport() {
    const brands = [...document.querySelectorAll('#brandPicker input:checked')]
        .map(cb => cb.value);

    if (!brands.length) return alert('Select at least one brand.');

    document.getElementById('startBtn').disabled = true;
    document.getElementById('cancelBtn').style.display = 'inline';
    document.getElementById('progressBar').style.display = 'block';
    document.getElementById('log').innerHTML = '';

    const res = await fetch('/api/gsmarena/bulk-import-brands', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(brands)
    });

    const reader  = res.body.getReader();
    const decoder = new TextDecoder();

    while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        const text  = decoder.decode(value);
        const lines = text.split('\n').filter(l => l.startsWith('data: '));
        for (const line of lines) {
            try { handleUpdate(JSON.parse(line.slice(6))); }
            catch {}
        }
    }
}

function handleUpdate(data) {
    const log = document.getElementById('log');

    if (data.allDone) {
        log.innerHTML += `<div style="color:#6a9955">✅ All brands complete!</div>`;
        document.getElementById('startBtn').disabled = false;
        document.getElementById('cancelBtn').style.display = 'none';
        return;
    }

    if (data.brandDone) {
        const r = data.report;
        log.innerHTML += `<div style="color:#569cd6">
            ✔ ${r.brand}: ${r.matched} matched, 
            ${r.unmatched.length} unmatched, 
            ${r.skipped} skipped, 
            ${r.errors.length} errors
        </div>`;
        return;
    }

    // Progress update
    const pct = Math.round((data.current / data.total) * 100);
    document.getElementById('barFill').style.width = pct + '%';
    document.getElementById('currentPhone').textContent =
        `[${data.brand}] ${data.current}/${data.total} — ${data.currentName}`;

    log.innerHTML += `<div>📱 ${data.currentName}</div>`;
    log.scrollTop = log.scrollHeight;
}

function cancelImport() {
    window.location.reload();
}
</script>
```

---

## 6. Single Phone Image Picker (Product Edit Page)

Add this button and modal to the existing product edit form.

```html
<!-- Add this button next to the existing image upload -->
<button type="button" onclick="openGsmPicker()">📷 Fetch from GSMArena</button>

<!-- Modal -->
<div id="gsmModal" style="display:none; position:fixed; inset:0; background:rgba(0,0,0,.5); z-index:1000">
  <div style="background:#fff; width:700px; margin:80px auto; border-radius:8px; padding:24px">
    <h3>Search GSMArena</h3>
    <input id="gsmSearch" placeholder="e.g. Samsung Galaxy A55" style="width:70%" />
    <button onclick="searchGsm()">Search</button>
    <button onclick="closeGsmModal()" style="float:right">✖ Close</button>
    <div id="gsmResults" style="display:flex; flex-wrap:wrap; gap:12px; margin-top:16px"></div>
  </div>
</div>

<script>
function openGsmPicker()    { document.getElementById('gsmModal').style.display = 'block'; }
function closeGsmModal()    { document.getElementById('gsmModal').style.display = 'none'; }

async function searchGsm() {
    const q = document.getElementById('gsmSearch').value;
    if (!q) return;

    document.getElementById('gsmResults').innerHTML = 'Searching...';

    const res   = await fetch(`/api/gsmarena/search?q=${encodeURIComponent(q)}`);
    const phones = await res.json();

    document.getElementById('gsmResults').innerHTML = phones.map(p => `
        <div onclick="selectGsmPhone('${p.deviceUrl}')"
             style="cursor:pointer; text-align:center; width:120px; padding:8px;
                    border:1px solid #ddd; border-radius:6px">
            <img src="${p.thumbnail}" style="width:100px; height:auto" />
            <div style="font-size:11px; margin-top:4px">${p.name}</div>
        </div>
    `).join('');
}

async function selectGsmPhone(deviceUrl) {
    const productId = document.getElementById('productId').value; // your hidden field

    const res  = await fetch('/api/gsmarena/use-image', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ productId: parseInt(productId), deviceUrl })
    });
    const data = await res.json();

    document.getElementById('productImagePreview').src = data.url;
    closeGsmModal();
}
</script>
```

---

## 7. Unmatched Images Review Page

After bulk import, some images will be downloaded but not automatically matched to a product (due to name differences). Build a simple `/admin/gsmarena/unmatched` page that:

- Lists all downloaded images that have no product assignment
- Shows a dropdown of existing products next to each image
- Lets the user manually assign and save

The unmatched list comes from `BulkImportReport.Unmatched` — persist it to a temp table or a JSON file on disk so it survives the request.

---

## Recommended Workflow

1. **Run bulk import once** to seed all existing phone inventory
2. **Review the unmatched page** and manually assign any images that didn't auto-match
3. **Use the search picker** going forward when adding new phones
4. **Re-run bulk import** per brand whenever you add a new brand to inventory

---

## Tips & Gotchas

**Caching** — Cache search results in `IMemoryCache` with a 24-hour TTL. GSMArena's catalog changes slowly and this reduces repeat requests significantly.

**Anti-blocking** — The random delays (`400–900ms` per phone, `600–1200ms` per page) are already built into the service. Do not remove them or GSMArena will return 429/503 responses.

**Image already on disk** — The `File.Exists(savePath)` check before downloading means re-running the import is safe and won't re-download images you already have.

**Name normalization** — The fuzzy match strips brand names and normalizes casing. If your product names follow a specific convention (e.g. "A55 5G" vs "Galaxy A55 5G"), adjust the `NormalizeName` method accordingly.

**Pagination** — GSMArena brand pages use the pattern `samsung-phones-9-p2.php`, `samsung-phones-9-p3.php` etc. The `ScrapeBrandListingsAsync` method handles this automatically.

