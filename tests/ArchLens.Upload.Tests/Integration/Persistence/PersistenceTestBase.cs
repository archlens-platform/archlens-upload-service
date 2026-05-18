using ArchLens.Upload.Infrastructure.Persistence.EFCore.Context;
using Microsoft.EntityFrameworkCore;

namespace ArchLens.Upload.Tests.Integration.Persistence;

public abstract class PersistenceTestBase : IDisposable
{
    protected readonly UploadDbContext Context;

    protected PersistenceTestBase()
    {
        var options = new DbContextOptionsBuilder<UploadDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        Context = new UploadDbContext(options);
    }

    public void Dispose()
    {
        Context.Dispose();
        GC.SuppressFinalize(this);
    }
}
