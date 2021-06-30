using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NetCoreHealthCheckPOC.DataAccess
{
    public class ExceptionThrowingDao : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new Exception("I was thrown", new Exception("this is an inner exception"));
        }
    }
}