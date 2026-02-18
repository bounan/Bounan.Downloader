using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Bounan.Downloader.Domain.Clients;
using Bounan.Downloader.Domain.Models;
using Bounan.Downloader.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Bounan.Downloader.Infrastructure.Clients;

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
internal sealed class LoanApiClient(
    IOptions<LoanApiOptions> loanApiOptions,
    IAmazonLambda lambdaClient)
    : ILoanApiClient, IDisposable
{
    private readonly SemaphoreSlim semaphore = new(1, 1);

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private IOptions<LoanApiOptions> LoanApiOptions { get; } = loanApiOptions;

    private IAmazonLambda LambdaClient { get; } = lambdaClient;

    public void Dispose()
    {
        semaphore.Dispose();
    }

    public async Task<GetVideoResponse> GetVideo(
        int myAnimeListId,
        string dub,
        int episode,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        try
        {
            var request = new InvokeRequest
            {
                FunctionName = LoanApiOptions.Value.FunctionArn,
                InvocationType = InvocationType.RequestResponse,
                Payload = JsonSerializer.Serialize(
                    new GetVideoRequest(myAnimeListId, dub, episode),
                    JsonSerializerOptions),
            };

            var response = await LambdaClient.InvokeAsync(request, cancellationToken);
            if (response.HttpStatusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException(
                    $"Failed to get video info from LoanApi. HTTP status code: {response.HttpStatusCode}");
            }

            string payload = Encoding.UTF8.GetString(response.Payload.ToArray());
            return JsonSerializer.Deserialize<GetVideoResponse>(payload, JsonSerializerOptions)
                   ?? throw new InvalidOperationException("Failed to deserialize response from LoanApi.");
        }
        finally
        {
            _ = semaphore.Release();
        }
    }
}
