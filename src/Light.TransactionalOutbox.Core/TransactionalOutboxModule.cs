using System;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;

namespace Light.TransactionalOutbox.Core;

public static class TransactionalOutboxModule
{
    public static IServiceCollection AddOutboxProcessor<TOutboxItem>(
        this IServiceCollection services,
        string configSectionPath = OutboxProcessorOptions.DefaultConfigSectionPath,
        bool registerDefaultResiliencePipeline = true
    )
    {
        if (registerDefaultResiliencePipeline)
        {
            services.AddResiliencePipeline<string>(
                OutboxProcessor<TOutboxItem>.ResiliencePipelineKey,
                (pipelineBuilder, context) =>
                {
                    var retryDelayGenerator = context.ServiceProvider.GetRequiredService<RetryDelayGenerator>();
                    pipelineBuilder
                       .AddRetry(
                            new RetryStrategyOptions
                            {
                                Delay = TimeSpan.FromSeconds(1),
                                MaxDelay = TimeSpan.FromMinutes(1),
                                MaxRetryAttempts = int.MaxValue,
                                BackoffType = DelayBackoffType.Linear,
                                UseJitter = true,
                                DelayGenerator = retryDelayGenerator.GenerateDelay
                            }
                        );
                }
            );
        }

        services
           .AddOptions<OutboxProcessorOptions>()
           .BindConfiguration(configSectionPath)
           .ValidateDataAnnotations()
           .ValidateOnStart();

        return services
           .AddSingleton<OutboxProcessor<TOutboxItem>>()
           .AddSingleton<IOutboxProcessor>(sp => sp.GetRequiredService<OutboxProcessor<TOutboxItem>>())
           .AddSingleton<IOutboxTrigger>(sp => sp.GetRequiredService<OutboxProcessor<TOutboxItem>>())
           .AddSingleton<IAwaitOutboxCompletion>(sp => sp.GetRequiredService<OutboxProcessor<TOutboxItem>>())
           .AddSingleton<RetryDelayGenerator>();
    }
}