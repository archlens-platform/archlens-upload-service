using ArchLens.SharedKernel.Domain;
using ArchLens.Upload.Domain.Entities.AnalysisProcessEntities;

namespace ArchLens.Upload.Domain.Interfaces.AnalysisProcessInterfaces;

public interface IAnalysisProcessRepository : IRepository<AnalysisProcess>
{
    Task<AnalysisProcess?> GetByDiagramIdAsync(Guid diagramId, CancellationToken cancellationToken = default);
}
