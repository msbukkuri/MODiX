﻿using System;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Options
{
    public class OptionsValidationStartupAction<TOptions>
            : StartupActionBase
        where TOptions : class, new()
    {
        public OptionsValidationStartupAction(
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override Task OnStartupAsync(CancellationToken cancellationToken)
        {
            // Force the options object to bind.
            _ = _serviceProvider.GetRequiredService<IOptions<TOptions>>().Value;

            return Task.CompletedTask;
        }

        private readonly IServiceProvider _serviceProvider;
    }
}
