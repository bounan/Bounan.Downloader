using Amazon.Lambda;
using Amazon.SQS;
using Bounan.Downloader.Worker.Clients;
using Bounan.Downloader.Worker.Configuration;
using Bounan.Downloader.Worker.Interfaces;
using Bounan.Downloader.Worker.Services;
using SqsClient = Bounan.Downloader.Worker.Clients.SqsClient;

namespace Bounan.Downloader.Worker;

public static class ServiceProviderExtensions
{
    public static IServiceCollection AddWorkerServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services
            .AddOptionsExt<AniManOptions>()
            .AddOptionsExt<SqsOptions>()
            .AddOptionsExt<ProcessingOptions>()
            .AddOptionsExt<ThumbnailOptions>()
            .AddOptionsExt<LoanApiOptions>();

        _ = services.AddLogging(logging =>
        {
            _ = logging
                .ClearProviders()
                .AddConfiguration(configuration.GetSection("Logging"))
                .AddConsole()
                .AddAWSProvider(configuration.GetAWSLoggingConfigSection());
        });

        _ = services.AddHttpClient();

        var awsOptions = configuration.GetAWSOptions();
        _ = services
            .AddSingleton<IAmazonLambda>(_ => awsOptions.CreateServiceClient<IAmazonLambda>())
            .AddSingleton<IAmazonSQS>(_ => awsOptions.CreateServiceClient<IAmazonSQS>());

        _ = services
            .AddSingleton<IAniManClient, AniManClient>()
            .AddSingleton<ILoanApiClient, LoanApiClient>()
            .AddSingleton<IShikimoriClient, ShikimoriClient>()
            .AddSingleton<ISqsClient, SqsClient>()
            .AddSingleton<IVideoCopyingService, VideoCopyingService>()
            .AddSingleton<IThumbnailService, ThumbnailService>();

        _ = services.AddHostedService<WorkerService>();

        return services;
    }

    private static IServiceCollection AddOptionsExt<T>(this IServiceCollection services)
        where T : class, IOptions
    {
        _ = services
            .AddOptions<T>()
            .BindConfiguration(T.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services;
    }
}
