// SERVICIOCORE_PROCESARDOCUMENTOSSAP/Program.cs
using Application.Abstractions;
using Application.Commands.Items;
using Application.Commands.Items.Cliente;
using Application.Commands.Items.Dealer;
using Application.Commands.Order;
using Application.Commands.Order.Cliente;
using Application.Commands.Order.Dealer;
using Application.Commands.Precio;
using Application.Commands.Precio.Cliente;
using Application.Commands.Precio.Dealer;
using Application.Commands.Stock;
using Application.Commands.Stock.Cliente;
using Application.Commands.Stock.Dealer;
using Application.Configuration;
using Application.DTO;
using Application.Handlers.Items;
using Application.Handlers.Items.Cliente;
using Application.Handlers.Items.Dealer;
using Application.Handlers.Order;
using Application.Handlers.Order.Cliente;
using Application.Handlers.Order.Dealer;
using Application.Handlers.Precio;
using Application.Handlers.Precio.Cliente;
using Application.Handlers.Precio.Dealer;
using Application.Handlers.Stock;
using Application.Handlers.Stock.Cliente;
using Application.Handlers.Stock.Dealer;
using Application.Interfaces;
using Application.Interfaces.API;
using Application.Interfaces.HANA;
using Application.Interfaces.Security;
using Application.Interfaces.UseCases.Items.Cliente;
using Application.Interfaces.UseCases.Items.Dealer;
using Application.Interfaces.UseCases.Order.Cliente;
using Application.Interfaces.UseCases.Order.Dealer;
using Application.Interfaces.UseCases.Precio.Cliente;
using Application.Interfaces.UseCases.Precio.Dealer;
using Application.Interfaces.UseCases.Stock.Cliente;
using Application.Interfaces.UseCases.Stock.Dealer;
using Application.UseCases.Items.Cliente;
using Application.UseCases.Items.Dealer;
using Domain.Configuration;
using Domain.SAP;
using Infrastructure.API;
using Infrastructure.DataProtection;
using Infrastructure.HANA;
using Infrastructure.Helper;
using Infrastructure.Logging;
using Infrastructure.Security;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using SERVICIOCORE_PROCESARDOCUMENTOSSAP;
using System.Net;
using System.Reflection;

var builder = Host.CreateApplicationBuilder(args);
// Configurar como servicio de Windows
if (OperatingSystem.IsWindows())
{
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = "SERVICIO_CORE_PROCESARDOCUMENTOS";
    });
}
// ==========================
// DataProtection Configuration
// ==========================
// Obtener la ruta de las keys desde configuración o usar ruta relativa al ejecutable
var keysPath = builder.Configuration["DataProtection:KeysDirectory"];

if (string.IsNullOrWhiteSpace(keysPath))
{
    // Si no está configurado, usar ruta relativa al directorio del ejecutable
    keysPath = Path.Combine(AppContext.BaseDirectory, "Key");
}
else if (!Path.IsPathRooted(keysPath))
{
    // Si es una ruta relativa, combinarla con el directorio del ejecutable
    keysPath = Path.Combine(AppContext.BaseDirectory, keysPath);
}

var keysDirectory = new DirectoryInfo(keysPath);
if (!keysDirectory.Exists)
{
    keysDirectory.Create();
}

builder.Services.AddDataProtection()
    .SetApplicationName("SapWorker")
    .PersistKeysToFileSystem(keysDirectory)
    .SetDefaultKeyLifetime(TimeSpan.FromDays(360) * 5);

// ==========================
// Secret Protector
// ==========================
builder.Services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();

// ==========================
// Build temporary service provider para desencriptar
// ==========================
var tempServiceProvider = builder.Services.BuildServiceProvider();
var secretProtector = tempServiceProvider.GetRequiredService<ISecretProtector>();

// ==========================
// Helper para desencriptar valores
// ==========================
static string DecryptIfNeeded(string? value, ISecretProtector protector)
{
    if (string.IsNullOrWhiteSpace(value))
        return string.Empty;

    if (value.StartsWith("enc:", StringComparison.OrdinalIgnoreCase))
        return protector.Decrypt(value);

    return value;
}

