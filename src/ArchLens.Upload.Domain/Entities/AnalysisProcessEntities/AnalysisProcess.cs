using ArchLens.SharedKernel.Domain;

namespace ArchLens.Upload.Domain.Entities.AnalysisProcessEntities;

public sealed class AnalysisProcess : Entity<Guid>
{
    public Guid DiagramUploadId { get; private set; }
    public string Status { get; private set; } = "Pending";
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public string? ErrorMessage { get; private set; }

    private AnalysisProcess() { }

    public static AnalysisProcess Create(Guid diagramUploadId)
    {
        return new AnalysisProcess
        {
            Id = Guid.NewGuid(),
            DiagramUploadId = diagramUploadId,
            Status = "Pending",
            StartedAt = DateTime.UtcNow
        };
    }

    public void MarkStarted()
    {
        Status = "Processing";
    }

    public void MarkCompleted()
    {
        Status = "Completed";
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkFailed(string errorMessage)
    {
        Status = "Failed";
        CompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }
}
