using Liike.Application;
using Liike.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// The API is the composition root; each layer owns its own registrations.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

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
app.UseAuthorization();
app.MapControllers();

app.Run();
