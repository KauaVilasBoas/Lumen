using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AegisIdentity.Backoffice.Services;

public sealed class AdminApiClient
{
    private const string AccessTokenClaimType = "access_token";

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AdminApiClient(HttpClient http, IHttpContextAccessor httpContextAccessor)
    {
        _http = http;
        _httpContextAccessor = httpContextAccessor;
    }

    public sealed record ProfileItem(Guid Id, string Name, string Description, bool IsSystem);

    public sealed record ProfileDetail(
        Guid Id,
        string Name,
        string Description,
        bool IsSystem,
        IReadOnlyList<Guid> PermissionIds);

    public sealed record PermissionItem(Guid Id, string Code, string DisplayName, bool IsOrphan);

    public sealed record PermissionGroup(Guid? GroupId, string GroupName, IReadOnlyList<PermissionItem> Permissions);

    public sealed record UserProfileItem(Guid AssignmentId, Guid ProfileId, string ProfileName, bool IsSystem);

    public async Task<IReadOnlyList<ProfileItem>?> ListProfilesAsync(CancellationToken ct = default)
    {
        using var request = BuildGet("api/profiles");
        using var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<List<ProfileItem>>(ct);
    }

    public async Task<ProfileDetail?> GetProfileAsync(Guid id, CancellationToken ct = default)
    {
        using var request = BuildGet($"api/profiles/{id}");
        using var response = await _http.SendAsync(request, ct);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ProfileDetail>(ct);
    }

    public async Task<(bool Success, string? Error)> CreateProfileAsync(
        string name,
        string description,
        CancellationToken ct = default)
    {
        using var request = BuildPost("api/profiles", new { name, description });
        using var response = await _http.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
            return (true, null);

        var body = await response.Content.ReadAsStringAsync(ct);
        return (false, body);
    }

    public async Task<(bool Success, string? Error)> UpdateProfileAsync(
        Guid id,
        string name,
        string description,
        CancellationToken ct = default)
    {
        using var request = BuildPut($"api/profiles/{id}", new { name, description });
        using var response = await _http.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
            return (true, null);

        var body = await response.Content.ReadAsStringAsync(ct);
        return (false, body);
    }

    public async Task<(bool Success, string? Error)> DeleteProfileAsync(Guid id, CancellationToken ct = default)
    {
        using var request = BuildDelete($"api/profiles/{id}");
        using var response = await _http.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
            return (true, null);

        var body = await response.Content.ReadAsStringAsync(ct);
        return (false, body);
    }

    public async Task<(bool Success, string? Error)> SetProfilePermissionsAsync(
        Guid profileId,
        IReadOnlyList<Guid> permissionIds,
        CancellationToken ct = default)
    {
        using var request = BuildPut($"api/profiles/{profileId}/permissions", new { permissionIds });
        using var response = await _http.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
            return (true, null);

        var body = await response.Content.ReadAsStringAsync(ct);
        return (false, body);
    }

    public async Task<IReadOnlyList<PermissionGroup>?> ListPermissionsAsync(CancellationToken ct = default)
    {
        using var request = BuildGet("api/permissions");
        using var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<List<PermissionGroup>>(ct);
    }

    public async Task<IReadOnlyList<UserProfileItem>?> ListUserProfilesAsync(Guid userId, CancellationToken ct = default)
    {
        using var request = BuildGet($"api/users/{userId}/profiles");
        using var response = await _http.SendAsync(request, ct);

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<List<UserProfileItem>>(ct);
    }

    public async Task<(bool Success, string? Error)> AssignUserProfileAsync(
        Guid userId,
        Guid profileId,
        CancellationToken ct = default)
    {
        using var request = BuildPost($"api/users/{userId}/profiles/{profileId}", body: null);
        using var response = await _http.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
            return (true, null);

        var body = await response.Content.ReadAsStringAsync(ct);
        return (false, body);
    }

    public async Task<(bool Success, string? Error)> RemoveUserProfileAsync(
        Guid userId,
        Guid profileId,
        CancellationToken ct = default)
    {
        using var request = BuildDelete($"api/users/{userId}/profiles/{profileId}");
        using var response = await _http.SendAsync(request, ct);

        if (response.IsSuccessStatusCode)
            return (true, null);

        var body = await response.Content.ReadAsStringAsync(ct);
        return (false, body);
    }

    private HttpRequestMessage BuildGet(string path)
    {
        var msg = new HttpRequestMessage(HttpMethod.Get, path);
        AttachBearerToken(msg);
        return msg;
    }

    private HttpRequestMessage BuildPost(string path, object? body)
    {
        var msg = new HttpRequestMessage(HttpMethod.Post, path);
        if (body is not null)
            msg.Content = JsonContent.Create(body);
        AttachBearerToken(msg);
        return msg;
    }

    private HttpRequestMessage BuildPut(string path, object body)
    {
        var msg = new HttpRequestMessage(HttpMethod.Put, path);
        msg.Content = JsonContent.Create(body);
        AttachBearerToken(msg);
        return msg;
    }

    private HttpRequestMessage BuildDelete(string path)
    {
        var msg = new HttpRequestMessage(HttpMethod.Delete, path);
        AttachBearerToken(msg);
        return msg;
    }

    private void AttachBearerToken(HttpRequestMessage message)
    {
        var token = _httpContextAccessor.HttpContext?.User
            .FindFirstValue(AccessTokenClaimType);

        if (token is not null)
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
