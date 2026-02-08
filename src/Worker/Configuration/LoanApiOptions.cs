using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Bounan.Downloader.Worker.Configuration;

public record LoanApiOptions : IOptions
{
    public static string SectionName => "LoanApi";

    [Required]
    [StringLength(100, MinimumLength = 10)]
    public required string FunctionArn { get; [UsedImplicitly] init; }
}
