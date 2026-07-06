using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace MimoShop.Services;

public sealed class GsmArenaCatalogImageProvider : ICatalogImageProvider
{
    public const string HttpClientName = "gsmarena";

    private static readonly Uri BaseUri = new("https://www.gsmarena.com/");
    private static readonly Uri ThumbnailBaseUri = new("https://fdn2.gsmarena.com/vv/bigpic/");
    private static readonly Uri QuickSearchUri = new(BaseUri, "quicksearch-82531.jpg");

    private readonly HttpClient httpClient;

    public GsmArenaCatalogImageProvider(IHttpClientFactory httpClientFactory)
    {
        httpClient = httpClientFactory.CreateClient(HttpClientName);
    }

    public async Task<IReadOnlyList<CatalogImageCandidate>> SearchAsync(
        CatalogImageSearchRequest request,
        CancellationToken cancellationToken)
    {
        string query = string.IsNullOrWhiteSpace(request.Query)
            ? $"{request.BrandName} {request.ModelName}".Trim()
            : request.Query.Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        string json = await httpClient.GetStringAsync(QuickSearchUri, cancellationToken);
        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Array || root.GetArrayLength() < 2)
        {
            return [];
        }

        JsonElement makers = root[0];
        JsonElement phones = root[1];
        string[] terms = NormalizeSearchText(query)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var results = new List<CatalogImageCandidate>();
        foreach (JsonElement phone in phones.EnumerateArray())
        {
            GsmArenaPhoneRecord? record = ReadPhoneRecord(phone, makers);
            if (record is null || !IsMatch(record, terms))
            {
                continue;
            }

            results.Add(new CatalogImageCandidate(
                $"{record.MakerName} {record.DisplayName}",
                "GSMArena",
                new Uri(BaseUri, BuildDevicePath(record)).ToString(),
                new Uri(ThumbnailBaseUri, record.ThumbnailFile).ToString(),
                null,
                null));

            if (results.Count >= 8)
            {
                break;
            }
        }

        return results;
    }

    public async Task<Uri?> GetFullImageUrlAsync(string deviceUrl, CancellationToken cancellationToken)
    {
        if (!IsAllowedDeviceUrl(deviceUrl, out Uri? uri))
        {
            return null;
        }

        string html = await httpClient.GetStringAsync(uri, cancellationToken);
        var document = new HtmlDocument();
        document.LoadHtml(html);

        HtmlNode? image = document.DocumentNode
            .SelectSingleNode("//div[contains(@class,'specs-photo-main')]//img");
        string src = image?.GetAttributeValue("src", string.Empty) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(src))
        {
            return null;
        }

        return new Uri(BaseUri, WebUtility.HtmlDecode(src));
    }

    public async Task<Stream> DownloadImageAsync(Uri imageUrl, CancellationToken cancellationToken)
    {
        if (!IsAllowedImageUrl(imageUrl))
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

    private static bool IsAllowedDeviceUrl(string value, out Uri? uri)
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

        if (!string.Equals(candidate.Host, "www.gsmarena.com", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(candidate.Host, "gsmarena.com", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        uri = candidate;
        return true;
    }

    private static bool IsAllowedImageUrl(Uri uri)
    {
        if (uri.Scheme != Uri.UriSchemeHttps)
        {
            return false;
        }

        return string.Equals(uri.Host, "fdn2.gsmarena.com", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Host, "fdn.gsmarena.com", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Host, "www.gsmarena.com", StringComparison.OrdinalIgnoreCase);
    }

    private static GsmArenaPhoneRecord? ReadPhoneRecord(JsonElement phone, JsonElement makers)
    {
        if (phone.ValueKind != JsonValueKind.Array || phone.GetArrayLength() < 5)
        {
            return null;
        }

        int makerId = phone[0].GetInt32();
        int phoneId = phone[1].GetInt32();
        string phoneName = phone[2].GetString() ?? string.Empty;
        string searchString = phone[3].GetString() ?? string.Empty;
        string thumbnailFile = phone[4].GetString() ?? string.Empty;
        string? overrideName = phone.GetArrayLength() > 5 ? phone[5].GetString() : null;

        if (!makers.TryGetProperty(makerId.ToString(), out JsonElement makerElement))
        {
            return null;
        }

        string makerName = makerElement.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(makerName)
            || string.IsNullOrWhiteSpace(phoneName)
            || string.IsNullOrWhiteSpace(thumbnailFile))
        {
            return null;
        }

        return new GsmArenaPhoneRecord(
            makerName,
            phoneId,
            phoneName,
            string.IsNullOrWhiteSpace(overrideName) ? phoneName : overrideName,
            searchString,
            thumbnailFile);
    }

    private static bool IsMatch(GsmArenaPhoneRecord record, string[] terms)
    {
        if (terms.Length == 0)
        {
            return false;
        }

        string haystack = NormalizeSearchText(
            $"{record.MakerName} {record.PhoneName} {record.DisplayName} {record.SearchString}");

        return terms.All(term => haystack.Contains(term, StringComparison.Ordinal));
    }

    private static string BuildDevicePath(GsmArenaPhoneRecord record)
    {
        string name = $"{record.MakerName} {record.PhoneName}".ToLowerInvariant();
        string slug = Regex.Replace(name, @"\s+|-|/|\.", "_");
        return $"{slug}-{record.PhoneId}.php";
    }

    private static string NormalizeSearchText(string value)
    {
        string decoded = WebUtility.HtmlDecode(value).ToLowerInvariant();
        return Regex.Replace(decoded, @"[^\p{L}\p{N}]+", " ").Trim();
    }

    private sealed record GsmArenaPhoneRecord(
        string MakerName,
        int PhoneId,
        string PhoneName,
        string DisplayName,
        string SearchString,
        string ThumbnailFile);
}
