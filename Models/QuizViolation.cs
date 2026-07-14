using System.ComponentModel.DataAnnotations;

namespace AiQuiz.Models;

public class QuizViolation
{
    public int Id { get; set; }

    public int StudentAttemptId { get; set; }
    public StudentAttempt? StudentAttempt { get; set; }

    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
    public int Count { get; set; } = 1;
    public string? Details { get; set; }
}
