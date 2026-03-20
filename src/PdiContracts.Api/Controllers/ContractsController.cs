using Microsoft.AspNetCore.Mvc;
using PdiContracts.Api.Models;
using PdiContracts.Api.Mappers;
using PdiContracts.Domain.Models;
using PdiContracts.Domain.Services;

namespace PdiContracts.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContractsController : ControllerBase
{
    private readonly ILogger<ContractsController> _logger;
    private readonly IContractProcessor _contractProcessor;
    private readonly IFeatureFlagService _featureFlagService;

    public ContractsController(
        ILogger<ContractsController> logger,
        IContractProcessor contractProcessor,
        IFeatureFlagService featureFlagService)
    {
        _logger = logger;
        _contractProcessor = contractProcessor;
        _featureFlagService = featureFlagService;
    }

    /// <summary>
    /// Endpoint para testar a feature flag
    /// </summary>
    [HttpGet("test-feature-flag/{assetHolder}")]
    public async Task<IActionResult> TestFeatureFlag(string assetHolder)
    {
        var contextJson = System.Text.Json.JsonSerializer.Serialize(
            new
            {
                entityId = $"test|{assetHolder}",
                originalAssetHolder = assetHolder
            }
        );
        
        var isEnabled = await _featureFlagService.IsEnabledAsync(
            "process-contract-specifications",
            contextJson,
            defaultValue: false
        );

        _logger.LogInformation(
            "Feature flag test: AssetHolder={AssetHolder}, IsEnabled={IsEnabled}",
            assetHolder,
            isEnabled
        );

        return Ok(new 
        { 
            featureName = "process-contract-specifications",
            assetHolder = assetHolder,
            isEnabled = isEnabled,
            context = contextJson
        });
    }

    /// <summary>
    /// Endpoint para receber contratos com garantia de idempotência
    /// </summary>
    /// <param name="request">Dados do contrato com chave de idempotência</param>
    /// <returns>Resposta confirmando o recebimento do contrato</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ContractResponseDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessContract([FromBody] ContractRequestDto request)
    {
        try
        {
            _logger.LogInformation(
                "IdempotencyKey: {IdempotencyKey}",
                request.IdempotencyKey
            );

            // Validações básicas
            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                return BadRequest(new { error = "IdempotencyKey é obrigatório" });
            }

            if (request.Contracts == null || !request.Contracts.Any())
            {
                return BadRequest(new { error = "Pelo menos um contrato deve ser fornecido" });
            }

            // Converte DTO para modelo de domain
            var domainRequest = ContractMapper.ToDomain(request);

            // Processa contratos com o serviço
            var processingResult = await _contractProcessor.ProcessAsync(domainRequest);

            if (!processingResult.Success)
            {
                _logger.LogError(
                    "Processamento falhou para IdempotencyKey: {IdempotencyKey}",
                    request.IdempotencyKey
                );
                return StatusCode(StatusCodes.Status202Accepted, new
                {
                    processingDetails = new
                    {
                        summary = new
                        {
                            totalContractSpecifications = 0,
                            totalNotifiedContractSpecifications = 0,
                            specificationsByAssetHolder = new List<object>()
                        }
                    }
                });
            }

            _logger.LogInformation(
                "Contratos processados com sucesso - IdempotencyKey: {IdempotencyKey}, Total: {Count}",
                request.IdempotencyKey,
                processingResult.Summary.TotalContractSpecifications
            );

            return StatusCode(StatusCodes.Status202Accepted, new
            {
                processingDetails = new
                {
                    summary = new
                    {
                        totalContractSpecifications = processingResult.Summary.TotalContractSpecifications,
                        totalNotifiedContractSpecifications = processingResult.Summary.TotalNotifiedContractSpecifications,
                        specificationsByAssetHolder = processingResult.Summary.SpecificationsByAssetHolder
                            .Select(g => new
                            {
                                g.OriginalAssetHolder,
                                g.Count,
                                g.NotifiedCount
                            })
                            .OrderBy(g => g.OriginalAssetHolder)
                            .ToList()
                    }
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar contrato");
            return StatusCode(500, new { error = "Erro interno ao processar contrato", details = ex.Message });
        }
    }
}
