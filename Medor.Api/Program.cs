// Medor.Api entry point: Serilog, optional .env, EF Core + SQL Server, typed HttpClients (CoinDesk, ČNB), CORS, migrations.
using Medor.Api;
using Medor.Api.Data;
using Medor.Api.Persistence;
using Medor.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Medor API");

    LocalEnv.LoadOptional();

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    builder.Services.AddControllers();

    builder.Services.AddOpenApi();

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "Connection string 'DefaultConnection' is missing. Set ConnectionStrings__DefaultConnection " +
            "(environment variable), add a Medor.Api/.env file from .env.example, or use " +
            "`dotnet user-secrets set ConnectionStrings:DefaultConnection \"...\"` for local development.");
    }

    builder.Services.AddDbContext<MedorDbContext>(options =>
        options.UseSqlServer(connectionString));

    builder.Services.Configure<CoinDeskOptions>(builder.Configuration.GetSection(CoinDeskOptions.SectionName));
    builder.Services.Configure<CnbOptions>(builder.Configuration.GetSection(CnbOptions.SectionName));

    builder.Services.AddHttpClient<ICoinDeskClient, CoinDeskClient>((sp, client) =>
    {
        var opt = sp.GetRequiredService<IOptions<CoinDeskOptions>>().Value;
        client.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(30);
    });
    builder.Services.AddHttpClient<ICnbExchangeRateClient, CnbExchangeRateClient>((_, client) =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });

    builder.Services.AddScoped<IBitcoinPriceRepository, BitcoinPriceRepository>();
    builder.Services.AddScoped<ILiveBitcoinPriceService, LiveBitcoinPriceService>();
    builder.Services.AddScoped<ISavedBitcoinPriceService, SavedBitcoinPriceService>();

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            policy.WithOrigins(
                    "https://localhost:7003",
                    "http://localhost:5192")
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
    });

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseSerilogRequestLogging();
    app.UseCors();
    app.UseAuthorization();
    app.MapControllers();

    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<MedorDbContext>();
        await db.Database.MigrateAsync().ConfigureAwait(false);
    }

    await app.RunAsync().ConfigureAwait(false);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}
