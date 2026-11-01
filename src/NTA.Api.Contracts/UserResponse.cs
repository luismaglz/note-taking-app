namespace NTA.Api.Contracts;

/// <summary>
/// Represents a user returned by the management API.
/// </summary>
public sealed record UserResponse(int Id, string Name);
