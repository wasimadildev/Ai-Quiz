using System.ComponentModel.DataAnnotations;

namespace AiQuiz.Models;

public class Quiz
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Quiz title is required.")]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Topic { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Difficulty { get; set; } = "Easy";

    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public string TeacherId { get; set; } = string.Empty;

    public ApplicationUser? Teacher { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<StudentAttempt> StudentAttempts { get; set; } = new List<StudentAttempt>();
}
