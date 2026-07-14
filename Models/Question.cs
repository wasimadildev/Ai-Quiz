using System.ComponentModel.DataAnnotations;

namespace AiQuiz.Models;

public class Question
{
    public int Id { get; set; }

    [Required]
    public string Text { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Explanation { get; set; }

    public int QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    public ICollection<Option> Options { get; set; } = new List<Option>();
    public ICollection<StudentAnswer> StudentAnswers { get; set; } = new List<StudentAnswer>();
}
