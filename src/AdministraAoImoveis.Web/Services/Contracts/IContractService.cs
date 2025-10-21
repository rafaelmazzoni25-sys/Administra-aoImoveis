using System.IO;
using AdministraAoImoveis.Web.Domain.Entities;

namespace AdministraAoImoveis.Web.Services.Contracts;

public interface IContractService
{
    Task<ContractOperationResult> GenerateAsync(ContractGenerationRequest request, CancellationToken cancellationToken = default);
    Task<ContractOperationResult> ActivateAsync(ContractActivationRequest request, CancellationToken cancellationToken = default);
    Task<ContractOperationResult> AttachDocumentAsync(ContractAttachmentRequest request, CancellationToken cancellationToken = default);
    Task<ContractOperationResult> CloseAsync(ContractClosureRequest request, CancellationToken cancellationToken = default);
}

public sealed record ContractGenerationRequest(
    Guid PropertyId,
    Guid NegotiationId,
    DateTime DataInicio,
    DateTime? DataFim,
    decimal ValorAluguel,
    decimal Encargos,
    string Usuario,
    string Ip,
    string Host);

public sealed record ContractActivationRequest(
    Guid ContractId,
    string Usuario,
    string Ip,
    string Host);

public sealed record ContractAttachmentRequest(
    Guid ContractId,
    string FileName,
    string ContentType,
    Stream Content,
    string Usuario,
    string Ip,
    string Host);

public sealed record ContractClosureRequest(
    Guid ContractId,
    DateTime DataEncerramento,
    string? Observacoes,
    string Usuario,
    string Ip,
    string Host);

public sealed record ContractOperationResult(
    bool Success,
    Contract? Contract,
    PropertyDocument? Document,
    string? ErrorMessage)
{
    public static ContractOperationResult SuccessResult(Contract contract, PropertyDocument? document = null) =>
        new(true, contract, document, null);

    public static ContractOperationResult Failure(string message) =>
        new(false, null, null, message);
}
