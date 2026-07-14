using System.ComponentModel.DataAnnotations;

namespace AiQuiz.Models;

public class StudentAttempt
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }

    [MaxLength(30)]
    public string Status { get; set; } = "InProgress"; // InProgress, Completed, Failed, Cancelled

    public int ViolationCount { get; set; }
    public bool IsTerminated { get; set; }

    public int QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser? Student { get; set; }

    public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
    public ICollection<ManualAnswer> ManualAnswers { get; set; } = new List<ManualAnswer>();
    public ICollection<QuizViolation> Violations { get; set; } = new List<QuizViolation>();
    public Result? Result { get; set; }
}
