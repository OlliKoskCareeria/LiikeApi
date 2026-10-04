using Microsoft.Extensions.DependencyInjection;

namespace Liike.Application;

public static class DependencyInjection
{
    // Keep Application registrations behind one entry point; register no business services until they exist.
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
