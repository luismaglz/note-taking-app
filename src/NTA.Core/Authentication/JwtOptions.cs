namespace NTA.Core.Authentication;

/// <summary>
/// Defines the configuration used to issue and validate JSON Web Tokens.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// Gets the configuration section name.
    /// </summary>
    public const string SectionName = "Jwt";

    /// <summary>
    /// Gets or sets the token issuer.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token audience.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symmetric signing key.
    /// </summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token lifetime in minutes.
    /// </summary>
    public int ExpirationMinutes { get; set; } = 60;

    /// <summary>
    /// Validates the configured token settings.
    /// </summary>
    public void Validate()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Issuer);
        ArgumentException.ThrowIfNullOrWhiteSpace(Audience);

        if (SigningKey.Length < 32)
        {
            throw new InvalidOperationException("JWT signing keys must contain at least 32 characters.");
        }

        if (ExpirationMinutes <= 0)
        {
            throw new InvalidOperationException("JWT expiration minutes must be greater than zero.");
        }
    }
}
