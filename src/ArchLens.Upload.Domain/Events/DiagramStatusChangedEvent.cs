using ArchLens.SharedKernel.Domain;

namespace ArchLens.Upload.Domain.Events;

public sealed record DiagramStatusChangedEvent(
    Guid DiagramId,
    string OldStatus,
    string NewStatus) : DomainEvent;