// ==========================
// Desencriptar valores en Program.cs (buena práctica)
// ==========================
var secretsSection = builder.Configuration.GetSection("ExternalServices:OptionSecretsSL");
var decryptedSecrets = new OptionSecretsSL
{
    ApiMiddlewareIPUrl = DecryptIfNeeded(secretsSection["ApiMiddlewareIPUrl"], secretProtector),
    ProcesarDocumentoEndPoint = DecryptIfNeeded(secretsSection["ProcesarDocumentoEndPoint"], secretProtector),
    ProcesarArticuloClienteEndPoint = DecryptIfNeeded(secretsSection["ProcesarArticuloClienteEndPoint"], secretProtector),
    ProcesarArticuloDealerEndPoint = DecryptIfNeeded(secretsSection["ProcesarArticuloDealerEndPoint"], secretProtector),
    ProcesarStockClienteEndPoint = DecryptIfNeeded(secretsSection["ProcesarStockClienteEndPoint"], secretProtector),
    ProcesarStockDealerEndPoint = DecryptIfNeeded(secretsSection["ProcesarStockDealerEndPoint"], secretProtector),
    ProcesarPrecioClienteEndPoint = DecryptIfNeeded(secretsSection["ProcesarPrecioClienteEndPoint"], secretProtector),
    ProcesarPrecioDealerEndPoint = DecryptIfNeeded(secretsSection["ProcesarPrecioDealerEndPoint"], secretProtector),
    GetOrdenesClienteEndPoint = DecryptIfNeeded(secretsSection["GetOrdenesClienteEndPoint"], secretProtector),
    GetOrdenesDealerEndPoint = DecryptIfNeeded(secretsSection["GetOrdenesDealerEndPoint"], secretProtector),
    AuthEndpoint = secretsSection["AuthEndpoint"], // No necesita desencriptar, es solo una ruta
    ApiClientId = secretsSection["ApiClientId"], // No necesita desencriptar si es público
    ApiClientSecret = DecryptIfNeeded(secretsSection["ApiClientSecret"], secretProtector), // Puede estar encriptado    
};
// Configurar OptionSecretsSL con valores ya desencriptados
builder.Services.Configure<OptionSecretsSL>(options =>
{ 
    options.ApiMiddlewareIPUrl = decryptedSecrets.ApiMiddlewareIPUrl;
    options.ProcesarDocumentoEndPoint = decryptedSecrets.ProcesarDocumentoEndPoint;
    options.ProcesarArticuloClienteEndPoint = decryptedSecrets.ProcesarArticuloClienteEndPoint;
    options.ProcesarArticuloDealerEndPoint = decryptedSecrets.ProcesarArticuloDealerEndPoint;
    options.ProcesarStockClienteEndPoint = decryptedSecrets.ProcesarStockClienteEndPoint;
    options.ProcesarStockDealerEndPoint = decryptedSecrets.ProcesarStockDealerEndPoint;
    options.ProcesarPrecioClienteEndPoint = decryptedSecrets.ProcesarPrecioClienteEndPoint;
    options.ProcesarPrecioDealerEndPoint = decryptedSecrets.ProcesarPrecioDealerEndPoint;
    options.GetOrdenesClienteEndPoint = decryptedSecrets.GetOrdenesClienteEndPoint;
    options.GetOrdenesDealerEndPoint = decryptedSecrets.GetOrdenesDealerEndPoint;
    options.AuthEndpoint = decryptedSecrets.AuthEndpoint;
    options.ApiClientId = decryptedSecrets.ApiClientId;
    options.ApiClientSecret = decryptedSecrets.ApiClientSecret;
});


// Desencriptar configuración HANA si es necesario

var hanaSection = builder.Configuration.GetSection("OdbcSettings:HANA");
var decryptedHanaServer = DecryptIfNeeded(hanaSection["Server"], secretProtector);
var decryptedHanaDatabase = DecryptIfNeeded(hanaSection["Database"], secretProtector);
var decryptedHanaPassword = DecryptIfNeeded(hanaSection["Password"], secretProtector);
var decryptedHanaUserId = DecryptIfNeeded(hanaSection["UserId"], secretProtector);


// Liberar el service provider temporal
(tempServiceProvider as IDisposable)?.Dispose();

// ==========================
// Configuración desde appsettings.json
// ==========================
builder.Services.Configure<WorkerSettings>(
    builder.Configuration.GetSection("AppWorkerSettings"));


// Configurar OdbcSettings con valores desencriptados
builder.Services.Configure<OdbcSettings>(options =>
{
    var hanaSection = builder.Configuration.GetSection("OdbcSettings:HANA");
    
    if (options.HANA == null)
        options.HANA = new HanaOdbcConfig();

    options.HANA.Driver = hanaSection["Driver"];
    options.HANA.Server = decryptedHanaServer; // Usar valor desencriptado
    options.HANA.Database = decryptedHanaDatabase;// Usar valor desencriptado  
    options.HANA.UserId = decryptedHanaUserId; // Usar valor desencriptado
    options.HANA.Password = decryptedHanaPassword; // Usar valor desencriptado
    options.HANA.CommandTimeout = hanaSection.GetValue<int>("CommandTimeout", 30);
  
});

