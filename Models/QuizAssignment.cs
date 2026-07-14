namespace AiQuiz.Models;

public class QuizAssignment
{
    public int Id { get; set; }

    public int QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    public string StudentId { get; set; } = string.Empty;
    public ApplicationUser? Student { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public bool HasJoined { get; set; }
    public DateTime? JoinedAt { get; set; }
}
