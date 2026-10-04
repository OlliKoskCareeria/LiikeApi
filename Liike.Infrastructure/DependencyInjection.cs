using Liike.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Liike.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Connection strings belong in user-secrets locally and managed environment configuration in Azure.
        var connectionString = configuration.GetConnectionString("PostgreSQL");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "The PostgreSQL connection string is missing. Configure ConnectionStrings:PostgreSQL.");
        }

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
