using AiQuiz.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AiQuiz.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Quiz> Quizzes => Set<Quiz>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<Option> Options => Set<Option>();
    public DbSet<StudentAttempt> StudentAttempts => Set<StudentAttempt>();
    public DbSet<StudentAnswer> StudentAnswers => Set<StudentAnswer>();
    public DbSet<Result> Results => Set<Result>();
    public DbSet<QuizSettings> QuizSettings => Set<QuizSettings>();
    public DbSet<QuizAssignment> QuizAssignments => Set<QuizAssignment>();
    public DbSet<QuizViolation> QuizViolations => Set<QuizViolation>();
    public DbSet<ManualAnswer> ManualAnswers => Set<ManualAnswer>();
    public DbSet<StudentNotification> StudentNotifications => Set<StudentNotification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Quiz>()
            .HasOne(q => q.Teacher)
            .WithMany(u => u.Quizzes)
            .HasForeignKey(q => q.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Question>()
            .HasOne(q => q.Quiz)
            .WithMany(q => q.Questions)
            .HasForeignKey(q => q.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Option>()
            .HasOne(o => o.Question)
            .WithMany(q => q.Options)
            .HasForeignKey(o => o.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentAttempt>()
            .HasOne(a => a.Student)
            .WithMany(u => u.StudentAttempts)
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StudentAttempt>()
            .HasOne(a => a.Quiz)
            .WithMany(q => q.StudentAttempts)
            .HasForeignKey(a => a.QuizId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Result>()
            .Property(r => r.Percentage)
            .HasPrecision(5, 2);

        builder.Entity<Result>()
            .Property(r => r.AutoMarksObtained).HasPrecision(7, 2);
        builder.Entity<Result>()
            .Property(r => r.SubjectiveMarksObtained).HasPrecision(7, 2);
        builder.Entity<Result>()
            .Property(r => r.TotalMarksObtained).HasPrecision(7, 2);
        builder.Entity<Result>()
            .Property(r => r.TotalMaxMarks).HasPrecision(7, 2);

        builder.Entity<Result>()
            .HasOne(r => r.StudentAttempt)
            .WithOne(a => a.Result)
            .HasForeignKey<Result>(r => r.StudentAttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuizSettings>()
            .HasOne(s => s.Quiz)
            .WithOne(q => q.Settings)
            .HasForeignKey<QuizSettings>(s => s.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuizSettings>()
            .Property(s => s.PassingPercentage)
            .HasPrecision(5, 2);
        builder.Entity<QuizSettings>()
            .Property(s => s.NegativeMarkFactor)
            .HasPrecision(5, 2);

        builder.Entity<QuizAssignment>()
            .HasOne(a => a.Quiz)
            .WithMany(q => q.Assignments)
            .HasForeignKey(a => a.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<QuizAssignment>()
            .HasOne(a => a.Student)
            .WithMany()
            .HasForeignKey(a => a.StudentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<QuizAssignment>()
            .HasIndex(a => new { a.QuizId, a.StudentId })
            .IsUnique();

        builder.Entity<QuizViolation>()
            .HasOne(v => v.StudentAttempt)
            .WithMany(a => a.Violations)
            .HasForeignKey(v => v.StudentAttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ManualAnswer>()
            .HasOne(m => m.StudentAttempt)
            .WithMany(a => a.ManualAnswers)
            .HasForeignKey(m => m.StudentAttemptId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ManualAnswer>()
            .HasOne(m => m.Question)
            .WithMany(q => q.ManualAnswers)
            .HasForeignKey(m => m.QuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ManualAnswer>()
            .HasOne(m => m.GradedBy)
            .WithMany()
            .HasForeignKey(m => m.GradedById)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<StudentNotification>()
            .HasOne(n => n.Student)
            .WithMany()
            .HasForeignKey(n => n.StudentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StudentNotification>()
            .HasOne(n => n.Quiz)
            .WithMany()
            .HasForeignKey(n => n.QuizId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
