namespace AiQuiz.Models;

public class StudentAttempt
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAt { get; set; }

    public int QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser? Student { get; set; }

    public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
    public Result? Result { get; set; }
}
