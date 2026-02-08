namespace Bounan.Downloader.Worker.Configuration;

internal record ThumbnailOptions
{
    public const string SectionName = "Thumbnail";

    public required string BotId { get; init; }
}
