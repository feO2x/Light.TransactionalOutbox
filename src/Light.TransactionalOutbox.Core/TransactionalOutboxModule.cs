﻿using System;
using Light.TransactionalOutbox.Core.OutboxProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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
            services.AddResiliencePipeline(
                OutboxConstants.ResiliencePipelineKey,
                pipelineBuilder =>
                {
                    pipelineBuilder
                       .AddRetry(
                            new RetryStrategyOptions
                            {
                                Delay = TimeSpan.FromSeconds(1),
                                MaxDelay = TimeSpan.FromMinutes(1),
                                MaxRetryAttempts = int.MaxValue,
                                BackoffType = DelayBackoffType.Linear,
                                UseJitter = true
                            }
                        );
                }
            );
        }

        services
           .AddOptions<OutboxProcessorOptions>()
           .BindConfiguration(configSectionPath)
           .ValidateOnStart();

        return services
           .AddSingleton<IValidateOptions<OutboxProcessorOptions>, OutboxProcessing.OutboxProcessorOptionsValidator>()
           .AddSingleton<OutboxProcessor<TOutboxItem>>()
           .AddSingleton<IOutboxProcessor>(sp => sp.GetRequiredService<OutboxProcessor<TOutboxItem>>())
           .AddSingleton<IOutboxTrigger>(sp => sp.GetRequiredService<OutboxProcessor<TOutboxItem>>())
           .AddSingleton<IAwaitOutboxCompletion>(sp => sp.GetRequiredService<OutboxProcessor<TOutboxItem>>());
    }
}