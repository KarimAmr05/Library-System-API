namespace LibrarySystem.Business.Messaging;

/// <summary>
/// Message contract published to RabbitMQ when a borrowing request is submitted.
/// Kept intentionally minimal: the request row is already persisted, so the
/// consumer only needs the identifier and can re-validate state safely,
/// making duplicate deliveries idempotent.
/// </summary>
/// <param name="RequestId">Identifier of the persisted pending borrowing request.</param>
public sealed record BorrowRequestMessage(Guid RequestId);
