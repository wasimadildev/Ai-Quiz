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

    public decimal MaxAutoMarks { get; set; }
    public decimal AutoMarksObtained { get; set; }
    public decimal MaxSubjectiveMarks { get; set; }
    public decimal SubjectiveMarksObtained { get; set; }
    public decimal TotalMaxMarks { get; set; }
    public decimal TotalMarksObtained { get; set; }

    public bool IsPass { get; set; }
    public bool IsManuallyReviewed { get; set; }

    [System.ComponentModel.DataAnnotations.MaxLength(2000)]
    public string? TeacherFeedback { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
