// SERVICIOCORE_PROCESARDOCUMENTOSSAP/Program.cs
using Application.Abstractions;
using Application.Commands.Items;
using Application.Commands.Items.Cliente;
using Application.Commands.Items.Dealer;
using Application.Commands.Stock.Cliente;
using Application.Configuration;
using Application.DTO;
using Application.Handlers.Items;
using Application.Handlers.Items.Cliente;
using Application.Handlers.Items.Dealer;
using Application.Interfaces;
using Application.Interfaces.API;
using Application.Interfaces.HANA;
using Application.Interfaces.Security;
using Application.Interfaces.UseCases.Items.Cliente;
using Application.Interfaces.UseCases.Items.Dealer;
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
    //CompanyDBSAP = DecryptIfNeeded(secretsSection["CompanyDBSAP"], secretProtector),
    //UserSLSAP = DecryptIfNeeded(secretsSection["UserSLSAP"], secretProtector),
    ApiMiddlewareIPUrl = DecryptIfNeeded(secretsSection["ApiMiddlewareIPUrl"], secretProtector),
    ProcesarDocumentoEndPoint = DecryptIfNeeded(secretsSection["ProcesarDocumentoEndPoint"], secretProtector),
    ProcesarArticuloClienteEndPoint = DecryptIfNeeded(secretsSection["ProcesarArticuloClienteEndPoint"], secretProtector),
    ProcesarArticuloDealerEndPoint = DecryptIfNeeded(secretsSection["ProcesarArticuloDealerEndPoint"], secretProtector),
    AuthEndpoint = secretsSection["AuthEndpoint"], // No necesita desencriptar, es solo una ruta
    ApiClientId = secretsSection["ApiClientId"], // No necesita desencriptar si es público
    ApiClientSecret = DecryptIfNeeded(secretsSection["ApiClientSecret"], secretProtector), // Puede estar encriptado
    //BaseUrlSLSAP = DecryptIfNeeded(secretsSection["BaseUrlSLSAP"], secretProtector),
    //LoginSLSAP = DecryptIfNeeded(secretsSection["LoginSLSAP"], secretProtector),
    //TransferEndPointSLSAP = DecryptIfNeeded(secretsSection["TransferEndPointSLSAP"], secretProtector)
};
// Configurar OptionSecretsSL con valores ya desencriptados
builder.Services.Configure<OptionSecretsSL>(options =>
{
    //options.CompanyDBSAP = decryptedSecrets.CompanyDBSAP;
    //options.UserSLSAP = decryptedSecrets.UserSLSAP;
    //options.PassWordSLSAP = decryptedSecrets.PassWordSLSAP;
    //options.BaseUrlSLSAP = decryptedSecrets.BaseUrlSLSAP;
    //options.LoginSLSAP = decryptedSecrets.LoginSLSAP;
    //options.TransferEndPointSLSAP = decryptedSecrets.TransferEndPointSLSAP;

    options.ApiMiddlewareIPUrl = decryptedSecrets.ApiMiddlewareIPUrl;
    options.ProcesarDocumentoEndPoint = decryptedSecrets.ProcesarDocumentoEndPoint;
    options.ProcesarArticuloClienteEndPoint = decryptedSecrets.ProcesarArticuloClienteEndPoint;
    options.ProcesarArticuloDealerEndPoint = decryptedSecrets.ProcesarArticuloDealerEndPoint;
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

//DOCUMENTOS
//builder.Services.AddScoped<
//    ICommandHandler<Application.Commands.GetPendingDocumentsTypeCommand, IEnumerable<SapDrivinTableDTO>>,
//    Application.Handlers.GetPendingDocumentsTypeCommandHandler>();

//builder.Services.AddScoped<
//    ICommandHandler<Application.Commands.CheckDocumentExistsCommand, bool>,
//    Application.Handlers.CheckDocumentExistsCommandHandler>();


//builder.Services.AddScoped<
//    ICommandHandler<Application.Commands.InsertDocumentCommand, bool>,
//    Application.Handlers.InsertDocumentCommandHandler>();

//ARTICULOS CLIENTE/DEALER
builder.Services.AddScoped<
    ICommandHandler<GetPendingClienteItemsTypeCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingClienteItemsTypeCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<GetPendingDealerItemsTypeCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingDealerItemsTypeCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<GetPendingClienteStockTypeCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingClienteStockTypeCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<GetPendingDealerStockTypeCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingDealerStockTypeCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<GetPendingClienteItemsToApiCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingClienteItemsToApiCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<GetPendingDealerItemsToApiCommand, IEnumerable<SapItemQueeDTO>>,
    GetPendingDealerItemsToApiCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<CheckItemExistsCommand, bool>,
    CheckItemExistsCommandHandler>();


builder.Services.AddScoped<
    ICommandHandler<InsertItemsCommand, bool>,
    InsertItemCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<InsertStockCommand, bool>,
    InsertStockCommandHandler>();

// Handlers para proceso HANA → API
//builder.Services.AddScoped<
//    ICommandHandler<Application.Commands.GetPendingHanaDocumentsCommand, IEnumerable<SapDrivinTable>>,
//    Application.Handlers.GetPendingHanaDocumentsCommandHandler>();

//builder.Services.AddScoped<
//    ICommandHandler<Application.Commands.SendDocumentsToApiCommand, (bool IsSuccess, string? ErrorMessage)>,
//    Application.Handlers.SendStockDocumentsToApiCommandHandler>();

//builder.Services.AddScoped<
//    ICommandHandler<Application.Commands.MarkStatusDocuemntAsCommand, bool>,
//    Application.Handlers.MarkStatusDocumentAsCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<SendItemsToApiCommand, (bool IsSuccess, string? Message)>,
    SendItemsToApiCommandHandler>();

builder.Services.AddScoped<
    ICommandHandler<MarkStatusItemAsCommand, bool>,
    MarkStatusItemAsCommandHandler>();

//builder.Services.AddScoped<
//    ICommandHandler<Application.Commands.GetPendingHooksCommand, IEnumerable<SapDrivinTable>>,
//    Application.Handlers.GetPendingHooksCommandHandler>();

//builder.Services.AddScoped<
//    ICommandHandler<Application.Commands.VerifyDocumentStatusSAPCommand, bool>,
//    Application.Handlers.VerifyDocumentStatusSAPCommandHandler>();

//builder.Services.AddScoped<
//    ICommandHandler<Application.Commands.ChangeStatusDocumentSAPCommand, bool>,
//    Application.Handlers.ChangeStatusDocumentSAPCommandHandler>();

// ==========================
// UseCases
// ==========================
//builder.Services.AddScoped<GetDocumentsTypeSapUseCase>();
//builder.Services.AddScoped<ProcesarDocumentsHanaUseCase>();
//builder.Services.AddScoped<GetHookSapUseCase>();
builder.Services.AddScoped<IGetClienteItemsSapUseCase, GetClienteItemsSapUseCase>();
builder.Services.AddScoped<IGetDealerItemsSapUseCase, GetDealerItemsSapUseCase>();
builder.Services.AddScoped<IProcesarClienteItemsHanaUseCase, ProcesarClienteItemsHanaUseCase>();
builder.Services.AddScoped<IProcesarDealerItemsHanaUseCase, ProcesarDealerItemsHanaUseCase>();

//STOCK
builder.Services.AddScoped<IGetClienteStockSapUseCase, GetClienteStockSapUseCase>();
builder.Services.AddScoped<IGetDealerStockSapUseCase, GetDealerStockSapUseCase>();



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