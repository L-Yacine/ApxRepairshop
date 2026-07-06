using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MimoShop.Services;

public sealed class IFixitCatalogImageProvider : ICatalogImageProvider
{
    public const string HttpClientName = "ifixit";

    private readonly HttpClient httpClient;

    public IFixitCatalogImageProvider(IHttpClientFactory httpClientFactory)
    {
        httpClient = httpClientFactory.CreateClient(HttpClientName);
    }

    public async Task<IReadOnlyList<CatalogImageCandidate>> SearchAsync(
        CatalogImageSearchRequest request,
        CancellationToken cancellationToken)
    {
        string query = string.IsNullOrWhiteSpace(request.Query)
            ? $"{request.ModelName} {request.PartTypeName}".Trim()
            : request.Query.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        // iFixit search endpoint
        // e.g. https://www.ifixit.com/api/2.0/search/query?doctypes=wiki&limit=8
        string escapedQuery = Uri.EscapeDataString(query);
        string url = $"api/2.0/search/{escapedQuery}?doctypes=wiki&limit=8";

        using HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"iFixit API returned status code {response.StatusCode} ({response.ReasonPhrase})");
        }

        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException ex)
        {
            throw new JsonException("Failed to parse iFixit response JSON.", ex);
        }

        using (document)
        {
            JsonElement root = document.RootElement;

            if (!root.TryGetProperty("results", out JsonElement resultsElement) || resultsElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var candidates = new List<CatalogImageCandidate>();
            foreach (JsonElement result in resultsElement.EnumerateArray())
            {
                // We filter results to ITEM, WIKI, or CATEGORY namespaces
                if (!result.TryGetProperty("namespace", out JsonElement nsOpt))
                {
                    continue;
                }
                string? ns = nsOpt.GetString();
                if (ns != "ITEM" && ns != "WIKI" && ns != "CATEGORY")
                {
                    continue;
                }

                if (!result.TryGetProperty("image", out JsonElement imageOpt) || imageOpt.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                string title = result.TryGetProperty("display_title", out JsonElement dtOpt) ? dtOpt.GetString() ?? "" : "";
                if (string.IsNullOrWhiteSpace(title))
                {
                    title = result.TryGetProperty("title", out JsonElement tOpt) ? tOpt.GetString() ?? "" : "";
                }

                string? thumbnail = imageOpt.TryGetProperty("thumbnail", out JsonElement thumbOpt) ? thumbOpt.GetString() : null;
                string? medium = imageOpt.TryGetProperty("medium", out JsonElement medOpt) ? medOpt.GetString() : null;

                if (string.IsNullOrWhiteSpace(thumbnail) || string.IsNullOrWhiteSpace(medium))
                {
                    continue;
                }

                candidates.Add(new CatalogImageCandidate(
                    title,
                    "iFixit",
                    medium,
                    thumbnail,
                    null,
                    null
                ));

                if (candidates.Count >= 8)
                {
                    break;
                }
            }

            return candidates;
        }
    }

    public Task<Uri?> GetFullImageUrlAsync(string sourceUrl, CancellationToken cancellationToken)
    {
        if (!IsAllowedImageUrl(sourceUrl, out Uri? uri))
        {
            return Task.FromResult<Uri?>(null);
        }

        return Task.FromResult<Uri?>(uri);
    }

    public async Task<Stream> DownloadImageAsync(Uri imageUrl, CancellationToken cancellationToken)
    {
        if (!IsAllowedImageUrl(imageUrl.AbsoluteUri, out _))
        {
            throw new CatalogImageException("مصدر الصورة غير مسموح.");
        }

        using HttpResponseMessage response = await httpClient.GetAsync(
            imageUrl,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        string? mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.IsNullOrWhiteSpace(mediaType)
            && !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new CatalogImageException("الرابط لا يشير إلى صورة صالحة.");
        }

        long? contentLength = response.Content.Headers.ContentLength;
        if (contentLength.HasValue && contentLength.Value > CatalogImageService.MaxInputBytes)
        {
            throw new CatalogImageException("حجم الصورة كبير جداً. الحد الأقصى هو 8 ميغابايت.");
        }

        await using Stream remoteStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var memoryStream = new MemoryStream();
        byte[] buffer = new byte[81920];
        long totalBytes = 0;

        while (true)
        {
            int bytesRead = await remoteStream.ReadAsync(buffer, cancellationToken);
            if (bytesRead == 0)
            {
                break;
            }

            totalBytes += bytesRead;
            if (totalBytes > CatalogImageService.MaxInputBytes)
            {
                memoryStream.Dispose();
                throw new CatalogImageException("حجم الصورة كبير جداً. الحد الأقصى هو 8 ميغابايت.");
            }

            await memoryStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }

        memoryStream.Position = 0;
        return memoryStream;
    }

    private static bool IsAllowedImageUrl(string value, out Uri? uri)
    {
        uri = null;
        if (!Uri.TryCreate(value, UriKind.Absolute, out Uri? candidate))
        {
            return false;
        }

        if (candidate.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        // iFixit images are served from cdn hosts like cart-products.cdn.ifixit.com or similar
        // Let's verify it belongs to ifixit.com or ifixit-assets or similar to be safe, or just check host ends with .ifixit.com
        if (!candidate.Host.EndsWith(".ifixit.com", StringComparison.OrdinalIgnoreCase)
            && !candidate.Host.Equals("ifixit.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        uri = candidate;
        return true;
    }
}
