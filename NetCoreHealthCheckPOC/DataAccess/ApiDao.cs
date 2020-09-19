using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NetCoreHealthCheckPOC.DataAccess
{
    public class ApiDao : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            var client = new HttpClient();
            var result = await client.GetAsync("https://api.agify.io/?name=andrew", cancellationToken);
            if (result.StatusCode == HttpStatusCode.OK)
            {
                return new HealthCheckResult(HealthStatus.Healthy);
            }

            return new HealthCheckResult(HealthStatus.Unhealthy);
        }
    }
}