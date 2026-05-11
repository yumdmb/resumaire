using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Resumaire.Api.Data.Entities;

namespace Resumaire.Api.Data;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Job> Jobs => Set<Job>();

    public DbSet<BaseResume> BaseResumes => Set<BaseResume>();

    public DbSet<TailoredResume> TailoredResumes => Set<TailoredResume>();

    public DbSet<TailoringSuggestion> TailoringSuggestions => Set<TailoringSuggestion>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>(user =>
        {
            user.Property(value => value.CreatedAt).IsRequired();
        });

        builder.Entity<Job>(job =>
        {
            job.ToTable("Jobs");

            job.HasKey(value => value.Id);

            job.Property(value => value.Company)
                .IsRequired()
                .HasMaxLength(200);

            job.Property(value => value.Title)
                .IsRequired()
                .HasMaxLength(200);

            job.Property(value => value.Link)
                .HasMaxLength(2048);

            job.Property(value => value.Description)
                .IsRequired();

            job.Property(value => value.Status)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(32);

            job.Property(value => value.DateApplied)
                .HasColumnType("date");

            job.Property(value => value.CreatedAt)
                .IsRequired();

            job.Property(value => value.UpdatedAt)
                .IsRequired();

            job.HasOne(value => value.User)
                .WithMany(value => value.Jobs)
                .HasForeignKey(value => value.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            job.HasOne(value => value.SelectedBaseResume)
                .WithMany()
                .HasForeignKey(value => value.SelectedBaseResumeId)
                .OnDelete(DeleteBehavior.SetNull);

            job.HasOne(value => value.SelectedTailoredResume)
                .WithMany()
                .HasForeignKey(value => value.SelectedTailoredResumeId)
                .OnDelete(DeleteBehavior.SetNull);

            job.HasIndex(value => new { value.UserId, value.Status });
            job.HasIndex(value => new { value.UserId, value.UpdatedAt });
            job.HasIndex(value => value.SelectedBaseResumeId);
            job.HasIndex(value => value.SelectedTailoredResumeId);
        });

        builder.Entity<BaseResume>(resume =>
        {
            resume.ToTable("BaseResumes");

            resume.HasKey(value => value.Id);

            resume.Property(value => value.SchemaVersion)
                .IsRequired();

            resume.Property(value => value.Revision)
                .IsRequired();

            resume.Property(value => value.ContentJson)
                .HasColumnType("jsonb")
                .IsRequired();

            resume.Property(value => value.CreatedAt)
                .IsRequired();

            resume.Property(value => value.UpdatedAt)
                .IsRequired();

            resume.HasOne(value => value.User)
                .WithOne(value => value.BaseResume)
                .HasForeignKey<BaseResume>(value => value.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            resume.HasIndex(value => value.UserId)
                .IsUnique();
        });

        builder.Entity<TailoredResume>(resume =>
        {
            resume.ToTable("TailoredResumes");

            resume.HasKey(value => value.Id);

            resume.Property(value => value.SourceBaseResumeContentJson)
                .HasColumnType("jsonb")
                .IsRequired();

            resume.Property(value => value.SchemaVersion)
                .IsRequired();

            resume.Property(value => value.VersionNumber)
                .IsRequired();

            resume.Property(value => value.Name)
                .HasMaxLength(200);

            resume.Property(value => value.ContentJson)
                .HasColumnType("jsonb")
                .IsRequired();

            resume.Property(value => value.CreatedAt)
                .IsRequired();

            resume.Property(value => value.UpdatedAt)
                .IsRequired();

            resume.HasOne(value => value.User)
                .WithMany(value => value.TailoredResumes)
                .HasForeignKey(value => value.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            resume.HasOne(value => value.Job)
                .WithMany(value => value.TailoredResumes)
                .HasForeignKey(value => value.JobId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            resume.HasOne(value => value.SourceBaseResume)
                .WithMany(value => value.TailoredResumes)
                .HasForeignKey(value => value.SourceBaseResumeId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            resume.HasIndex(value => new { value.UserId, value.JobId });
            resume.HasIndex(value => new { value.UserId, value.JobId, value.VersionNumber })
                .IsUnique();
            resume.HasIndex(value => value.SourceBaseResumeId);
        });

        builder.Entity<TailoringSuggestion>(suggestion =>
        {
            suggestion.ToTable("TailoringSuggestions");

            suggestion.HasKey(value => value.Id);

            suggestion.Property(value => value.ReviewState)
                .HasConversion<string>()
                .IsRequired()
                .HasMaxLength(32);

            suggestion.Property(value => value.TargetSection)
                .IsRequired()
                .HasMaxLength(100);

            suggestion.Property(value => value.OriginalContentJson)
                .HasColumnType("jsonb");

            suggestion.Property(value => value.SuggestedContentJson)
                .HasColumnType("jsonb")
                .IsRequired();

            suggestion.Property(value => value.AcceptedContentJson)
                .HasColumnType("jsonb");

            suggestion.Property(value => value.SourceEvidenceJson)
                .HasColumnType("jsonb");

            suggestion.Property(value => value.CreatedAt)
                .IsRequired();

            suggestion.HasOne(value => value.User)
                .WithMany(value => value.TailoringSuggestions)
                .HasForeignKey(value => value.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            suggestion.HasOne(value => value.Job)
                .WithMany(value => value.TailoringSuggestions)
                .HasForeignKey(value => value.JobId)
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            suggestion.HasOne(value => value.TailoredResume)
                .WithMany(value => value.TailoringSuggestions)
                .HasForeignKey(value => value.TailoredResumeId)
                .OnDelete(DeleteBehavior.SetNull);

            suggestion.HasOne(value => value.SourceBaseResume)
                .WithMany(value => value.TailoringSuggestions)
                .HasForeignKey(value => value.SourceBaseResumeId)
                .OnDelete(DeleteBehavior.SetNull);

            suggestion.HasIndex(value => new { value.UserId, value.JobId, value.ReviewState });
            suggestion.HasIndex(value => value.TailoredResumeId);
            suggestion.HasIndex(value => value.SourceBaseResumeId);
        });
    }
}
