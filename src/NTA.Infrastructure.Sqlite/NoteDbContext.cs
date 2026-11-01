using Microsoft.EntityFrameworkCore;
using NTA.Infrastructure.Sqlite.Models;

namespace NTA.Infrastructure.Sqlite;

/// <summary>
/// Represents the SQLite persistence context for note and user records.
/// </summary>
public sealed class NoteDbContext(DbContextOptions<NoteDbContext> options) : DbContext(options)
{
    internal DbSet<SqliteNote> Notes => Set<SqliteNote>();

    internal DbSet<SqliteUser> Users => Set<SqliteUser>();

    internal DbSet<SqliteNoteAccessPolicy> NoteAccessPolicies => Set<SqliteNoteAccessPolicy>();

    internal DbSet<SqliteNoteAccessGrant> NoteAccessGrants => Set<SqliteNoteAccessGrant>();

    internal DbSet<SqliteTeam> Teams => Set<SqliteTeam>();

    internal DbSet<SqliteTeamMembership> TeamMemberships => Set<SqliteTeamMembership>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var note = modelBuilder.Entity<SqliteNote>();

        note.ToTable("Notes");
        note.HasKey(entity => entity.Id);
        note.Property(entity => entity.Title).HasMaxLength(200).IsRequired();
        note.Property(entity => entity.Content).IsRequired();
        note.Property(entity => entity.Revision).HasDefaultValue(1).IsRequired();
        var user = modelBuilder.Entity<SqliteUser>();

        user.ToTable("Users");
        user.HasKey(entity => entity.Id);
        user.Property(entity => entity.Id).ValueGeneratedNever();

        var policy = modelBuilder.Entity<SqliteNoteAccessPolicy>();

        policy.ToTable("NoteAccessPolicies");
        policy.HasKey(entity => entity.NoteId);

        var grant = modelBuilder.Entity<SqliteNoteAccessGrant>();

        grant.ToTable("NoteAccessGrants");
        grant.HasKey(entity => new { entity.NoteId, entity.PrincipalKind, entity.PrincipalId, entity.Permission });
        grant.Property(entity => entity.PrincipalKind).HasMaxLength(20).IsRequired();
        grant.Property(entity => entity.Permission).HasMaxLength(20).IsRequired();

        var team = modelBuilder.Entity<SqliteTeam>();

        team.ToTable("Teams");
        team.HasKey(entity => entity.Id);
        team.Property(entity => entity.Id).ValueGeneratedNever();

        var membership = modelBuilder.Entity<SqliteTeamMembership>();

        membership.ToTable("TeamMemberships");
        membership.HasKey(entity => new { entity.TeamId, entity.UserId });
    }
}
