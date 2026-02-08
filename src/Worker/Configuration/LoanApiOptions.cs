using JetBrains.Annotations;

namespace Bounan.Downloader.Worker.Configuration;

public record LoanApiOptions
{
    public const string SectionName = "LoanApi";

    public required string FunctionArn { get; [UsedImplicitly] init; }
}
