using Beacon.Contracts.Applications;
using Beacon.Web.Components.Pages;
using Beacon.Web.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Beacon.Web.Tests;

public sealed class BoardTests : BunitContext
{
    private readonly FakeBeaconApiClient _api = new();

    public BoardTests()
    {
        Services.AddSingleton<IBeaconApiClient>(_api);
    }

    private static ApplicationResponse App(string company, string status, params string[] allowed) =>
        new(Guid.NewGuid(), company, "Engineer", null, null, status,
            DateTimeOffset.UnixEpoch, DateTimeOffset.UnixEpoch, Version: 1, AllowedTransitions: allowed);

    [Fact]
    public void Cards_apear_in_the_column_matching_their_status()
    {
        _api.Applications = [App("Contoso", "Saved"), App("Fabrikam", "Applied")];

        var cut = Render<Board>();

        var columns = cut.FindAll("section.column");
        Assert.Equal(6, columns.Count);

        Assert.Contains("Contoso", columns[0].TextContent);
        Assert.Contains("Fabrikam", columns[1].TextContent);
        Assert.DoesNotContain("Contoso", columns[1].TextContent);
    }

    [Fact]
    public void Only_the_allowed_moves_are_offered_as_buttons()
    {
        _api.Applications = [App("Contoso", "Saved", "Applied", "Withdrawn")];

        var cut = Render<Board>();

        var buttons = cut.FindAll(".actions button").Select(b => b.TextContent.Trim()).ToList();
        Assert.Equal(2, buttons.Count);

        Assert.Equal(["Applied", "Withdrawn"], buttons);
        Assert.DoesNotContain("Saved", buttons);
    }

    [Fact]
    public void Final_states_show_no_buttons()
    {
        _api.Applications = [App("Contoso", "Rejected")];

        var cut = Render<Board>();

        var buttons = cut.FindAll(".actions button");

        Assert.Empty(buttons);
    }

    [Fact]
    public void A_conflict_shows_the_notice_and_reloads_the_board()
    {
        _api.Applications = [App("Contoso", "Saved", "Applied")];
        _api.NextMoveResult = new MoveResult(MoveOutcome.Conflict, Message: "changed elsewhere");

        var cut = Render<Board>();
        var loadsBefore = _api.GetApplicationsCalls;

        cut.Find(".actions button").Click();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("This application was changed somewhere else. The board has been refreshed.", cut.Find(".notice").TextContent);
            Assert.Equal((loadsBefore + 1), _api.GetApplicationsCalls);
        });
    }
}