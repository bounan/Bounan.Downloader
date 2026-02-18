namespace Bounan.Downloader.Domain.Models;

public record GetVideoResponse(Dictionary<string, Uri> Playlists, Uri ThumbnailUrl);
