using System.Net;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Beacon.Contracts.Applications;
using Beacon.Contracts.Documents;
using Beacon.Contracts.Ask;
using Microsoft.AspNetCore.Mvc;

namespace Beacon.Web.Services;

public sealed class BeaconApiClient(HttpClient http) : IBeaconApiClient
{
    public async Task<IReadOnlyList<ApplicationResponse>> GetApplicationsAsync(CancellationToken ct = default)
    {
        return await http.GetFromJsonAsync<List<ApplicationResponse>>("api/applications", ct) ?? [];
    }

    public async Task<MoveResult> ChangeStatusAsync(Guid id, string status, uint version, CancellationToken ct = default)
    {
        var response = await http.PatchAsJsonAsync(
            $"api/applications/{id}/status",
            new ChangeStatusRequest { Status = status, Version = version },
            ct);

        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
                var updated = await response.Content.ReadFromJsonAsync<ApplicationResponse>(ct);
                return new MoveResult(MoveOutcome.Moved, updated);

            case HttpStatusCode.Conflict:
                var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(ct);
                return new MoveResult(MoveOutcome.Conflict, Message: problem?.Detail);

            case HttpStatusCode.NotFound:
                return new MoveResult(MoveOutcome.NotFound, Message: "This application no longer exists.");

            default:
                response.EnsureSuccessStatusCode();
                throw new InvalidOperationException($"Unexpected status {response.StatusCode}");
        }
    }

    public async Task<CreateResult> CreateApplicationAsync(CreateApplicationRequest request, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/applications", request, ct);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(ct);
            return new CreateResult(null, problem?.Errors ?? new Dictionary<string, string[]>());
        }

        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<ApplicationResponse>(ct);
        
        return new CreateResult(created, null);
    }

    public async Task<ApplicationResponse?> GetApplicationAsync(Guid id, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/applications/{id}", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ApplicationResponse>(ct);
    }

    public async Task<IReadOnlyList<DocumentResponse>> GetDocumentsAsync(Guid applicationId, CancellationToken ct = default)
    {
        var response = await http.GetAsync($"api/applications/{applicationId}/documents", ct);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<DocumentResponse>>(ct) ?? [];
    }

    public async Task<DocumentUploadResult> UploadDocumentAsync(
        Stream content, string fileName, string kind, Guid applicationId, CancellationToken ct = default)
    {
        using var form = new MultipartFormDataContent();

        var file = new StreamContent(content);
        file.Headers.ContentType = new("text/plain");
        form.Add(file, "file", fileName);
        form.Add(new StringContent(kind), "kind");
        form.Add(new StringContent(applicationId.ToString()), "applicationId");

        var response = await http.PostAsync("api/documents", form, ct);

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>(ct);
            var message = problem is null
                ? "The upload was rejected."
                : string.Join(" ", problem.Errors.SelectMany(e => e.Value));
            return new DocumentUploadResult(null, message);
        }

        response.EnsureSuccessStatusCode();
        return new DocumentUploadResult(await response.Content.ReadFromJsonAsync<DocumentResponse>(ct), null);
    }

    private static readonly JsonSerializerOptions Json = JsonSerializerOptions.Web;

    public async IAsyncEnumerable<AskEvent> AskAsync(
        string question, Guid? applicationId, [EnumeratorCancellation] CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/ask")
        {
            Content = JsonContent.Create(new AskRequest { Question = question, ApplicationId = applicationId })
        };

        using var response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            yield return new ErrorEvent("You're asking questions too quickly. Please wait and try again.");
            yield return new DoneEvent(Grounded: false);
            yield break;
        }

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            yield return new ErrorEvent("Please enter a question (up to 1,000 characters).");
            yield return new DoneEvent(Grounded: false);
            yield break;
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(ct);

        await foreach (var item in SseParser.Create(stream).EnumerateAsync(ct))
        {
            AskEvent? evt = item.EventType switch
            {
                "sources" => JsonSerializer.Deserialize<SourcesEvent>(item.Data, Json),
                "token" => JsonSerializer.Deserialize<TokenEvent>(item.Data, Json),
                "error" => JsonSerializer.Deserialize<ErrorEvent>(item.Data, Json),
                "done" => JsonSerializer.Deserialize<DoneEvent>(item.Data, Json),
                _ => null
            };

            if (evt is not null)
                yield return evt;
        }
    }            
}