using System.ComponentModel.DataAnnotations;

namespace AiQuiz.Models;

public class Question
{
    public int Id { get; set; }

    [Required]
    public string Text { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Explanation { get; set; }

    [MaxLength(30)]
    public string QuestionType { get; set; } = "MCQ"; // MCQ, TrueFalse, ShortAnswer, LongAnswer

    public int Marks { get; set; } = 1;

    public bool IsRequired { get; set; } = true;

    public int QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    public ICollection<Option> Options { get; set; } = new List<Option>();
    public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
    public ICollection<ManualAnswer> ManualAnswers { get; set; } = new List<ManualAnswer>();
}
