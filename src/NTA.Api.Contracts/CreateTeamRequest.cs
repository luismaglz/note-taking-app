namespace NTA.Api.Contracts;

/// <summary>
/// Defines the fields required to create a team.
/// </summary>
public sealed record CreateTeamRequest(int? Id, string Name);
