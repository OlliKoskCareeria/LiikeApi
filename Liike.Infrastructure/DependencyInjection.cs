using Liike.Application.Services;
using Liike.Infrastructure.Persistence;
using Liike.Infrastructure.Security;
using Liike.Infrastructure.Services;
using Liike.Domain;
using Microsoft.AspNetCore.Identity;
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
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(options => options.IsValid(), "JWT issuer, audience, signing key, and lifetime must be configured.")
            .ValidateOnStart();
        services.AddScoped<IPasswordHasher<UserAccount>, PasswordHasher<UserAccount>>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddHttpClient<NominatimGeocodingService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddScoped<IGeocodingService>(serviceProvider =>
            serviceProvider.GetRequiredService<NominatimGeocodingService>());
        services.AddScoped<IPublicEventService, PublicEventService>();
        services.AddScoped<IOrganizerEventService, OrganizerEventService>();
        services.AddScoped<IOrganizerService, OrganizerService>();

        return services;
    }
}
