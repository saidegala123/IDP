using DTPortal.Core.ConfigProviders;
using DTPortal.Core.Domain.Models;
using DTPortal.Core.Domain.Models.RegistrationAuthority;
using DTPortal.Core.Domain.Repositories;
using DTPortal.Core.Domain.Services;
using DTPortal.Core.Persistence.Repositories;
using DTPortal.Core.Services;
using DTPortal.Core.Utilities;
using DTPortal.IDP.Attribute;
using DTPortal.IDP.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NLog;
using NLog.Web;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VaultSharp;
using VaultSharp.V1.AuthMethods.Token;
using VaultSharp.V1.Commons;

var logger = NLog.LogManager.Setup().LoadConfigurationFromAppSettings().GetCurrentClassLogger();
logger.Info("Init main");

try
{
    var builder = WebApplication.CreateBuilder(args);

    var securityConfig = builder.Configuration
                         .GetSection("SecurityConfig")
                         .Get<SecurityConfig>();

    // Call each setup function only if the feature is enabled
    if (securityConfig?.UseRateLimiting == true)
        DTPortal.IDP.Extensions.WebHostExtensions.ConfigureRateLimiting(builder.Services, securityConfig, logger);

    if (securityConfig?.UseKestrelSettings == true)
        DTPortal.IDP.Extensions.WebHostExtensions.ConfigureKestrel(builder.WebHost, securityConfig, logger);

    await ConfigureServices(builder);

    //builder.Configuration.AddIniFile($"settings.{builder.Environment.EnvironmentName}", true, false);

    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddNLogWeb();

    //builder.WebHost.ConfigureKestrel(serverOptions =>
    //{
    //    serverOptions.Limits.MaxRequestHeadersTotalSize = 1048576;
    //});

    var app = builder.Build();

    logger.Info("WebApplication build successful");

    if (securityConfig?.UseSecurityHeaders == true)
        DTPortal.IDP.Extensions.WebHostExtensions.ConfigureSecurityHeaders(app, securityConfig, logger);


    // For Proxy Servers
    string basePath = builder.Configuration["BasePath"];
    if (!string.IsNullOrEmpty(basePath))
    {
        app.Use(async (context, next) =>
        {
            context.Request.PathBase = basePath;
            await next.Invoke();
        });
    }

    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
        app.UseForwardedHeaders();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        app.UseStatusCodePagesWithReExecute("/Error/{0}");
        app.UseForwardedHeaders();
    }

    app.UseStaticFiles();

    app.UseRouting();

    app.UseCors();

    app.UseAuthentication();

    app.UseAuthorization();

    app.UseSession();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    app.Run();
}
catch (Exception ex)
{
    // NLog: catch setup errors
    logger.Error(ex, ex.Message);
    throw;
}
finally
{
    // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
    NLog.LogManager.Shutdown();
}