// ==========================
// Servicios de Infrastructure
// ==========================
builder.Services.AddSingleton<IHanaConnectionFactory, HanaConnectionFactory>();
builder.Services.AddScoped<IHanaRepository, HanaRepository>();
builder.Services.AddScoped<ExecuteStoredProcedureHanaAsync>();

// ==========================
// JWT Token Service (obtener tokens de la API)
// ==========================
// Registrar IHttpClientFactory primero
builder.Services.AddHttpClient();
// Registrar como Singleton para mantener el caché del token
builder.Services.AddSingleton<IApiTokenService, ApiTokenService>();


// ==========================
// API Client con JWT Authentication Handler
// ==========================
builder.Services.AddHttpClient<IApiClient, ApiClient>()
    .AddHttpMessageHandler(serviceProvider =>
    {
        var tokenService = serviceProvider.GetRequiredService<IApiTokenService>();
        var logger = serviceProvider.GetRequiredService<ILogger<JwtAuthenticationHandler>>();
        return new JwtAuthenticationHandler(tokenService, logger);
    });

// ==========================
// Unit of Work
// ==========================
builder.Services.AddScoped<IUnitOfWork, HanaUnitOfWork>();

// ==========================
// Mapster Configuration
// ==========================
var typeAdapterConfig = TypeAdapterConfig.GlobalSettings;
MapsterConfig.ConfigureMappings();
builder.Services.AddSingleton(typeAdapterConfig);
builder.Services.AddScoped<IMapper, ServiceMapper>();

// ==========================
// Command Handlers
// ==========================

//ARTICULOS CLIENTE/DEALER
builder.Services.AddScoped<
    ICommandHandler<GetPendingClienteItemsTypeCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingClienteItemsTypeCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<GetPendingDealerItemsTypeCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingDealerItemsTypeCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<CheckItemExistsCommand, bool>,
    CheckItemExistsCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<InsertItemsCommand, bool>,
    InsertItemCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<MarkStatusItemAsCommand, bool>,
    MarkStatusItemAsCommandHandler>();

//ENVIO HACIA API - ARTICULOS
builder.Services.AddScoped<
    ICommandHandler<GetPendingClienteItemsToApiCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingClienteItemsToApiCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<GetPendingDealerItemsToApiCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingDealerItemsToApiCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<SendItemsToApiCommand, (bool IsSuccess, string? Message)>,
    SendItemsToApiCommandHandler>();

//STOCK CLIENTE/DEALER
builder.Services.AddScoped<
    ICommandHandler<GetPendingClienteStockTypeCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingClienteStockTypeCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<GetPendingDealerStockTypeCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingDealerStockTypeCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<InsertStockCommand, bool>,
    InsertStockCommandHandler>();


builder.Services.AddScoped<
    ICommandHandler<MarkStatusStockAsCommand, bool>,
    MarkStatusStockAsCommandHandler>();

//ENVIO HACIA API - STOCK
builder.Services.AddScoped<
    ICommandHandler<GetPendingClienteStockToApiCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingClienteStockToApiCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<GetPendingDealerStockToApiCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingDealerStockToApiCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<SendStockToApiCommand, (bool IsSuccess, string? Message)>,
    SendStockToApiCommandHandler>();

//PRECIO CLIENTE/DEALER
builder.Services.AddScoped<
    ICommandHandler<GetPendingClientePrecioTypeCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingClientePrecioTypeCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<GetPendingDealerPrecioTypeCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingDealerPrecioTypeCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<InsertPrecioCommand, bool>,
    InsertPrecioCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<MarkStatusPrecioAsCommand, bool>,
    MarkStatusPrecioAsCommandHandler>();

//ENVIO HACIA API - PRECIO
builder.Services.AddScoped<
    ICommandHandler<GetPendingClientePrecioToApiCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingClientePrecioToApiCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<GetPendingDealerPrecioToApiCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingDealerPrecioToApiCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<SendPrecioToApiCommand, (bool IsSuccess, string? Message)>,
    SendPrecioToApiCommandHandler>();

//ORDENES
builder.Services.AddScoped<
    ICommandHandler<GetPendingsClienteOrdersCommand, IEnumerable<WooOrderDTO>>,
    GetPendingsClienteOrdersHandler>();

