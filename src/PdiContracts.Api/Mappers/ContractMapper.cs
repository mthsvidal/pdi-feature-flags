using PdiContracts.Api.Models;
using DomainContract = PdiContracts.Domain.Models.Contract;
using DomainContractRequest = PdiContracts.Domain.Models.ContractRequest;
using DomainContractSpecification = PdiContracts.Domain.Models.ContractSpecification;
using DomainBankAccount = PdiContracts.Domain.Models.BankAccount;

namespace PdiContracts.Api.Mappers;

/// <summary>
/// Mapper para conversão de ContractRequestDto (API) para modelos de domínio
/// </summary>
public class ContractMapper
{
    /// <summary>
    /// Converte um ContractRequestDto para ContractRequest (Domain)
    /// </summary>
    public static DomainContractRequest ToDomain(ContractRequestDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new DomainContractRequest
        {
            IdempotencyKey = dto.IdempotencyKey,
            Contracts = dto.Contracts?
                .Select(ToDomainContract)
                .ToList() ?? new List<DomainContract>()
        };
    }

    /// <summary>
    /// Converte um ContractDto para Contract (Domain)
    /// </summary>
    public static DomainContract ToDomainContract(ContractDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new DomainContract
        {
            Reference = dto.Reference,
            ContractDueDate = dto.ContractDueDate,
            AssetHolderDocumentType = dto.AssetHolderDocumentType,
            AssetHolder = dto.AssetHolder,
            ContractUniqueIdentifier = dto.ContractUniqueIdentifier,
            SignatureDate = dto.SignatureDate,
            EffectType = dto.EffectType,
            WarrantyType = dto.WarrantyType,
            WarrantyAmount = dto.WarrantyAmount,
            BalanceDue = dto.BalanceDue,
            DivisionMethod = dto.DivisionMethod,
            EffectStrategy = dto.EffectStrategy,
            PercentageValue = dto.PercentageValue,
            BankAccount = ToDomainBankAccount(dto.BankAccount),
            ContractSpecifications = dto.ContractSpecifications?
                .Select(ToDomainSpecification)
                .ToList() ?? new List<DomainContractSpecification>()
        };
    }

    /// <summary>
    /// Converte um BankAccountDto para BankAccount (Domain)
    /// </summary>
    public static DomainBankAccount ToDomainBankAccount(BankAccountDto dto)
    {
        if (dto == null)
            return new DomainBankAccount();

        return new DomainBankAccount
        {
            Branch = dto.Branch,
            Account = dto.Account,
            AccountDigit = dto.AccountDigit,
            AccountType = dto.AccountType,
            Ispb = dto.Ispb,
            DocumentType = dto.DocumentType,
            DocumentNumber = dto.DocumentNumber
        };
    }

    /// <summary>
    /// Converte um ContractSpecificationDto para ContractSpecification (Domain)
    /// </summary>
    public static DomainContractSpecification ToDomainSpecification(ContractSpecificationDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        return new DomainContractSpecification
        {
            ExpectedSettlementDate = dto.ExpectedSettlementDate,
            OriginalAssetHolder = dto.OriginalAssetHolder,
            ReceivableDebtor = dto.ReceivableDebtor,
            PaymentScheme = dto.PaymentScheme,
            EffectValue = dto.EffectValue,
            InitialExpectedSettlementDate = dto.InitialExpectedSettlementDate,
            FinalExpectedSettlementDate = dto.FinalExpectedSettlementDate
        };
    }

    /// <summary>
    /// Converte um ContractRequest (Domain) para ContractRequestDto (API)
    /// </summary>
    public static ContractRequestDto ToDto(DomainContractRequest domain)
    {
        if (domain == null)
            throw new ArgumentNullException(nameof(domain));

        return new ContractRequestDto
        {
            IdempotencyKey = domain.IdempotencyKey,
            Contracts = domain.Contracts?
                .Select(ContractToDto)
                .ToList() ?? new List<ContractDto>()
        };
    }

    /// <summary>
    /// Converte um Contract (Domain) para ContractDto (API)
    /// </summary>
    public static ContractDto ContractToDto(DomainContract domain)
    {
        if (domain == null)
            throw new ArgumentNullException(nameof(domain));

        return new ContractDto
        {
            Reference = domain.Reference,
            ContractDueDate = domain.ContractDueDate,
            AssetHolderDocumentType = domain.AssetHolderDocumentType,
            AssetHolder = domain.AssetHolder,
            ContractUniqueIdentifier = domain.ContractUniqueIdentifier,
            SignatureDate = domain.SignatureDate,
            EffectType = domain.EffectType,
            WarrantyType = domain.WarrantyType,
            WarrantyAmount = domain.WarrantyAmount,
            BalanceDue = domain.BalanceDue,
            DivisionMethod = domain.DivisionMethod,
            EffectStrategy = domain.EffectStrategy,
            PercentageValue = domain.PercentageValue,
            BankAccount = BankAccountToDto(domain.BankAccount),
            ContractSpecifications = domain.ContractSpecifications?
                .Select(SpecificationToDto)
                .ToList() ?? new List<ContractSpecificationDto>()
        };
    }

    /// <summary>
    /// Converte um BankAccount (Domain) para BankAccountDto (API)
    /// </summary>
    public static BankAccountDto BankAccountToDto(DomainBankAccount domain)
    {
        if (domain == null)
            return new BankAccountDto();

        return new BankAccountDto
        {
            Branch = domain.Branch,
            Account = domain.Account,
            AccountDigit = domain.AccountDigit,
            AccountType = domain.AccountType,
            Ispb = domain.Ispb,
            DocumentType = domain.DocumentType,
            DocumentNumber = domain.DocumentNumber
        };
    }

    /// <summary>
    /// Converte um ContractSpecification (Domain) para ContractSpecificationDto (API)
    /// </summary>
    public static ContractSpecificationDto SpecificationToDto(DomainContractSpecification domain)
    {
        if (domain == null)
            throw new ArgumentNullException(nameof(domain));

        return new ContractSpecificationDto
        {
            ExpectedSettlementDate = domain.ExpectedSettlementDate,
            OriginalAssetHolder = domain.OriginalAssetHolder,
            ReceivableDebtor = domain.ReceivableDebtor,
            PaymentScheme = domain.PaymentScheme,
            EffectValue = domain.EffectValue,
            InitialExpectedSettlementDate = domain.InitialExpectedSettlementDate,
            FinalExpectedSettlementDate = domain.FinalExpectedSettlementDate
        };
    }
}
