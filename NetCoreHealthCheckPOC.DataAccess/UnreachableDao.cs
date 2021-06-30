using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NetCoreHealthCheckPOC.DataAccess
{
    public class UnreachableDao : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri("https://google.com/")
            };

            var result = await client.GetAsync("this_does_not_exist", cancellationToken);
            if (result.StatusCode == HttpStatusCode.OK)
            {
                return new HealthCheckResult(HealthStatus.Healthy);
            }

            return new HealthCheckResult(HealthStatus.Unhealthy);
        }
    }
}