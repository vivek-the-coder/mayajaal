using Microsoft.Extensions.DependencyInjection;
using MayaJaal.Domain.Engines;
using MayaJaal.Domain.Services;
using MayaJaal.Infrastructure.Cryptography;
using MayaJaal.Infrastructure.Services;
using MayaJaal.Infrastructure.Storage;
using MayaJaal.Shared.Contracts;

namespace MayaJaal.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers domain engines and infrastructure services used by Guardian and SecurityCenter.
    /// </summary>
    public static IServiceCollection AddMayaJaalCore(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<HashChain>();
        services.AddSingleton<IEncryptionProvider, AesGcmEncryption>();
        services.AddSingleton<Argon2Kdf>();
        services.AddSingleton<IKeyManager, KeyManager>();

        services.AddSingleton<IRiskEngine, RiskEngine>();
        services.AddSingleton<IConfidenceEngine, ConfidenceEngine>();
        services.AddSingleton<ICorrelationEngine, CorrelationEngine>();
        services.AddSingleton<IPolicyEngine, PolicyEngine>();
        services.AddSingleton<ISafetyGate, SafetyGate>();

        services.AddSingleton<IEventStore, EventStore>();
        services.AddSingleton<IVaultService, VaultService>();
        services.AddSingleton<IIncidentService, IncidentService>();
        services.AddSingleton<IAssetProtectionService, AssetProtectionService>();
        services.AddSingleton<IHoneyFileService, HoneyFileService>();
        services.AddSingleton<Reporting.ReportGenerator>();

        return services;
    }
}
