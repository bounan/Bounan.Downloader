using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Bounan.Downloader.Domain.Clients;

namespace Bounan.Downloader.Infrastructure.Clients;

public class ShikimoriClient(IHttpClientFactory httpClientFactory) : IShikimoriClient
{
    private static readonly Uri Endpoint = new("https://shikimori.one/api/graphql");

    private static readonly JsonSerializerOptions JsonSerializerOptions =
        new() { PropertyNameCaseInsensitive = true };

    public async Task<string> GetTitleAsync(int myAnimeListId, CancellationToken cancellationToken)
    {
        const string query = """
                             query ($id: String!) {
                               animes(ids: $id) {
                                 id
                                 name
                                 russian
                               }
                             }
                             """;

        string myAnimeListIdStr = myAnimeListId.ToString(CultureInfo.InvariantCulture);

        var payload = new { query, variables = new { id = myAnimeListIdStr } };

        using StringContent content = new(
            JsonSerializer.Serialize(payload),
            Encoding.UTF8,
            "application/json");

        using var httpClient = httpClientFactory.CreateClient();
        using var response = await httpClient.PostAsync(Endpoint, content, cancellationToken);
        _ = response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var result = await JsonSerializer.DeserializeAsync<ShikimoriResponse>(
            stream,
            JsonSerializerOptions,
            cancellationToken);

        var anime = result?.Data.Animes.Single()
                               ?? throw new InvalidOperationException("Anime not found");
        if (anime.Id != myAnimeListIdStr)
        {
            throw new InvalidOperationException($"Received wrong anime with id {anime.Id}");
        }

        return string.IsNullOrEmpty(anime.Russian)
            ? anime.Name
            : anime.Russian;
    }

    private record ShikimoriResponse(ShikimoriData Data);

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    private record ShikimoriData(ShikimoriAnime[] Animes);

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
    private record ShikimoriAnime(string Id, string Name, string Russian);
}
