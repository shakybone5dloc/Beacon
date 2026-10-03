using Beacon.Application.Abstractions;
using Beacon.Contracts.Applications;
using Beacon.Domain.Applications;
using Microsoft.EntityFrameworkCore;

namespace Beacon.Application.Applications;

public sealed class ApplicationService(IBeaconDbContext db, TimeProvider time)
{
    public async Task<ApplicationResponse> CreateAsync(CreateApplicationRequest request, CancellationToken ct)
    {
        var application = JobApplication.Create(
            request.Company,
            request.Role,
            request.JobUrl,
            request.Notes,
            time.GetUtcNow());
        await db.JobApplications.AddAsync(application, ct);
        await db.SaveChangesAsync(ct);

        return application.ToResponse();

    }

    public async Task<ApplicationResponse?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var application = await db.JobApplications
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, ct);

        return application?.ToResponse();
    }

    public async Task<IReadOnlyList<ApplicationResponse>> ListAsync(CancellationToken ct)
    {
        var applications = await db.JobApplications
            .AsNoTracking()
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(ct);

        return applications.Select(a => a.ToResponse()).ToList();
    }

    public async Task<ApplicationResponse?> ChangeStatusAsync(
        Guid id, ApplicationStatus newStatus, uint expectedVersion, CancellationToken ct)
    {
        var application = await db.JobApplications.FirstOrDefaultAsync(a => a.Id == id, ct);
        if (application is null) return null;

        db.Entry(application).Property(a => a.Version).OriginalValue = expectedVersion;

        application.ChangeStatus(newStatus, time.GetUtcNow());

        await db.SaveChangesAsync(ct);
        return application.ToResponse();
    }
}