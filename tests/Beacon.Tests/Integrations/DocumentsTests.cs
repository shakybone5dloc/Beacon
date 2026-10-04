using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Beacon.Contracts.Applications;
using Beacon.Contracts.Documents;

namespace Beacon.Tests.Integrations;

public sealed class DocumentsTests(BeaconApiFactory factory) : IClassFixture<BeaconApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Upload_is_accepted_then_processed_to_ready()
    {
        var ct = TestContext.Current.CancellationToken;
        var text = new string('x', 2500);

        var response = await UploadAsync("resume.md", text, ct: ct);

        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var accepted = await response.Content.ReadFromJsonAsync<DocumentResponse>(ct);
        Assert.NotNull(accepted);
        Assert.Equal("Pending", accepted.Status);
        Assert.Equal($"/api/documents/{accepted.Id}", response.Headers.Location?.ToString());

        var finished = await WaitUntilFinishedAsync(accepted.Id, ct);

        Assert.Equal("Ready", finished.Status);
        Assert.Equal(3, finished.ChunkCount);
    }

    [Fact]
    public async Task Pdf_is_rejected_with_400()
    {
        var ct = TestContext.Current.CancellationToken;
        var text = new string('x', 2500);

        var response = await UploadAsync("resume.pdf", text, ct: ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task File_over_2MB_is_rejected_with_400()
    {
        var ct = TestContext.Current.CancellationToken;
        var text = new string('x', 2100000);

        var response = await UploadAsync("resume.md", text, ct: ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_application_is_rejected_with_400()
    {
        var ct = TestContext.Current.CancellationToken;
        var text = new string('x', 2500);

        var response = await UploadAsync("resume.md", text, "Resume", Guid.NewGuid(), ct: ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Whitespace_only_file_fails_processing()
    {
        var ct = TestContext.Current.CancellationToken;
        var text = "   ";

        var response = await UploadAsync("resume.md", text, ct: ct);
        var document = await response.Content.ReadFromJsonAsync<DocumentResponse>(ct);
        Assert.NotNull(document);
        var finished = await WaitUntilFinishedAsync(document.Id, ct);
        Assert.Equal("Failed", finished.Status);
        Assert.Equal("Document contains no text", finished.Error);
    }

    [Fact]
    public async Task Documents_are_listed_for_their_application()
    {
        var ct = TestContext.Current.CancellationToken;
        var appResponse = await _client.PostAsJsonAsync("/api/applications",
            new CreateApplicationRequest { Company = "Contoso", Role = "Engineer" }, ct);
        var app = (await appResponse.Content.ReadFromJsonAsync<ApplicationResponse>(ct))!;

        var text = new string('x', 2500);
        var docResponse = await UploadAsync("resume.md", text, "Resume", app.Id, ct: ct);
        var doc = (await docResponse.Content.ReadFromJsonAsync<DocumentResponse>(ct))!;

        var appDocResponse = await _client.GetAsync($"/api/applications/{app.Id}/documents", ct);
        var appDocs = (await appDocResponse.Content.ReadFromJsonAsync<IReadOnlyList<DocumentResponse>>(ct))!;

        Assert.NotNull(appDocs);
        var only = Assert.Single(appDocs);
        Assert.Equal(doc.Id, only.Id);
        Assert.Equal("resume.md", only.FileName);

    }


    private async Task<HttpResponseMessage> UploadAsync(
        string fileName, string text, string kind = "Resume", Guid? applicationId = null,
        CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();

        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(text));
        file.Headers.ContentType = new("text/plain");
        form.Add(file, "file", fileName);

        form.Add(new StringContent(kind), "kind");
        if (applicationId is { } id)
            form.Add(new StringContent(id.ToString()), "applicationId");

        return await _client.PostAsync("/api/documents", form, ct);
    }

    private async Task<DocumentResponse> WaitUntilFinishedAsync(Guid id, CancellationToken ct = default)
    {
        var timer = Stopwatch.StartNew();

        while (true)
        {
            var document = await _client.GetFromJsonAsync<DocumentResponse>($"/api/documents/{id}", ct);

            if (document!.Status is "Ready" or "Failed")
                return document;

            if (timer.Elapsed > TimeSpan.FromSeconds(10))
                throw new TimeoutException($"Document {id} still '{document.Status}' after 10s.");

            await Task.Delay(100, ct);
        }
    }
}