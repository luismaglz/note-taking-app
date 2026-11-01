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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var note = modelBuilder.Entity<SqliteNote>();

        note.ToTable("Notes");
        note.HasKey(entity => entity.Id);
        note.Property(entity => entity.Title).HasMaxLength(200).IsRequired();
        note.Property(entity => entity.Content).IsRequired();
        note.Property(entity => entity.Visibility).HasMaxLength(20).IsRequired();

        var user = modelBuilder.Entity<SqliteUser>();

        user.ToTable("Users");
        user.HasKey(entity => entity.Id);
        user.Property(entity => entity.Id).ValueGeneratedNever();
    }
}
