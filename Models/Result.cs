namespace AiQuiz.Models;

public class Result
{
    public int Id { get; set; }

    public int StudentAttemptId { get; set; }
    public StudentAttempt? StudentAttempt { get; set; }

    public int TotalQuestions { get; set; }
    public int CorrectAnswers { get; set; }
    public int WrongAnswers { get; set; }
    public decimal Percentage { get; set; }
    public int TotalScore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
