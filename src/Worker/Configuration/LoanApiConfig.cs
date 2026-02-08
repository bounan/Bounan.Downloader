namespace Bounan.Downloader.Worker.Configuration;

public record LoanApiConfig
{
    public const string SectionName = "LoanApi";

    public required string FunctionArn { get; init; }
}