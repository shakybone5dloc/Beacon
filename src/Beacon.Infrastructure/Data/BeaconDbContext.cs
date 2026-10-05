using Microsoft.EntityFrameworkCore;
using Beacon.Application.Abstractions;
using Beacon.Domain.Applications;
using Beacon.Domain.Documents;

namespace Beacon.Infrastructure.Data;

public class BeaconDbContext(DbContextOptions<BeaconDbContext> options) : DbContext(options), IBeaconDbContext
{
    public DbSet<JobApplication> JobApplications => Set<JobApplication>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(BeaconDbContext).Assembly);
    }
}