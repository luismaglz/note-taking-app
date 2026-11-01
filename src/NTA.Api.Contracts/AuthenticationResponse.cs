namespace NTA.Api.Contracts;

/// <summary>
/// Represents the access token returned by an authentication endpoint.
/// </summary>
public sealed record AuthenticationResponse(string AccessToken);
