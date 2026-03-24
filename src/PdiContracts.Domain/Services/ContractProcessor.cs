using System.Text.Json;
using Microsoft.Extensions.Logging;
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
                "process-contract-specifications",
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

        // 3) Faz a varredura das CSs para montar resumo e disparar notificação.
        var summaryByAssetHolder = new Dictionary<string, AssetHolderGroup>();
        foreach (var specification in allSpecifications)
        {
            var holder = specification.OriginalAssetHolder;
            var isAssetHolderActive = assetHolderIsActive.TryGetValue(holder, out var holderActive) && holderActive;
            var isSpecificationActive = specificationIsActive[specification];

            summary.TotalContractSpecifications++;

            if (!summaryByAssetHolder.TryGetValue(holder, out var group))
            {
                group = new AssetHolderGroup
                {
                    OriginalAssetHolder = holder,
                    Count = 0,
                    NotifiedCount = 0
                };

                summaryByAssetHolder[holder] = group;
            }

            group.Count++;

            if (isAssetHolderActive && isSpecificationActive)
            {
                summary.TotalNotifiedContractSpecifications++;
                group.NotifiedCount++;
                await NotifyAssetHolderAsync(contract, specification);
            }
        }

        foreach (var group in summaryByAssetHolder.Values.OrderBy(g => g.OriginalAssetHolder))
        {
            summary.SpecificationsByAssetHolder.Add(group);
        }

        return processedContract;
    }

    /// <summary>
    /// Notifica a credenciadora sobre uma especificação de contrato processada
    /// </summary>
    private Task NotifyAssetHolderAsync(Contract contract, ContractSpecification specification)
    {
        // TODO: Implementar chamada real ao serviço de notificação da credenciadora

        var message =
            $"Notificando asset holder para especificação de contrato: Reference={contract.Reference}, OriginalAssetHolder={specification.OriginalAssetHolder}, PaymentScheme={specification.PaymentScheme}";

        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;

        return Task.CompletedTask;
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
}

