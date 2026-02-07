using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using Bounan.Downloader.Worker.Interfaces;

namespace Bounan.Downloader.Worker.Clients;

public class ShikimoriClient(IHttpClientFactory httpClientFactory) : IShikimoriClient
{
    private static readonly Uri s_endpoint = new("https://shikimori.one/api/graphql");

    private static readonly JsonSerializerOptions s_jsonSerializerOptions =
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
            "application/json"
        );

        using HttpClient httpClient = httpClientFactory.CreateClient();
        using HttpResponseMessage response = await httpClient.PostAsync(s_endpoint, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        ShikimoriResponse? result = await JsonSerializer.DeserializeAsync<ShikimoriResponse>(
            stream,
            s_jsonSerializerOptions,
            cancellationToken);

        ShikimoriAnime anime = result?.Data.Animes.Single()
                               ?? throw new InvalidOperationException("Anime not found");
        if (anime.Id != myAnimeListIdStr)
            throw new InvalidOperationException($"Received wrong anime with id {anime.Id}");

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