async Task ConfigureServices(WebApplicationBuilder builder)
{
    builder.Services.AddHttpClient();

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        // Only loopback proxies are allowed by default.
        // Clear that restriction because forwarders are enabled by explicit 
        // configuration.
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });
    var environment = builder.Environment;

    // Load secrets from Vault only in Staging or Production
    if (environment.IsStaging() || environment.IsProduction())
    {
        var vaultAddress = builder.Configuration["Vault:Address"];
        var vaultToken = builder.Configuration["Vault:Token"];
        var secretPath = builder.Configuration["Vault:SecretPath"];

        // Initialize Vault client
        var authMethod = new TokenAuthMethodInfo(vaultToken);
        var vaultClientSettings = new VaultClientSettings(vaultAddress, authMethod);
        var vaultClient = new VaultClient(vaultClientSettings);

        // Fetch secret data from Vault
        Secret<SecretData> secret = await vaultClient.V1.Secrets.KeyValue.V2.ReadSecretAsync(
            path: secretPath,
            mountPoint: "secret"
        );

        var data = secret.Data.Data;

        // Override configuration values
        var memoryConfig = new Dictionary<string, string>
        {
            ["ConnectionStrings:IDPConnString"] = data["ConnectionStrings:IDPConnString"]?.ToString(),
            ["ConnectionStrings:RAConnString"] = data["ConnectionStrings:RAConnString"]?.ToString(),
            ["ConnectionStrings:PKIConnString"] = data["ConnectionStrings:PKIConnString"]?.ToString(),
            ["RedisConnString"] = data["RedisConnString"]?.ToString(),
            ["JWTConfig"] = data["JWTConfig"]?.ToString(),
        };

        // Inject Vault secrets into configuration
        builder.Configuration.AddInMemoryCollection(memoryConfig);
    }
    else
    {
        Console.WriteLine("Skipping Vault secrets loading (Development environment).");
    }
    // Now you can access them like normal config values
    var idpConnectionString = builder.Configuration.GetConnectionString("IDPConnString");
    var raConnectionString = builder.Configuration.GetConnectionString("RAConnString");
    var pkiConnectionString = builder.Configuration.GetConnectionString("PKIConnString");
    var redisConn = builder.Configuration["RedisConnString"];
    var jwtConfig = builder.Configuration["JWTConfig"];


    //logger.Info("IDP Database ConnString: " + idpConnectionString);
    //logger.Info("RA Database ConnString: " + raConnectionString);
    //logger.Info("PKI Database ConnString: " + pkiConnectionString);
    //logger.Info("Redis Database ConnString: " + redisConn);

    if (String.Equals(builder.Configuration["DataPersistenceRequired"], "True"))
    {
        Console.WriteLine(builder.Configuration["RedisConnString"]);

        string configString = builder.Configuration["RedisConnString"];

        if (builder.Configuration.GetValue<bool>("EncryptionEnabled"))
        {
            logger.Info("Decrypt Text Started");
            configString = PKIMethods.Instance.
                    PKIDecryptSecureWireData(configString);
            logger.Info("Decrypt Text completed : :" + configString);
        }
        ;


        var redis = ConnectionMultiplexer.Connect(configString);
        builder.Services.AddDataProtection()
            .PersistKeysToStackExchangeRedis(redis, "DataProtection-Keys");
        Console.WriteLine("Redis connection succedded");
    }

    // Get JWT Token Configuration
    //var jwtConfig = builder.Configuration.GetSection("JWTConfig").Get<JWTConfig>();
    var jwtConfigDeserialized = JsonConvert.DeserializeObject<JWTConfig>(jwtConfig);
    builder.Services.AddSingleton(jwtConfigDeserialized);

    builder.Services.AddCors();
    builder.Services.AddTransient<ICorsPolicyProvider, CustomCorsPolicyProvider>();
    Console.WriteLine("Initialized started");

    //var serviceProvider = builder.Services.BuildServiceProvider();
    //var logger = serviceProvider.GetService<ILogger<UnitOfWork>>();
    //builder.Services.AddSingleton(typeof(ILogger), logger);
    builder.Services.AddScoped<Microsoft.Extensions.Logging.ILogger, Microsoft.Extensions.Logging.Logger<UnitOfWork>>();

    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddSingleton<ILogClient, LogClient>();
    builder.Services.AddScoped<ILocalJWTManager, LocalJWTManager>();
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
       .AddCookie(Config =>
       {
           var SessionName = builder.Configuration["IDPSessionName"].ToString();
           if (string.IsNullOrEmpty(SessionName))
               SessionName = "IDPSession";
           Config.Cookie.Name = SessionName;
       });
    builder.Services.AddAuthorization();
    builder.Services.AddFido2(options =>
    {
        options.ServerDomain = builder.Configuration["fido2:serverDomain"];
        options.ServerName = builder.Configuration["fido2:serverName"];
        options.Origin = builder.Configuration["fido2:origin"];
        options.TimestampDriftTolerance = builder.Configuration.GetValue<int>("fido2:timestampDriftTolerance");
    });

    builder.Services.AddSession();
    builder.Services.AddScoped<IPushNotificationClient, PushNotificationClient>();
    builder.Services.AddScoped<ITokenManager, TokenManager>();
    builder.Services.AddScoped<IPKIServiceClient, PKIServiceClient>();
    builder.Services.AddScoped<IRAServiceClient, RAServiceClient>();

    Console.WriteLine("Database initialization started");


    if (builder.Configuration.GetValue<bool>("EncryptionEnabled"))
    {
        try
        {
            logger.Info("Decrypt Text Started");
            idpConnectionString = PKIMethods.Instance.
                    PKIDecryptSecureWireData(idpConnectionString);
            logger.Info("WebApplication build successful");
            logger.Info("Decrypt Text completed : :" + idpConnectionString);
            raConnectionString = PKIMethods.Instance.
                    PKIDecryptSecureWireData(raConnectionString);
            pkiConnectionString = PKIMethods.Instance.
                    PKIDecryptSecureWireData(pkiConnectionString);
        }
        catch (Exception ex)
        {
            logger.Error("Decrypt Text Error : :" + ex.Message);
        }
    }

    builder.Services.AddDbContext<idp_dtplatformContext>(options =>
        options.UseNpgsql(idpConnectionString));

    builder.Services.AddDbContext<ra_0_2Context>(options =>
        options.UseNpgsql(raConnectionString));

    logger.Info("Database initialization success");
    Console.WriteLine("Database initialization success");
    //builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
    builder.Services.AddScoped<ICacheClient, CacheClient>();
    builder.Services.AddScoped<IEmailSender, EmailSender>();
    builder.Services.AddScoped<IIpRestriction, IPRrestriction>();
    builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
    builder.Services.AddScoped<IUserConsentService, UserConsentService>();
    builder.Services.AddScoped<ITokenManagerService, TokenManagerService>();
    builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
    builder.Services.AddScoped<IGlobalConfiguration, GlobalConfiguration>();
    builder.Services.AddSingleton<IKafkaConfigProvider, KafkaConfigProvider>();
    builder.Services.AddScoped<IClientService, ClientService>();
    builder.Services.AddScoped<IUserManagementService, UserManagementService>();
    builder.Services.AddScoped<CustomAuthorizationAttribute>();
    builder.Services.AddScoped<IUserInfoService, UserInfoService>();
    builder.Services.AddScoped<IAssertionValidationClient, AssertionValidationClient>();
    builder.Services.AddScoped<IPKILibrary, PKILibrary>();
    builder.Services.AddScoped<ICertificateService, CertificateService>();
    builder.Services.AddScoped<IAssertionValidationClient, AssertionValidationClient>();
    builder.Services.AddScoped<IRoleManagementService, RoleManagementService>();
    builder.Services.AddScoped<IHelper, Helper>();
    builder.Services.AddScoped<IPushNotificationService, PushNotificationService>();
    builder.Services.AddScoped<ISubscriberService, SubscriberService>();
    builder.Services.AddScoped<IGoogleMapService, GoogleMapService>();
    builder.Services.AddScoped<IClientsPurposeService, ClientsPurposeService>();
    builder.Services.AddScoped<IPurposeService, PurposeService>();
    builder.Services.AddScoped<ITransactionProfileRequestService, TransactionProfileRequestService>();
    builder.Services.AddScoped<ITransactionProfileConsentService, TransactionProfileConsentService>();
    builder.Services.AddScoped<ITransactionProfileStatusService, TransactionProfileStatusService>();
    builder.Services.AddScoped<ICategoryService, CategoryService>();
    builder.Services.AddScoped<IUserClaimService, UserClaimService>();
    builder.Services.AddScoped<IScopeService, ScopeService>();
    builder.Services.AddScoped<IAttributeServiceTransactionsService, AttributeServiceTransactionsService>();
    builder.Services.AddScoped<IUserProfileService, UserProfileService>();
    builder.Services.AddScoped<IEConsentService, EConsentService>();
    builder.Services.AddScoped<IUserProfilesConsentService, UserProfilesConsentService>();
    builder.Services.AddScoped<IUserConsoleService, UserConsoleService>();
    builder.Services.AddScoped<IAuthSchemeSevice, AuthSchemeService>();
    builder.Services.AddScoped<ISDKAuthenticationService, SDKAuthenticationService>();
    builder.Services.AddScoped<IMobileAuthenticationService, MobileAuthenticationService>();
    builder.Services.AddScoped<ICredentialService, CredentialService>();
    builder.Services.AddScoped<IOrganizationService, DTPortal.Core.Services.OrganizationService>();
    builder.Services.AddScoped<ISelfServiceConfigurationService, SelfServiceConfigurationService>();
    builder.Services.AddScoped<IWalletConfigurationService, WalletConfigurationService>();
    builder.Services.AddScoped<IUserDataService, UserDataService>();
    builder.Services.AddScoped<ICertificateIssuanceService, CertificateIssuanceService>();
    builder.Services.AddScoped<ILogReportService, LogReportService>();
    builder.Services.AddScoped<Helper>();
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddScoped<IMessageLocalizer, MessageLocalizer>();



    Console.WriteLine("****Initialization success****");

    builder.Services.AddControllersWithViews().AddNewtonsoftJson(options =>
               options.SerializerSettings.ReferenceLoopHandling =
               Newtonsoft.Json.ReferenceLoopHandling.Ignore
           );

    // Initialize Monitor with full config
    DTPortal.Core.Utilities.Monitor.Initialize(builder.Configuration, logger);
}