builder.Services.AddScoped<
    ICommandHandler<GetClienteOrdersToUpdateWoo, IEnumerable<SapItemQueeDTO>>,
    GetClienteOrdersToUpdateWooHandler>();

builder.Services.AddScoped<
    ICommandHandler<GetPendingsDealerOrdersCommand, IEnumerable<WooOrderDTO>>,
    GetPendingsDealerOrdersHandler>();

builder.Services.AddScoped<
    ICommandHandler<GetDealerOrdersToUpdateWoo, IEnumerable<SapItemQueeDTO>>,
    GetDealerOrdersToUpdateWooHandler>();

builder.Services.AddScoped<
    ICommandHandler<CheckOrderExistsCommand, bool>,
    CheckOrderExistsCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<InsertOrdersCommand, bool>,
    InsertOrdersCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<MarkStatusOrderAsCommand, bool>,
    MarkStatusOrdenAsCommandHandler>();

// ==========================
// UseCases
// ==========================

//ARTICULOS
builder.Services.AddScoped<IGetClienteItemsSapUseCase, GetClienteItemsSapUseCase>();
builder.Services.AddScoped<IGetDealerItemsSapUseCase, GetDealerItemsSapUseCase>();
builder.Services.AddScoped<IProcesarClienteItemsHanaUseCase, ProcesarClienteItemsHanaUseCase>();
builder.Services.AddScoped<IProcesarDealerItemsHanaUseCase, ProcesarDealerItemsHanaUseCase>();

//STOCK
builder.Services.AddScoped<IGetClienteStockSapUseCase, GetClienteStockSapUseCase>();
builder.Services.AddScoped<IGetDealerStockSapUseCase, GetDealerStockSapUseCase>();
builder.Services.AddScoped<IProcesarClienteStockHanaUseCase, ProcesarClienteStockHanaUseCase>();
builder.Services.AddScoped<IProcesarDealerStockHanaUseCase, ProcesarDealerStockHanaUseCase>();

//PRECIO
builder.Services.AddScoped<IGetClientePrecioSapUseCase, GetClientePrecioSapUseCase>();
builder.Services.AddScoped<IGetDealerPrecioSapUseCase, GetDealerPrecioSapUseCase>();
builder.Services.AddScoped<IProcesarClientePrecioHanaUseCase, ProcesarClientePrecioHanaUseCase>();
builder.Services.AddScoped<IProcesarDealerPrecioHanaUseCase, ProcesarDealerPrecioHanaUseCase>();

//ORDENES
builder.Services.AddScoped<IGetClienteOrderSapUseCase, GetClienteOrderSapUseCase>();
builder.Services.AddScoped<IGetDealerOrderSapUseCase, GetDealerOrderSapUseCase>();
builder.Services.AddScoped<IProcesarClienteOrderSapUseCase, ProcesarClienteOrderSapUseCase>();
builder.Services.AddScoped<IProcesarDealerOrderSapUseCase, ProcesarDealerOrderSapUseCase>();

// ==========================
// Worker
// ==========================
builder.Services.AddHostedService<Worker>();

// ==========================
// Logging
// ==========================
// Obtener la ruta del directorio del proyecto para los logs
var projectName = Assembly.GetExecutingAssembly().GetName().Name;
var logDirectory = Path.Combine(AppContext.BaseDirectory, "LOGS"  + "_" + projectName);

builder.Services.AddLogging(logging =>
{
    logging.ClearProviders();
    logging.AddConsole();
    // Crear el FileLoggerProvider directamente, sin lambda
    var serviceProvider = builder.Services.BuildServiceProvider();
    var workerSettings = serviceProvider.GetRequiredService<IOptions<WorkerSettings>>();
    logging.AddProvider(new FileLoggerProvider(logDirectory, workerSettings));
    logging.SetMinimumLevel(LogLevel.Information);
});

// ==========================
// Security Protocol
// ==========================
ServicePointManager.SecurityProtocol =
    SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;


// ==========================
// Build and Run
// ==========================
var host = builder.Build();

using var scope = host.Services.CreateScope();

var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

var assembly = Assembly.GetExecutingAssembly();
var assemblyName = assembly.GetName().Name;
var assemblyVersion = assembly.GetName().Version?.ToString() ?? "0.0.0";
logger.LogInformation(
    "Aplicación iniciada | {Service} | Versión: {Version} | Ambiente: {Environment}",
    assemblyName,
    assemblyVersion,
    environment.EnvironmentName
);


host.Run();