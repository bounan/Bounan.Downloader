using Amazon.Lambda;
using Amazon.SQS;
using Bounan.Downloader.Domain.Clients;
using Bounan.Downloader.Infrastructure.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bounan.Downloader.Infrastructure.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddHttpClient();

        var awsOptions = configuration.GetAWSOptions();
        _ = services
            .AddSingleton<IAmazonLambda>(_ => awsOptions.CreateServiceClient<IAmazonLambda>())
            .AddSingleton<IAmazonSQS>(_ => awsOptions.CreateServiceClient<IAmazonSQS>());

        _ = services
            .AddSingleton<IAniManClient, AniManClient>()
            .AddSingleton<ILoanApiClient, LoanApiClient>()
            .AddSingleton<IShikimoriClient, ShikimoriClient>()
            .AddSingleton<IVideoUploader, VideoUploader>();

        return services;
    }
}
