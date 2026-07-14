using System.ComponentModel.DataAnnotations;
using AiQuiz.Models;

namespace AiQuiz.ViewModels;

public class QuizFormViewModel
{
    public int Id { get; set; }

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Topic { get; set; } = string.Empty;

    [Required]
    public string Difficulty { get; set; } = "Easy";

    public bool IsPublished { get; set; }
    public List<QuestionFormViewModel> Questions { get; set; } = new();
}

public class QuestionFormViewModel
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Question")]
    public string Text { get; set; } = string.Empty;

    [Display(Name = "Explanation")]
    public string? Explanation { get; set; }

    [Required]
    [Display(Name = "Option A")]
    public string OptionA { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Option B")]
    public string OptionB { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Option C")]
    public string OptionC { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Option D")]
    public string OptionD { get; set; } = string.Empty;

    [Required]
    [RegularExpression("A|B|C|D", ErrorMessage = "Correct answer must be A, B, C, or D.")]
    [Display(Name = "Correct Answer")]
    public string CorrectAnswer { get; set; } = "A";
}

public class GenerateQuizViewModel
{
    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Subject { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Topic { get; set; } = string.Empty;

    [Required]
    public string Difficulty { get; set; } = "Easy";

    [Required]
    [Range(1, 20)]
    [Display(Name = "Number of Questions")]
    public int NumberOfQuestions { get; set; } = 5;

    [Required]
    [Display(Name = "Question Type")]
    public string QuestionType { get; set; } = "MCQ";
}

public class AttemptQuizViewModel
{
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public List<AttemptQuestionViewModel> Questions { get; set; } = new();
}

public class AttemptQuestionViewModel
{
    public int QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please select an answer.")]
    public int? SelectedOptionId { get; set; }

    public List<Option> Options { get; set; } = new();
}

public class DashboardViewModel
{
    public int TotalQuizzes { get; set; }
    public int PublishedQuizzes { get; set; }
    public int Attempts { get; set; }
}
