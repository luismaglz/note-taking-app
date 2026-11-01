using Microsoft.EntityFrameworkCore;

namespace NTA.Infrastructure.Sqlite;

public sealed class NoteDbContext(DbContextOptions<NoteDbContext> options) : DbContext(options)
{
    internal DbSet<NoteEntity> Notes => Set<NoteEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var note = modelBuilder.Entity<NoteEntity>();

        note.ToTable("Notes");
        note.HasKey(entity => entity.Id);
        note.Property(entity => entity.Title).HasMaxLength(200).IsRequired();
        note.Property(entity => entity.Content).IsRequired();
        note.Property(entity => entity.Visibility).HasMaxLength(20).IsRequired();
    }
}
