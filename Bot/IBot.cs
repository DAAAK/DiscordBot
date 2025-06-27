using Microsoft.Extensions.DependencyInjection;

public interface IBot
{
    Task StartAsync(ServiceProvider services);
    Task StopAsync();
}
