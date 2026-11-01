using System.Net;
using System.Text;
using NTA.DemoSeeder;

namespace NTA.DemoSeeder.Tests;

/// <summary>
/// Verifies the API workflow used to create the demonstration fixtures.
/// </summary>
public sealed class DemoDataSeederTests
{
    /// <summary>
    /// Creates users, teams, memberships, notes, and the two sharing grant kinds through the APIs.
    /// </summary>
    [Fact]
    public async Task SeedAsync_CreatesTheCompleteFixtureSetThroughApiRequests()
    {
        var managementHandler = new FixtureApiHandler(isManagementApi: true, fixturesExist: false);
        var usersHandler = new FixtureApiHandler(isManagementApi: false, fixturesExist: false);
        using var managementClient = new HttpClient(managementHandler) { BaseAddress = new Uri("http://management/") };
        using var usersClient = new HttpClient(usersHandler) { BaseAddress = new Uri("http://users/") };

        await new DemoDataSeeder(managementClient, usersClient).SeedAsync(CancellationToken.None);

        Assert.Equal(3, managementHandler.UserCreates);
        Assert.Equal(2, managementHandler.TeamCreates);
        Assert.Equal(4, managementHandler.MemberCreates);
        Assert.Equal(3, usersHandler.NoteCreates);
        Assert.Equal(2, usersHandler.GrantCreates);
        Assert.Contains(usersHandler.RequestBodies, body => body.Contains("\"principalKind\":\"User\"", StringComparison.Ordinal));
        Assert.Contains(usersHandler.RequestBodies, body => body.Contains("\"principalKind\":\"Team\"", StringComparison.Ordinal));
    }

    /// <summary>
    /// Reuses matching records and accepts the documented duplicate-grant conflict on a subsequent run.
    /// </summary>
    [Fact]
    public async Task SeedAsync_WithExistingFixtures_DoesNotCreateDuplicateRecords()
    {
        var managementHandler = new FixtureApiHandler(isManagementApi: true, fixturesExist: true);
        var usersHandler = new FixtureApiHandler(isManagementApi: false, fixturesExist: true);
        using var managementClient = new HttpClient(managementHandler) { BaseAddress = new Uri("http://management/") };
        using var usersClient = new HttpClient(usersHandler) { BaseAddress = new Uri("http://users/") };

        await new DemoDataSeeder(managementClient, usersClient).SeedAsync(CancellationToken.None);

        Assert.Equal(0, managementHandler.UserCreates);
        Assert.Equal(0, managementHandler.TeamCreates);
        Assert.Equal(0, managementHandler.MemberCreates);
        Assert.Equal(0, usersHandler.NoteCreates);
        Assert.Equal(2, usersHandler.GrantCreates);
    }

    private sealed class FixtureApiHandler(bool isManagementApi, bool fixturesExist) : HttpMessageHandler
    {
        internal int UserCreates { get; private set; }

        internal int TeamCreates { get; private set; }

        internal int MemberCreates { get; private set; }

        internal int NoteCreates { get; private set; }

        internal int GrantCreates { get; private set; }

        internal List<string> RequestBodies { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri!.AbsolutePath;
            if (request.Content is not null)
            {
                RequestBodies.Add(await request.Content.ReadAsStringAsync(cancellationToken));
            }

            if (request.Method == HttpMethod.Post && path.StartsWith("/auth/", StringComparison.Ordinal))
            {
                return Json(HttpStatusCode.OK, """{"accessToken":"fixture-token"}""");
            }

            if (isManagementApi)
            {
                return HandleManagementRequest(request, path);
            }

            return HandleUsersRequest(request, path);
        }

        private HttpResponseMessage HandleManagementRequest(HttpRequestMessage request, string path)
        {
            if (request.Method == HttpMethod.Get && path == "/users")
            {
                return Json(
                    HttpStatusCode.OK,
                    fixturesExist
                        ? """[{"id":90101,"name":"Demo Ada"},{"id":90102,"name":"Demo Bruno"},{"id":90103,"name":"Demo Chandra"}]"""
                        : "[]");
            }

            if (request.Method == HttpMethod.Post && path == "/users")
            {
                UserCreates++;
                return Json(HttpStatusCode.Created, "{}");
            }

            if (request.Method == HttpMethod.Get && path == "/teams")
            {
                return Json(
                    HttpStatusCode.OK,
                    fixturesExist
                        ? """[{"id":90201,"name":"Demo Product","memberIds":[90101,90102]},{"id":90202,"name":"Demo Research","memberIds":[90102,90103]}]"""
                        : "[]");
            }

            if (request.Method == HttpMethod.Post && path == "/teams")
            {
                TeamCreates++;
                return Json(HttpStatusCode.Created, "{}");
            }

            if (request.Method == HttpMethod.Get && path.EndsWith("/members", StringComparison.Ordinal))
            {
                return Json(
                    HttpStatusCode.OK,
                    fixturesExist
                        ? path.StartsWith("/teams/90201/", StringComparison.Ordinal)
                            ? """[{"userId":90101},{"userId":90102}]"""
                            : """[{"userId":90102},{"userId":90103}]"""
                        : "[]");
            }

            if (request.Method == HttpMethod.Post && path.EndsWith("/members", StringComparison.Ordinal))
            {
                MemberCreates++;
                return Json(HttpStatusCode.OK, "{}");
            }

            return Json(HttpStatusCode.NotFound, "{}");
        }

        private HttpResponseMessage HandleUsersRequest(HttpRequestMessage request, string path)
        {
            if (request.Method == HttpMethod.Get && path == "/notes")
            {
                return Json(
                    HttpStatusCode.OK,
                    fixturesExist
                        ? """[{"id":"a4bf0c08-d49a-4740-830d-42ac80c83b26","ownerId":90101,"title":"[Demo] Product roadmap","content":"fixture","createdAt":"2026-01-01T00:00:00+00:00","updatedAt":"2026-01-01T00:00:00+00:00"},{"id":"b4bf0c08-d49a-4740-830d-42ac80c83b26","ownerId":90102,"title":"[Demo] Research findings","content":"fixture","createdAt":"2026-01-01T00:00:00+00:00","updatedAt":"2026-01-01T00:00:00+00:00"},{"id":"c4bf0c08-d49a-4740-830d-42ac80c83b26","ownerId":90103,"title":"[Demo] Welcome note","content":"fixture","createdAt":"2026-01-01T00:00:00+00:00","updatedAt":"2026-01-01T00:00:00+00:00"}]"""
                        : "[]");
            }

            if (request.Method == HttpMethod.Post && path == "/notes")
            {
                NoteCreates++;
                return Json(
                    HttpStatusCode.Created,
                    """{"id":"a4bf0c08-d49a-4740-830d-42ac80c83b26","ownerId":90101,"title":"fixture","content":"fixture","createdAt":"2026-01-01T00:00:00+00:00","updatedAt":"2026-01-01T00:00:00+00:00"}""");
            }

            if (request.Method == HttpMethod.Post && path.EndsWith("/access-grants", StringComparison.Ordinal))
            {
                GrantCreates++;
                return Json(fixturesExist ? HttpStatusCode.Conflict : HttpStatusCode.Created, "{}");
            }

            return Json(HttpStatusCode.NotFound, "{}");
        }

        private static HttpResponseMessage Json(HttpStatusCode statusCode, string content) =>
            new(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };
    }
}
