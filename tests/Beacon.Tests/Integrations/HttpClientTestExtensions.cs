using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using Beacon.Contracts.Applications;
using Beacon.Contracts.Documents;

namespace Beacon.Tests.Integrations;

internal static class HttpClientTestExtensions
{
    public static async Task<HttpResponseMessage> UploadDocumentAsync(
        this HttpClient client, string fileName, string text,
        string kind = "Resume", Guid? applicationId = null, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(text));
        file.Headers.ContentType = new("text/plain");
        form.Add(file, "file", fileName);
        form.Add(new StringContent(kind), "kind");
        if (applicationId is { } id)
            form.Add(new StringContent(id.ToString()), "applicationId");

        return await client.PostAsync("/api/documents", form, ct);
    }

    // Upload, expect 202, then poll until Ready/Failed
    public static async Task<DocumentResponse> UploadAndWaitAsync(
        this HttpClient client, string fileName, string text,
        string kind = "Resume", Guid? applicationId = null, CancellationToken ct = default)
    {
        var response = await client.UploadDocumentAsync(fileName, text, kind, applicationId, ct);
        Assert.Equal(HttpStatusCode.Accepted, response.StatusCode);
        var accepted = (await response.Content.ReadFromJsonAsync<DocumentResponse>(ct));
        return await client.WaitUntilFinishedAsync(accepted.Id, ct);
    }

    public static async Task<DocumentResponse> WaitUntilFinishedAsync(
        this HttpClient client, Guid id, CancellationToken ct = default)
    {
        var timer = Stopwatch.StartNew();
        while (true)
        {
            var doc = await client.GetFromJsonAsync<DocumentResponse>($"/api/documents/{id}", ct);
            if (doc!.Status is "Ready" or "Failed") return doc;
            if (timer.Elapsed > TimeSpan.FromSeconds(10))
                throw new TimeoutException($"Document {id} still '{doc.Status}' after 10s.");
            await Task.Delay(100, ct);
        }
    }

    public static async Task<ApplicationResponse> CreateApplicationAsync(
        this HttpClient client, string company, CancellationToken ct = default)
    {
        var response = await client.PostAsJsonAsync("/api/applications",
            new CreateApplicationRequest { Company = company, Role = "Engineer" }, ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ApplicationResponse>(ct))!;
    }
}