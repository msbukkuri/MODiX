﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Modix.Business.Diagnostics
{
    public static class Setup
    {
        public static IServiceCollection AddDiagnostics(this IServiceCollection services, IConfiguration configuration)
            => services
                .Add(services => services.AddOptions<DiagnosticsConfiguration>()
                    .Bind(configuration.GetSection("MODiX").GetSection("Business").GetSection("Diagnostics"))
                    .ValidateDataAnnotations()
                    .ValidateOnStartup())
                .AddScoped<IDiagnosticsService, DiagnosticsService>();
    }
}
