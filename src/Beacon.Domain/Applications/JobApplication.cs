namespace Beacon.Domain.Applications;

public class JobApplication
{

    private JobApplication() { }

    public static JobApplication Create(string company, string role, string? jobUrl, string? notes, DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(company);
        ArgumentException.ThrowIfNullOrWhiteSpace(role);
       
        return new JobApplication
        {
            Id = Guid.CreateVersion7(),
            Company = company.Trim(),
            Role = role.Trim(),
            JobUrl = string.IsNullOrWhiteSpace(jobUrl) ? null : jobUrl.Trim(),
            Status = ApplicationStatus.Saved,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

    }

    public const int MaxNoteLength = 300;
    public Guid Id { get; private set; }
    public string Company { get; private set; } = null!;
    public string Role { get; private set; } = null!;
    public string? JobUrl { get; private set; }
    public string? Notes { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

}