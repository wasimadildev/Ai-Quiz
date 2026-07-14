using System.ComponentModel.DataAnnotations;

namespace AiQuiz.Models;

public class ManualAnswer
{
    public int Id { get; set; }

    public int StudentAttemptId { get; set; }
    public StudentAttempt? StudentAttempt { get; set; }

    public int QuestionId { get; set; }
    public Question? Question { get; set; }

    [MaxLength(5000)]
    public string AnswerText { get; set; } = string.Empty;

    public bool IsGraded { get; set; }
    public decimal? AwardedMarks { get; set; }

    [MaxLength(1000)]
    public string? TeacherFeedback { get; set; }

    public DateTime? GradedAt { get; set; }
    public string? GradedById { get; set; }
    public ApplicationUser? GradedBy { get; set; }
}
