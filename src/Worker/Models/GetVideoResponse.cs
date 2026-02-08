namespace Bounan.Downloader.Worker.Models;

public record GetVideoResponse(Dictionary<string, Uri> Playlists, Uri ThumbnailUrl);