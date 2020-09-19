using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Newtonsoft.Json;

namespace NetCoreHealthCheck
{
    public static class Extensions
    {
        // this method creates a health endpoint w/o a controller, and with the options defined in the referenced method
        public static void MapNetCoreHealthChecks(this IEndpointRouteBuilder endpoints, string route = "/Health", HealthCheckOptions options = null)
        {
            options ??= GetHealthCheckOptions();
            endpoints.MapHealthChecks(route, options);
        }

        // without this method, the health check only determines if the service is alive
        public static void AddCheckHealthAsyncMethodsToHealthCheck(this IServiceCollection services)
        {
            // add health checks and save off the builder for later
            var iHealthChecksBuilder = services.AddHealthChecks();

            // get all the types for classes that implement the IHealthCheck interface
            var typesThatImplementIHealthCheck = Assembly.GetCallingAssembly()
                .GetTypes()
                .Where(type => typeof(IHealthCheck).IsAssignableFrom(type)
                               && type.IsClass);

            // get the AddCheck generic method that is normally used as follows:
            // services.AddHealthChecks().AddCheck<Dao>("some name");
            var addCheckMethodInfo = typeof(HealthChecksBuilderAddCheckExtensions)
                .GetMethods()
                .Single(method => method.IsGenericMethod
                                 && method.Name == "AddCheck"
                                 && method.GetParameters()
                                     .Select(parameter => parameter.ParameterType)
                                     .SequenceEqual(new List<Type>
                                     {
                                         typeof(IHealthChecksBuilder),
                                         typeof(string),
                                         typeof(HealthStatus?),
                                         typeof(IEnumerable<string>)
                                     }));

            foreach (var typeThatImplementsIHealthCheck in typesThatImplementIHealthCheck)
            {
                var addCheckGenericMethodInfo = addCheckMethodInfo.MakeGenericMethod(typeThatImplementsIHealthCheck);
                var invokeParameters = new object[] { iHealthChecksBuilder, $"{typeThatImplementsIHealthCheck.Name}", null, null };
                addCheckGenericMethodInfo.Invoke(iHealthChecksBuilder, invokeParameters);
            }
        }

        private static HealthCheckOptions GetHealthCheckOptions()
        {
            var options = new HealthCheckOptions
            {
                ResponseWriter = async (context, healthReport) =>
                {
                    context.Response.ContentType = "application/json";
                    
                    context.Response.StatusCode =
                        healthReport.Entries.Any(entry => entry.Value.Status == HealthStatus.Unhealthy)
                            ? 503
                            : 200;

                    var result = new
                    {
                        Status = healthReport.Status.ToString(),
                        RanOn = DateTime.Now,
                        TotalDuration = healthReport.TotalDuration,
                        DependencyStates = healthReport.Entries.Select(e => new
                        {
                            Key = e.Key,
                            Value = new
                            {
                                Status = e.Value.Status.ToString(),
                                Description = string.IsNullOrWhiteSpace(e.Value.Description)
                                    ? e.Key
                                    : e.Value.Description,
                                Duration = e.Value.Duration,
                                Exception = e.Value.Exception,
                                Data = e.Value.Data,
                                Tags = e.Value.Tags
                            }
                        })
                    };

                    await context.Response.WriteAsync(JsonConvert.SerializeObject(result));
                }
            };

            return options;
        }
    }
}
