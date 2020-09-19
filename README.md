Net Core HealthCheck
======

Usage
======

Basic Healthcheck
------

Using the MapNetCoreHealthChecks method will create a route that will execute a healthcheck

Inside of Startup.cs of your web project, add the following lines to the Configure method:
```
app.UseEndpoints(endpoints =>
{
    endpoints.MapNetCoreHealthChecks(); // this needs to be added to create the "/Health" route
});
```

You can also set a custom route
```
app.UseEndpoints(endpoints =>
{
    endpoints.MapNetCoreHealthChecks("MyRouteName");
});
```

Extensive Healthcheck
------
Using the AddCheckHealthAsyncMethodsToHealthCheck method will add to your healthcheck the result of each CheckHealthAsync method for each class implementing the IHealthCheck interface

Inside of Startup.cs of your web project, add the following lines to the ConfigureServices method:
```
services.AddCheckHealthAsyncMethodsToHealthCheck();
```

Implement the IHealthCheck interface on your data access classes
```
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
```