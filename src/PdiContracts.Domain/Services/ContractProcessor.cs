using System.Text.Json;
using Microsoft.Extensions.Logging;
using PdiContracts.Domain.Constants;
using PdiContracts.Domain.Models;

namespace PdiContracts.Domain.Services;

/// <summary>
/// Serviço para processamento e validação de contratos
/// </summary>
public class ContractProcessor : IContractProcessor
{
    private readonly ILogger<ContractProcessor> _logger;
    private readonly IFeatureFlagService _featureFlagService;

    public ContractProcessor(ILogger<ContractProcessor> logger, IFeatureFlagService featureFlagService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _featureFlagService = featureFlagService ?? throw new ArgumentNullException(nameof(featureFlagService));
    }

    /// <inheritdoc/>
    public async Task<ContractProcessingResult> ProcessAsync(ContractRequest request)
    {
        var result = new ContractProcessingResult
        {
            ProcessedContract = new ContractRequest
            {
                IdempotencyKey = request.IdempotencyKey,
                Contracts = new List<Contract>()
            }
        };

        try
        {
                var processingSummary = new ProcessingSummary();
                var contracts = request.Contracts ?? new List<Contract>();

                // Processa cada contrato
                foreach (var contract in contracts)
                {
                    // Processa especificações com feature flag CS por CS
                    var processedContract = await ProcessContractSpecificationsAsync(contract, request, processingSummary);
                    result.ProcessedContract?.Contracts.Add(processedContract);
                }

                result.Summary = processingSummary;
                result.Success = true;
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Erro ao processar contratos para IdempotencyKey: {IdempotencyKey}",
                request.IdempotencyKey
            );

            result.Success = false;
            return result;
        }
    }

    /// <summary>
    /// Processa as especificações de um contrato, usando feature flag apenas para notificação
    /// </summary>
    private async Task<Contract> ProcessContractSpecificationsAsync(Contract contract, ContractRequest request, ProcessingSummary summary)
    {
        var processedContract = new Contract
        {
            Reference = contract.Reference,
            EffectType = contract.EffectType,
            BankAccount = contract.BankAccount,
            ContractSpecifications = contract.ContractSpecifications ?? new List<ContractSpecification>()
        };

        var allSpecifications = processedContract.ContractSpecifications;

        if (!allSpecifications.Any())
        {
            return processedContract;
        }

        // 1) Avalia cada CS e guarda o resultado de ativação por especificação.
        var specificationIsActive = new Dictionary<ContractSpecification, bool>();

        foreach (var specification in allSpecifications)
        {
            var contextJson = BuildEvaluationContextJson(request, contract, specification);
            var isActive = await _featureFlagService.IsEnabledAsync(
                FeatureFlagNames.ProcessContractSpecifications,
                contextJson,
                defaultValue: false
            );

            specificationIsActive[specification] = isActive;
        }

        // 2) Cria o dicionário: originalAssetHolder -> isActive.
        // Regra: o assetHolder é ativo se pelo menos uma CS dele estiver ativa.
        
        var assetHolderIsActive = allSpecifications
            .GroupBy(specification => specification.OriginalAssetHolder)
            .ToDictionary(
                group => group.Key,
                group => group.Any(specification => specificationIsActive[specification])
            );

        // 3) Agrupa CSs por holder e monta resumo, disparando notificação 1x por holder ativo.
        var specificationsByHolder = allSpecifications
            .GroupBy(s => s.OriginalAssetHolder)
            .ToDictionary(g => g.Key, g => g.ToList());

        var summaryByAssetHolder = new Dictionary<string, AssetHolderGroup>();
        foreach (var holderGroup in specificationsByHolder)
        {
            var holder = holderGroup.Key;
            var holderSpecs = holderGroup.Value;
            var isHolderActive = assetHolderIsActive.TryGetValue(holder, out var active) && active;

            var group = new AssetHolderGroup
            {
                OriginalAssetHolder = holder,
                Count = holderSpecs.Count,
                NotificationPath = null
            };

            summary.TotalContractSpecifications += group.Count;

            if (isHolderActive)
            {
                group.NotificationPath = await NotifyAssetHolderAsync(request, contract, holder, holderSpecs);
                summary.TotalNotifiedContractSpecifications += group.Count;
            }

            summaryByAssetHolder[holder] = group;
        }

        foreach (var group in summaryByAssetHolder.Values.OrderBy(g => g.OriginalAssetHolder))
        {
            summary.SpecificationsByAssetHolder.Add(group);
        }

        return processedContract;
    }

    /// <summary>
    /// Notifica a credenciadora armazenando o contrato filtrado apenas com CSs do originalAssetHolder ativo
    /// </summary>
    private async Task<string> NotifyAssetHolderAsync(
        ContractRequest request,
        Contract contract,
        string originalAssetHolder,
        List<ContractSpecification> holderSpecifications)
    {
        // Cria contrato filtrado com apenas as CSs do holder
        var contractForHolder = new Contract
        {
            Reference = contract.Reference,
            EffectType = contract.EffectType,
            BankAccount = contract.BankAccount,
            ContractSpecifications = holderSpecifications
        };

        var notificationsRoot = @"C:\temp";
        var holderFolder = SanitizePathSegment(originalAssetHolder);
        var idempotencyFolder = SanitizePathSegment(request.IdempotencyKey);
        var targetDirectory = Path.Combine(notificationsRoot, holderFolder, idempotencyFolder);

        Directory.CreateDirectory(targetDirectory);

        var fileName = $"{SanitizePathSegment(contract.Reference)}.json";
        var filePath = Path.Combine(targetDirectory, fileName);

        var payload = new
        {
            request.IdempotencyKey,
            ContractReference = contract.Reference,
            contract.EffectType,
            contract.BankAccount,
            OriginalAssetHolder = originalAssetHolder,
            ContractSpecifications = holderSpecifications,
            GeneratedAtUtc = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(filePath, json);

        _logger.LogInformation(
            "Contrato filtrado armazenado em {Path}. Reference={Reference}, OriginalAssetHolder={OriginalAssetHolder}, CsCount={Count}",
            filePath,
            contract.Reference,
            originalAssetHolder,
            holderSpecifications.Count);

        return filePath;
    }

    private static string BuildEvaluationContextJson(ContractRequest request, Contract contract, ContractSpecification specification)
    {
        var entityId = string.Join("|",
            request.IdempotencyKey,
            contract.Reference,
            specification.OriginalAssetHolder,
            specification.PaymentScheme);

        var context = new
        {
            entityId,
            idempotencyKey = request.IdempotencyKey,
            contractReference = contract.Reference,
            originalAssetHolder = specification.OriginalAssetHolder,
            paymentScheme = specification.PaymentScheme
        };

        return JsonSerializer.Serialize(context);
    }

    private static string SanitizePathSegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(ch => invalidChars.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
    }
}