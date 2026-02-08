using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Bounan.Downloader.Worker.Configuration;
using Bounan.Downloader.Worker.Interfaces;
using Bounan.Downloader.Worker.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Bounan.Downloader.Worker.Clients;

[SuppressMessage("Design", "CA1031:Do not catch general exception types")]
internal sealed class LoanApiClient(
    IOptions<LoanApiConfig> loanApiConfig,
    IAmazonLambda lambdaClient)
    : ILoanApiClient, IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    private readonly JsonSerializerSettings _jsonSerializerSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
    };

    private IOptions<LoanApiConfig> LoanApiConfig { get; } = loanApiConfig;

    private IAmazonLambda LambdaClient { get; } = lambdaClient;

    public void Dispose()
    {
        _semaphore.Dispose();
    }

    public async Task<GetVideoResponse> GetVideo(
        int myAnimeListId,
        string dub,
        int episode,
        CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var request = new InvokeRequest
            {
                FunctionName = LoanApiConfig.Value.FunctionArn,
                InvocationType = InvocationType.RequestResponse,
                Payload = JsonConvert.SerializeObject(
                    new GetVideoRequest(myAnimeListId, dub, episode),
                    _jsonSerializerSettings),
            };

            InvokeResponse response = await LambdaClient.InvokeAsync(request, cancellationToken);
            if (response.HttpStatusCode != HttpStatusCode.OK)
                throw new InvalidOperationException(
                    $"Failed to get video info from LoanApi. HTTP status code: {response.HttpStatusCode}");

            string payload = Encoding.UTF8.GetString(response.Payload.ToArray());
            return JsonConvert.DeserializeObject<GetVideoResponse>(payload, _jsonSerializerSettings)
                   ?? throw new InvalidOperationException("Failed to deserialize response from LoanApi.");
        }
        finally
        {
            _semaphore.Release();
        }
    }
}