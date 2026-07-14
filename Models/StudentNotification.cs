using System.ComponentModel.DataAnnotations;

namespace AiQuiz.Models;

public class StudentNotification
{
    public int Id { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser? Student { get; set; }

    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // QuizAssigned, QuizPublished, QuizCancelled, ResultPublished, FeedbackAvailable

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int? QuizId { get; set; }
    public Quiz? Quiz { get; set; }
}
