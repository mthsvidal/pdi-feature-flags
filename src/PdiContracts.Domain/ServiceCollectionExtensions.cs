using Microsoft.Extensions.DependencyInjection;
using PdiContracts.Domain.Services;

namespace PdiContracts.Domain;

/// <summary>
/// Metodos de extensao para configurar o client de feature flags.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adiciona o client Flipt ao container de injecao de dependencia (via API REST).
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="settings">Configuracoes de integracao</param>
    /// <returns>Service collection para encadeamento</returns>
    public static IServiceCollection AddFliptClient(this IServiceCollection services, FliptSettings settings)
    {
        if (settings == null)
            throw new ArgumentNullException(nameof(settings));

        // Registrar settings como singleton
        services.AddSingleton(settings);

        // Registrar client REST para avaliacao de flags.
        services.AddScoped<IFliptClient, FliptClient>();

        // Registrar FeatureFlagService
        services.AddScoped<IFeatureFlagService, FeatureFlagService>();

        // Registrar ContractProcessor para processamento e validação de contratos
        services.AddScoped<IContractProcessor, ContractProcessor>();

        return services;
    }

    /// <summary>
    /// Adiciona o client Flipt ao container de injecao de dependencia usando configuracao.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureSettings">Action para configurar as settings</param>
    /// <returns>Service collection para encadeamento</returns>
    public static IServiceCollection AddFliptClient(this IServiceCollection services, Action<FliptSettings> configureSettings)
    {
        if (configureSettings == null)
            throw new ArgumentNullException(nameof(configureSettings));

        var settings = new FliptSettings();
        configureSettings(settings);

        return services.AddFliptClient(settings);
    }
}
