using JetBrains.Annotations;

namespace Bounan.Downloader.Worker.Models;

public record GetVideoRequest([UsedImplicitly] int MyAnimeListId, string Dub, int Episode);
