using System.ComponentModel.DataAnnotations;

namespace Light.TransactionalOutbox.Core;

public class OutboxProcessorOptions
{
    public const string DefaultConfigSectionPath = "TransactionalOutbox";
    
    [Range(1, int.MaxValue, ErrorMessage = "The batch size must be greater than 0.")]
    public int BatchSize { get; set; } = 30;
}