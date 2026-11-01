using Microsoft.EntityFrameworkCore;
using NTA.Infrastructure.Sqlite.Models;

namespace NTA.Infrastructure.Sqlite;

/// <summary>
/// Represents the SQLite persistence context for note records.
/// </summary>
public sealed class NoteDbContext(DbContextOptions<NoteDbContext> options) : DbContext(options)
{
    internal DbSet<SqliteNote> Notes => Set<SqliteNote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var note = modelBuilder.Entity<SqliteNote>();

        note.ToTable("Notes");
        note.HasKey(entity => entity.Id);
        note.Property(entity => entity.Title).HasMaxLength(200).IsRequired();
        note.Property(entity => entity.Content).IsRequired();
        note.Property(entity => entity.Visibility).HasMaxLength(20).IsRequired();
    }
}
