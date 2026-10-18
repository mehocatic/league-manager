using LeagueManager.Api.Authentication;
using LeagueManager.Api.OpenApi;
using LeagueManager.Application;
using LeagueManager.Infrastructure;
using LeagueManager.Infrastructure.Ai;
using LeagueManager.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is missing.");

// Add services to the container.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(connectionString, builder.Environment.ContentRootPath);
builder.Services.AddLeagueAuthentication(builder.Configuration, builder.Environment);
builder.Services.Configure<GeminiOptions>(builder.Configuration.GetSection(GeminiOptions.SectionName));
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<BearerSecurityTransformer>();
    options.AddOperationTransformer<BearerSecurityTransformer>();
});

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Only the Swagger UI page is used; it renders the built-in /openapi/v1.json, so Swashbuckle's own generator is not needed.
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "League Manager API v1"));
}

app.UseHttpsRedirection();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
