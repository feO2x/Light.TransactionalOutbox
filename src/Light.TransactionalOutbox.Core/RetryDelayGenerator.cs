using System;
using System.Threading.Tasks;
using Polly.Retry;

namespace Light.TransactionalOutbox.Core;

public sealed class RetryDelayGenerator
{
    public TimeSpan? OverriddenDelay { get; set; }
    
    public ValueTask<TimeSpan?> GenerateDelay(RetryDelayGeneratorArguments<object> arg)
    {
        return new ValueTask<TimeSpan?>(OverriddenDelay);
    }
}