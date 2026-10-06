using Liike.Application;
using Liike.Domain;
using Liike.Infrastructure;
using Liike.Infrastructure.Persistence;
using Liike.Infrastructure.Security;
using Liike.Swagger;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

if (args is ["--hash-password"])
{
    var password = ReadPasswordFromConsole();
    if (password.Length is < 12 or > 128)
    {
        Console.Error.WriteLine("Password must be between 12 and 128 characters.");
        return;
    }

    Console.WriteLine($"PasswordHash: {new PasswordHasher<UserAccount>().HashPassword(new UserAccount(), password)}");
    return;
}

var builder = WebApplication.CreateBuilder(args);

// The API is the composition root; each layer owns its own registrations.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    var xmlFile = $"{builder.Environment.ApplicationName}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token returned by the login endpoint."
    });
    options.OperationFilter<BearerAuthorizationOperationFilter>();
});
builder.Services.AddProblemDetails();

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (!jwtOptions.IsValid())
{
    throw new InvalidOperationException(
        "Configure Jwt:Issuer, Jwt:Audience, Jwt:SigningKey (at least 32 bytes), and a valid Jwt:LifetimeMinutes.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var accountIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier) ??
                                     context.Principal?.FindFirstValue("sub");
                var roleValue = context.Principal?.FindFirstValue(ClaimTypes.Role);
                if (!Guid.TryParse(accountIdValue, out var accountId) ||
                    !Enum.TryParse<AccountRole>(roleValue, out var tokenRole))
                {
                    context.Fail("The token does not identify a valid account.");
                    return;
                }

                var dbContext = context.HttpContext.RequestServices.GetRequiredService<ApplicationDbContext>();
                var account = await dbContext.UserAccounts
                    .AsNoTracking()
                    .Where(userAccount => userAccount.Id == accountId)
                    .Select(userAccount => new
                    {
                        userAccount.Role,
                        userAccount.OrganizerId,
                        IsActive = userAccount.Role != AccountRole.Organizer ||
                                   (userAccount.Organizer != null && userAccount.Organizer.IsActive)
                    })
                    .SingleOrDefaultAsync(context.HttpContext.RequestAborted);

                var tokenOrganizerId = context.Principal?.FindFirstValue("organizer_id");
                if (account is null || account.Role != tokenRole || !account.IsActive ||
                    (tokenRole == AccountRole.Organizer &&
                     (!Guid.TryParse(tokenOrganizerId, out var organizerId) || account.OrganizerId != organizerId)))
                {
                    context.Fail("The account is no longer authorized.");
                }
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Organizer", policy => policy
        .RequireAuthenticatedUser()
        .RequireRole(AccountRole.Organizer.ToString())
        .RequireClaim("must_change_password", "false"));
    options.AddPolicy("Admin", policy => policy
        .RequireAuthenticatedUser()
        .RequireRole(AccountRole.Admin.ToString()));
    options.AddPolicy("AccountOwner", policy => policy
        .RequireAuthenticatedUser()
        .RequireRole(AccountRole.Organizer.ToString(), AccountRole.Admin.ToString()));
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("PublicSearch", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 100,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("Login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("Organizer", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ??
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

// Emit structured logs in hosted environments while keeping local output readable.
builder.Logging.ClearProviders();
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddConsole();
}
else
{
    builder.Logging.AddJsonConsole();
}

// Authorization policies can be added later; no authentication scheme is configured yet.
builder.Services.AddAuthorization();

var app = builder.Build();

// Converts unhandled exceptions to generic Problem Details responses without leaking internals.
app.UseExceptionHandler();

// Swagger is kept out of production by default to avoid exposing API metadata publicly.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

app.Run();

static string ReadPasswordFromConsole()
{
    if (Console.IsInputRedirected)
    {
        throw new InvalidOperationException("Run the hash command in an interactive terminal.");
    }

    Console.Write("Enter password (input hidden): ");
    var password = new StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return password.ToString();
        }

        if (key.Key == ConsoleKey.Backspace)
        {
            if (password.Length > 0)
            {
                password.Length--;
            }

            continue;
        }

        if (!char.IsControl(key.KeyChar))
        {
            password.Append(key.KeyChar);
        }
    }
}
