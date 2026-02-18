using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Bounan.Common;
using Bounan.Downloader.Domain.Clients;
using Bounan.Downloader.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bounan.Downloader.Infrastructure.Clients;

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
public sealed partial class AniManClient(
    ILogger<AniManClient> logger,
    IOptions<AniManOptions> aniManOptions,
    IAmazonLambda lambdaClient)
    : IAniManClient, IDisposable
{
    private readonly SemaphoreSlim semaphore = new(1, 1);

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private ILogger<AniManClient> Logger { get; } = logger;

    private IOptions<AniManOptions> AniManOptions { get; } = aniManOptions;

    private IAmazonLambda LambdaClient { get; } = lambdaClient;

    public void Dispose()
    {
        semaphore.Dispose();
    }

    public async Task<DownloaderResponse?> GetNextVideo(CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            var request = new InvokeRequest
            {
                FunctionName = AniManOptions.Value.GetVideoToDownloadLambdaFunctionName,
                InvocationType = InvocationType.RequestResponse,
            };

            var response = await LambdaClient.InvokeAsync(request, cancellationToken);
            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                Log.FailedToGetVideoInfo(Logger, response.HttpStatusCode);
                return null;
            }

            string payload = Encoding.UTF8.GetString(response.Payload.ToArray());
            return JsonSerializer.Deserialize<DownloaderResponse>(payload, JsonSerializerOptions);
        }
        catch (Exception ex)
        {
            Log.FailedToGetVideoInfo(Logger, ex);
            return null;
        }
        finally
        {
            _ = semaphore.Release();
        }
    }

    public async Task SendResult(DownloaderResultRequest result, CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            var request = new InvokeRequest
            {
                FunctionName = AniManOptions.Value.UpdateVideoStatusLambdaFunctionName,
                InvocationType = InvocationType.RequestResponse,
                Payload = JsonSerializer.Serialize(result, JsonSerializerOptions),
            };

            var response = await LambdaClient.InvokeAsync(request, cancellationToken);
            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                Log.FailedToSendResult(Logger, response.HttpStatusCode);
            }
        }
        catch (Exception ex)
        {
            Log.FailedToSendResult(Logger, ex);
        }
        finally
        {
            _ = semaphore.Release();
        }
    }
}
