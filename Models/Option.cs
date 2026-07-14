using System.ComponentModel.DataAnnotations;

namespace AiQuiz.Models;

public class Option
{
    public int Id { get; set; }

    [Required]
    [MaxLength(1)]
    public string Label { get; set; } = string.Empty;

    [Required]
    public string Text { get; set; } = string.Empty;

    public bool IsCorrect { get; set; }

    public int QuestionId { get; set; }
    public Question? Question { get; set; }
}
