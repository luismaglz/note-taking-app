namespace NTA.Api.Contracts;

/// <summary>
/// Defines the fields required to create a user.
/// </summary>
public sealed record CreateUserRequest(int? Id, string Name);
