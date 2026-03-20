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
        var allSpecifications = contract.ContractSpecifications ?? new List<ContractSpecification>();

        if (!allSpecifications.Any())
        {
            return new Contract
            {
                Reference = contract.Reference,
                EffectType = contract.EffectType,
                BankAccount = contract.BankAccount,
                ContractSpecifications = allSpecifications
            };
        }

        // Avalia a feature no Flipt para cada CS usando entityId + context.
        var notificationTasks = allSpecifications.Select(async specification =>
        {
            var contextJson = BuildEvaluationContextJson(request, contract, specification);
            var shouldNotify = await _featureFlagService.IsEnabledAsync(
                "process-contract-specifications",
                contextJson,
                defaultValue: false
            );

            return new { Specification = specification, ShouldNotify = shouldNotify };
        }).ToList();

        var results = await Task.WhenAll(notificationTasks);

        // Agrupa especificações por Asset Holder e conta as que serão notificadas
        var groupsByAssetHolder = allSpecifications
            .GroupBy(s => s.OriginalAssetHolder)
            .ToDictionary(g => g.Key, g => g.Count());

        var notifiedByAssetHolder = results
            .Where(r => r.ShouldNotify)
            .GroupBy(r => r.Specification.OriginalAssetHolder)
            .ToDictionary(g => g.Key, g => g.Count());

        // Adiciona os grupos ao resumo
        foreach (var kvp in groupsByAssetHolder)
        {
            summary.TotalContractSpecifications += kvp.Value;

            var notifiedCount = notifiedByAssetHolder.TryGetValue(kvp.Key, out var value)
                ? value
                : 0;

            summary.TotalNotifiedContractSpecifications += notifiedCount;

            var group = new AssetHolderGroup
            {
                OriginalAssetHolder = kvp.Key,
                Count = kvp.Value,
                NotifiedCount = notifiedCount
            };
            summary.SpecificationsByAssetHolder.Add(group);
        }

        // Notifica para as especificações habilitadas
        foreach (var result in results.Where(r => r.ShouldNotify))
        {
            await NotifyAssetHolderAsync(contract, result.Specification);
        }

        // Retorna contrato com TODAS as especificações
        return new Contract
        {
            Reference = contract.Reference,
            EffectType = contract.EffectType,
            BankAccount = contract.BankAccount,
            ContractSpecifications = allSpecifications
        };
    }

    /// <summary>
    /// Notifica a credenciadora sobre uma especificação de contrato processada
    /// </summary>
    private async Task NotifyAssetHolderAsync(Contract contract, ContractSpecification specification)
    {
        // TODO: Implementar chamada real ao serviço de notificação da credenciadora

        var message =
            $"Notificando asset holder para especificação de contrato: Reference={contract.Reference}, OriginalAssetHolder={specification.OriginalAssetHolder}, PaymentScheme={specification.PaymentScheme}";

        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;

        await Task.CompletedTask;
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

