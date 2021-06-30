using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            var data = new Dictionary<string, object>
            {
                {"key", "value"}
            };
            var result = await client.GetAsync("https://api.agify.io/?name=andrew", cancellationToken);
            return result.StatusCode == HttpStatusCode.OK 
                ? new HealthCheckResult(HealthStatus.Healthy, "healthy", null, data)
                : new HealthCheckResult(HealthStatus.Unhealthy,"unhealthy", null, data);
        }
    }
}