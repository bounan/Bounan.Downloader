using Bounan.Downloader.Application.Options;
using Bounan.Downloader.Domain.Options;
using Bounan.Downloader.Infrastructure.Options;
using Bounan.Downloader.Worker.Abstractions;
using Bounan.Downloader.Worker.Services;
using SqsClient = Bounan.Downloader.Worker.Services.SqsClient;

namespace Bounan.Downloader.Worker.Extensions;

internal static class DependencyInjectionExtensions
{
    public static IServiceCollection AddWorkerServices(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        _ = services
            .AddOptionsExt<AniManOptions>()
            .AddOptionsExt<SqsOptions>()
            .AddOptionsExt<ProcessingOptions>()
            .AddOptionsExt<ThreadingOptions>()
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

        _ = services
            .AddSingleton<JobSignalingService>()
            .AddSingleton<IJobSignalReceiver>(sp => sp.GetRequiredService<JobSignalingService>())
            .AddSingleton<IJobSignalSender>(sp => sp.GetRequiredService<JobSignalingService>());

        _ = services
            .AddHostedService<ConfigurationLoggingService>()
            .AddHostedService<SqsClient>()
            .AddHostedService<WorkerService>();

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
