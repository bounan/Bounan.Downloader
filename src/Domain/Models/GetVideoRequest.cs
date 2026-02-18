using JetBrains.Annotations;

namespace Bounan.Downloader.Domain.Models;

public record GetVideoRequest([UsedImplicitly] int MyAnimeListId, string Dub, int Episode);
