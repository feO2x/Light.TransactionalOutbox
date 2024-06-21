using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Light.TransactionalOutbox.Core.OutboxProcessing;

public class OutboxProcessorOptions
{
    public const string DefaultConfigSectionPath = "TransactionalOutbox";
    
    [Range(1, int.MaxValue, ErrorMessage = "The batch size must be greater than 0.")]
    public int BatchSize { get; set; } = 30;
}

[OptionsValidator]
public sealed partial class OutboxProcessorOptionsValidator : IValidateOptions<OutboxProcessorOptions>; 