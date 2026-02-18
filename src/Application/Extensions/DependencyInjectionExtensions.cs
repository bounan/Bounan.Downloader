using Bounan.Downloader.Application.Abstractions;
using Bounan.Downloader.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Bounan.Downloader.Application.Extensions;

public static class DependencyInjectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        _ = services
            .AddSingleton<IVideoCopyingService, VideoCopyingService>()
            .AddSingleton<IThumbnailService, ThumbnailService>();

        return services;
    }
}
