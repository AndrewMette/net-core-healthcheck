using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
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
            // get all of the assemblies to check based on AppDomain.CurrentDomain.BaseDirectory
            var assemblies = GetSolutionAssemblies();
            
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
            // get the type of IHealthCheck so we can use it inside our foreach loop
            var typeOfIHealthCheck = typeof(IHealthCheck);

            // loop over our assemblies
            foreach (var assembly in assemblies)
            {
                // prevent failure from being unable to load certain types from Microsoft assemblies
                try
                {
                    // get all the classes in the current assembly that implement
                    // IHealthCheck that aren't in the same namespace as the interface
                    var typesThatImplementIHealthCheck = assembly
                        .GetTypes()
                        .Where(type => typeOfIHealthCheck.IsAssignableFrom(type)
                                       && type.IsClass
                                       && type.Namespace != typeOfIHealthCheck.Namespace);

                    // for each type found, add its CheckHealthAsync method to the health check
                    foreach (var typeThatImplementsIHealthCheck in typesThatImplementIHealthCheck)
                    {
                        var addCheckGenericMethodInfo = addCheckMethodInfo.MakeGenericMethod(typeThatImplementsIHealthCheck);
                        var invokeParameters = new object[] { iHealthChecksBuilder, $"{typeThatImplementsIHealthCheck.Name}", null, null };
                        addCheckGenericMethodInfo.Invoke(iHealthChecksBuilder, invokeParameters);
                    }
                }
                // swallow this type of exception - just move on with what you can load
                catch (ReflectionTypeLoadException) { }
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

                    //var dependencyStates = healthReport.Entries.Select(e => new
                    //{
                    //    Key = e.Key,
                    //    Value = new
                    //    {
                    //        Status = e.Value.Status.ToString(),
                    //        Description = string.IsNullOrWhiteSpace(e.Value.Description)
                    //            ? e.Key
                    //            : e.Value.Description,
                    //        Duration = e.Value.Duration,
                    //        Exception = e.Value.Exception,
                    //        Data = e.Value.Data,
                    //        Tags = e.Value.Tags
                    //    }
                    //});
                    List<dynamic> dependencyStates = new List<dynamic>();
                    foreach (var entry in healthReport.Entries)
                    {
                        dynamic value = new ExpandoObject();
                        value.Status = entry.Value.Status.ToString();
                        value.Description = string.IsNullOrWhiteSpace(entry.Value.Description)
                                    ? entry.Key
                                    : entry.Value.Description;
                        value.Duration = entry.Value.Duration;
                        if (entry.Value.Exception != null)
                        {
                            var displayException = CreateDisplayException(entry.Value.Exception);
                            value.Exception = displayException;
                        }
                        if (entry.Value.Data != null && entry.Value.Data.Any())
                        {
                            value.Data = entry.Value.Data;
                        }
                        
                        //value.Tags = entry.Value.Tags;

                        var dependencyState = new
                        {
                            Key = entry.Key,
                            Value = value
                        };
                        dependencyStates.Add(dependencyState);
                    }

                    var result = new
                    {
                        Status = healthReport.Status.ToString(),
                        RanOn = DateTime.Now,
                        TotalDuration = healthReport.TotalDuration,
                        DependencyStates = dependencyStates
                    };

                    await context.Response.WriteAsync(JsonConvert.SerializeObject(result));
                }
            };

            return options;
        }

        private static dynamic CreateDisplayException(Exception exception)
        {
            dynamic displayException = new ExpandoObject();
            displayException.ClassName = exception.GetType().Name;
            displayException.Message = exception.Message;
            displayException.StackTraceString = exception.StackTrace;

            if (exception.Data.Count > 0)
            {
                displayException.Data = exception.Data;
            }
            
            if (exception.InnerException != null)
            {
                displayException.InnerException = CreateDisplayException(exception.InnerException);
            }

            return displayException;
        }

        private static IEnumerable<Assembly> GetSolutionAssemblies()
        {
            var currentDomainBaseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var assemblies = Directory.GetFiles(currentDomainBaseDirectory, "*.dll")
                .Select(x => Assembly.Load(AssemblyName.GetAssemblyName(x)));
            return assemblies;
        }
    }
}
