using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bounan.Common;
using Bounan.Downloader.Worker.Configuration;
using Bounan.Downloader.Worker.Helpers;
using Bounan.Downloader.Worker.Interfaces;
using Hls2TlgrUploader.Interfaces;
using JetBrains.Annotations;
using Microsoft.Extensions.Options;

namespace Bounan.Downloader.Worker.Services;

internal partial class VideoCopyingService(
    ILogger<VideoCopyingService> logger,
    IOptions<ProcessingOptions> processingConfig,
    IHttpClientFactory httpClientFactory,
    ILoanApiClient loanApiClient,
    IAniManClient aniManClient,
    IThumbnailService thumbnailService,
    IVideoUploadingService videoUploadingService)
    : IVideoCopyingService
{
    private readonly ProcessingOptions processingOptions = processingConfig.Value;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private ILogger<VideoCopyingService> Logger => logger;

    private IHttpClientFactory HttpClientFactory => httpClientFactory;

    private IAniManClient AniManClient => aniManClient;

    private ILoanApiClient LoanApiClient => loanApiClient;

    private IVideoUploadingService VideoUploadingService => videoUploadingService;

    private IThumbnailService ThumbnailService => thumbnailService;

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task ProcessVideo(IVideoKey videoKey, CancellationToken cancellationToken)
    {
        Log.ReceivedVideoKey(Logger, videoKey);
        using var innerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        innerCts.CancelAfter(processingOptions.TimeoutSeconds * 1000);

        ArgumentNullException.ThrowIfNull(videoKey);
        try
        {
            await Retry.DoAsync(
                async ct => await ProcessVideoInternalAsync(videoKey, ct),
                Logger,
                cancellationToken: innerCts.Token);
        }
        catch (Exception e)
        {
            Log.ErrorProcessingVideo(Logger, e);

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            await SendResult(videoKey, null, cts.Token);
        }
    }

    private async Task ProcessVideoInternalAsync(IVideoKey videoKey, CancellationToken cancellationToken)
    {
        var (playlistUri, origThumbnail) = await GetPlaylistAndThumbnailAsync(videoKey, cancellationToken);
        Log.GotVideoInfo(Logger, playlistUri, origThumbnail);

        var videoParts = await GetVideoPartsAsync(playlistUri, cancellationToken);

        var thumbnailStreamTask = ThumbnailService.GetThumbnailJpegStreamAsync(
            origThumbnail,
            videoKey,
            cancellationToken);

        var videoMetadata = new VideoMetadata(videoKey);

        var message = await VideoUploadingService.CopyToTelegramAsync(
            videoParts,
            thumbnailStreamTask,
            EncodeMetadata(videoMetadata),
            cancellationToken);
        Log.VideoUploaded(Logger, message.MessageId);

        await SendResult(videoKey, message.MessageId, cancellationToken);
    }

    private async Task<(Uri Playlist, Uri Thumbnail)> GetPlaylistAndThumbnailAsync(
        IVideoKey videoKey,
        CancellationToken cancellationToken)
    {
        var (playlists, thumb) =
            await LoanApiClient.GetVideo(videoKey.MyAnimeListId, videoKey.Dub, videoKey.Episode, cancellationToken);
        Log.GotPlaylistsAndThumbnail(Logger, playlists, thumb);

        var sortedPlaylists = playlists
            .OrderBy(pair => pair.Key.Length)
            .ThenBy(pair => pair.Key);
        var bestQualityPlaylist = processingOptions.UseLowestQuality
            ? sortedPlaylists.First().Value
            : sortedPlaylists.Last().Value;
        Log.ProcessingPlaylist(Logger, bestQualityPlaylist);

        return (bestQualityPlaylist, thumb);
    }

    private async Task<IList<Uri>> GetVideoPartsAsync(Uri playlist, CancellationToken cancellationToken)
    {
        using var httpClient = HttpClientFactory.CreateClient();

        var response = await httpClient.GetAsync(playlist, cancellationToken);
        _ = response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync(cancellationToken);
        var videoParts = content
            .Split('\n')
            .Where(line => line.StartsWith("./", StringComparison.Ordinal))
            .Select(relativeFilePath => new Uri(playlist, relativeFilePath))
            .ToArray();

        return videoParts;
    }

    private async Task SendResult(IVideoKey videoKey, int? messageId, CancellationToken cancellationToken)
    {
        var key = new VideoKey(videoKey.MyAnimeListId, videoKey.Dub, videoKey.Episode);
        var dwnResult = new DownloaderResultRequest(key, messageId);
        await AniManClient.SendResult(dwnResult, cancellationToken);
        Log.ResultSent(Logger, dwnResult);
    }

    private static string EncodeMetadata(VideoMetadata metadata)
    {
        string json = JsonSerializer.Serialize(metadata, JsonSerializerOptions);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
    }

    private record VideoMetadata([UsedImplicitly] IVideoKey VideoKey);
}
