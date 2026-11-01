using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using NTA.Api.Contracts;

namespace NTA.Api.Management.Tests;

/// <summary>
/// Verifies manager-protected user, team, and membership management endpoints.
/// </summary>
public sealed class ManagementEndpointsTests(ManagementAuthFactory factory) : IClassFixture<ManagementAuthFactory>
{
    /// <summary>
    /// Requires a manager token to access management resources.
    /// </summary>
    [Fact]
    public async Task ListUsers_WithoutManagerToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/users");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    /// <summary>
    /// Performs user and team CRUD while enforcing membership invariants.
    /// </summary>
    [Fact]
    public async Task Manager_CanManageUsersTeamsAndMemberships()
    {
        using var client = factory.CreateClient();
        await AuthenticateManagerAsync(client);

        var createUser = await client.PostAsJsonAsync("/users", new CreateUserRequest(101, "Ada"));
        Assert.Equal(HttpStatusCode.Created, createUser.StatusCode);
        Assert.Equal("Ada", (await createUser.Content.ReadFromJsonAsync<UserResponse>())!.Name);

        var getUser = await client.GetFromJsonAsync<UserResponse>("/users/101");
        Assert.Equal("Ada", getUser!.Name);

        var updateUser = await client.PutAsJsonAsync("/users/101", new UpdateUserRequest("Ada Lovelace"));
        Assert.Equal(HttpStatusCode.OK, updateUser.StatusCode);
        Assert.Equal("Ada Lovelace", (await updateUser.Content.ReadFromJsonAsync<UserResponse>())!.Name);

        var users = await client.GetFromJsonAsync<UserResponse[]>("/users");
        Assert.Contains(users!, user => user.Id == 101);

        var createTeam = await client.PostAsJsonAsync("/teams", new CreateTeamRequest(201, "Platform"));
        Assert.Equal(HttpStatusCode.Created, createTeam.StatusCode);
        Assert.Empty((await createTeam.Content.ReadFromJsonAsync<TeamResponse>())!.MemberIds);

        var updateTeam = await client.PutAsJsonAsync("/teams/201", new UpdateTeamRequest("Platform Engineering"));
        Assert.Equal(HttpStatusCode.OK, updateTeam.StatusCode);
        Assert.Equal("Platform Engineering", (await updateTeam.Content.ReadFromJsonAsync<TeamResponse>())!.Name);

        var teams = await client.GetFromJsonAsync<TeamResponse[]>("/teams");
        Assert.Contains(teams!, team => team.Id == 201);

        var addMember = await client.PostAsJsonAsync("/teams/201/members", new AddTeamMemberRequest(101));
        Assert.Equal(HttpStatusCode.OK, addMember.StatusCode);
        Assert.Contains(101, (await addMember.Content.ReadFromJsonAsync<TeamResponse>())!.MemberIds);

        var updateMemberTeam = await client.PutAsJsonAsync("/teams/201", new UpdateTeamRequest("Platform Team"));
        Assert.Equal(HttpStatusCode.OK, updateMemberTeam.StatusCode);
        Assert.Contains(101, (await updateMemberTeam.Content.ReadFromJsonAsync<TeamResponse>())!.MemberIds);

        var members = await client.GetFromJsonAsync<TeamMemberResponse[]>("/teams/201/members");
        Assert.Contains(members!, member => member.UserId == 101);

        var duplicate = await client.PostAsJsonAsync("/teams/201/members", new AddTeamMemberRequest(101));
        Assert.Equal(HttpStatusCode.Conflict, duplicate.StatusCode);

        var missingUser = await client.PostAsJsonAsync("/teams/201/members", new AddTeamMemberRequest(999));
        Assert.Equal(HttpStatusCode.NotFound, missingUser.StatusCode);

        var deleteMemberUser = await client.DeleteAsync("/users/101");
        Assert.Equal(HttpStatusCode.Conflict, deleteMemberUser.StatusCode);

        var deleteNonEmptyTeam = await client.DeleteAsync("/teams/201");
        Assert.Equal(HttpStatusCode.Conflict, deleteNonEmptyTeam.StatusCode);

        var removeMember = await client.DeleteAsync("/teams/201/members/101");
        Assert.Equal(HttpStatusCode.OK, removeMember.StatusCode);
        Assert.Empty((await removeMember.Content.ReadFromJsonAsync<TeamResponse>())!.MemberIds);

        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/teams/201")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/users/101")).StatusCode);
    }

    private static async Task AuthenticateManagerAsync(HttpClient client)
    {
        var response = await client.PostAsync("/auth/1", null);
        response.EnsureSuccessStatusCode();
        var authentication = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authentication!.AccessToken);
    }
}
