using NTA.Api.Contracts;

namespace NTA.DemoSeeder;

internal static class DemoFixtures
{
    internal static IReadOnlyList<DemoUser> Users { get; } =
    [
        new(90101, "Demo Ada"),
        new(90102, "Demo Bruno"),
        new(90103, "Demo Chandra")
    ];

    internal static IReadOnlyList<DemoTeam> Teams { get; } =
    [
        new(90201, "Demo Product", [90101, 90102]),
        new(90202, "Demo Research", [90102, 90103])
    ];

    internal static IReadOnlyList<DemoNote> Notes { get; } =
    [
        new("roadmap", 90101, "[Demo] Product roadmap", "A shared roadmap for the demo product team."),
        new("research", 90102, "[Demo] Research findings", "A shared research summary for the demo research team."),
        new("welcome", 90103, "[Demo] Welcome note", "A private note from the third demo user.")
    ];

    internal static IReadOnlyList<DemoGrant> Grants { get; } =
    [
        new("roadmap", AccessPrincipalKindContract.User, 90102, NotePermissionContract.Read),
        new("research", AccessPrincipalKindContract.Team, 90202, NotePermissionContract.Write)
    ];
}

internal sealed record DemoUser(int Id, string Name);

internal sealed record DemoTeam(int Id, string Name, IReadOnlyList<int> MemberIds);

internal sealed record DemoNote(string Key, int OwnerId, string Title, string Content);

internal sealed record DemoGrant(
    string NoteKey,
    AccessPrincipalKindContract PrincipalKind,
    int PrincipalId,
    NotePermissionContract Permission);
