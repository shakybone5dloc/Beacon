namespace Beacon.Domain.Applications;

public static class ApplicationStatusTransitions
{
    private static readonly Dictionary<ApplicationStatus, ApplicationStatus[]> Allowed = new()
    {
        [ApplicationStatus.Saved] = [ApplicationStatus.Applied, ApplicationStatus.Withdrawn],
        [ApplicationStatus.Applied] = [ApplicationStatus.Interviewing, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn],
        [ApplicationStatus.Interviewing] = [ApplicationStatus.Offer, ApplicationStatus.Rejected, ApplicationStatus.Withdrawn],
        [ApplicationStatus.Offer] = [ApplicationStatus.Withdrawn],
        [ApplicationStatus.Rejected] = [],
        [ApplicationStatus.Withdrawn] = [],
    };

    public static bool CanMove(ApplicationStatus from, ApplicationStatus to) =>
        Allowed.TryGetValue(from, out var targets) && targets.Contains(to);
}