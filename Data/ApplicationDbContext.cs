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
            .HasOne(r => r.StudentAttempt)
            .WithOne(a => a.Result)
            .HasForeignKey<Result>(r => r.StudentAttemptId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

