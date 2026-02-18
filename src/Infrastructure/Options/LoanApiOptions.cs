using System.ComponentModel.DataAnnotations;
using Bounan.Downloader.Domain.Options;
using JetBrains.Annotations;

namespace Bounan.Downloader.Infrastructure.Options;

public record LoanApiOptions : IOptions
{
    public static string SectionName => "LoanApi";

    [Required]
    [StringLength(100, MinimumLength = 10)]
    public required string FunctionArn { get; [UsedImplicitly] init; }
}
