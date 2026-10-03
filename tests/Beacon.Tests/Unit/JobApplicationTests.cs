using Beacon.Domain;
using Beacon.Domain.Applications;
using static Beacon.Domain.Applications.ApplicationStatus;

namespace Beacon.Tests.Unit;

public sealed class JobApplicationTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);

    private static readonly Dictionary<ApplicationStatus, ApplicationStatus[]> PathTo = new()
    {
        [Saved] = [],
        [Applied] = [Applied],
        [Interviewing] = [Applied, Interviewing],
        [Offer] = [Applied, Interviewing, Offer],
        [Rejected] = [Applied, Rejected],
        [Withdrawn] = [Withdrawn],
    };

    private static JobApplication At(ApplicationStatus status)
    {
        var app = JobApplication.Create("Contoso", "Engineer", null, null, Now);
        foreach (var step in PathTo[status])
            app.ChangeStatus(step, Now);
        return app;
    }

    [Theory]
    [InlineData(Saved, Applied)]
    [InlineData(Saved, Withdrawn)]
    [InlineData(Applied, Interviewing)]
    [InlineData(Applied, Rejected)]
    [InlineData(Applied, Withdrawn)]
    [InlineData(Interviewing, Offer)]
    [InlineData(Interviewing, Rejected)]
    [InlineData(Interviewing, Withdrawn)]
    [InlineData(Offer, Withdrawn)]
    public void Allowed_moves_succeed(ApplicationStatus from, ApplicationStatus to)
    {
        var app = At(from);
        app.ChangeStatus(to, Now);
        Assert.Equal(to, app.Status);
    }

    [Theory]
    [InlineData(Saved, Offer)]
    [InlineData(Applied, Saved)]
    [InlineData(Offer, Interviewing)]
    [InlineData(Interviewing, Applied)]
    [InlineData(Rejected, Offer)]
    [InlineData(Withdrawn, Saved)]
    public void Illegal_moves_throw(ApplicationStatus from, ApplicationStatus to)
    {
        var app = At(from);

        Assert.Throws<DomainException>(() => app.ChangeStatus(to, Now));
    }

    [Fact]
    public async Task Moving_to_the_same_status_throws()
    {
        var app = At(Applied);
        Assert.Throws<DomainException>(() => app.ChangeStatus(Applied, Now));
    }

    [Fact]
    public void ChangeStatus_updates_updatedAt()
    {
        var app = At(Saved);
        var later = Now.AddHours(1);

        app.ChangeStatus(Applied, later);

        Assert.Equal(later, app.UpdatedAt);
        Assert.Equal(Now, app.CreatedAt);
    }
}