using JetBrains.Annotations;

namespace Bounan.Downloader.Worker.Configuration;

public record LoanApiConfig
{
    public const string SectionName = "LoanApi";

    public required string FunctionArn { get; [UsedImplicitly] init; }
}
