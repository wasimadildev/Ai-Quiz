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

    // Settings
    public QuizSettingsViewModel Settings { get; set; } = new();
    public List<string> AssignedStudentIds { get; set; } = new();
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
    [Display(Name = "Question Type")]
    public string QuestionType { get; set; } = "MCQ";

    [Required]
    [Display(Name = "Marks")]
    public int Marks { get; set; } = 1;

    [Display(Name = "Required")]
    public bool IsRequired { get; set; } = true;

    // MCQ options
    [Display(Name = "Option A")]
    public string? OptionA { get; set; }

    [Display(Name = "Option B")]
    public string? OptionB { get; set; }

    [Display(Name = "Option C")]
    public string? OptionC { get; set; }

    [Display(Name = "Option D")]
    public string? OptionD { get; set; }

    [Display(Name = "Correct Answer")]
    public string CorrectAnswer { get; set; } = "A";
}

public class QuizSettingsViewModel
{
    [Display(Name = "Time Limit (minutes)")]
    public int TimeLimitMinutes { get; set; } = 30;

    [Display(Name = "Passing Percentage")]
    public decimal PassingPercentage { get; set; } = 50;

    public bool RandomizeQuestions { get; set; }
    public bool RandomizeOptions { get; set; }
    public bool AllowReview { get; set; } = true;
    public bool OneAttemptOnly { get; set; } = true;
    public bool NegativeMarking { get; set; }
    public decimal NegativeMarkFactor { get; set; } = 0.25m;
    public bool ShowResultImmediately { get; set; } = true;
    public bool RequireFullscreen { get; set; } = true;
    public bool EnableAntiCheating { get; set; } = true;
    public string? JoinCode { get; set; }
    public string? QuizPassword { get; set; }
    public bool HasPassword { get; set; }
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableUntil { get; set; }

    // Violation limits
    public int MaxTabSwitches { get; set; } = 3;
    public int MaxWindowChanges { get; set; } = 3;
    public int MaxCopyAttempts { get; set; } = 2;
    public int MaxPasteAttempts { get; set; } = 2;
    public int MaxRightClicks { get; set; } = 2;
    public int MaxFullscreenExits { get; set; } = 3;
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
    public int AttemptId { get; set; }
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public List<AttemptQuestionViewModel> Questions { get; set; } = new();
    public QuizSettingsViewModel? Settings { get; set; }
    public bool RequiresPassword { get; set; }
}

public class AttemptQuestionViewModel
{
    public int QuestionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public string QuestionType { get; set; } = "MCQ";
    public int Marks { get; set; } = 1;
    public bool IsRequired { get; set; } = true;

    // For MCQ/TrueFalse
    public int? SelectedOptionId { get; set; }
    public List<Option> Options { get; set; } = new();

    // For Short/Long Answer
    public string? TextAnswer { get; set; }
}

public class DashboardViewModel
{
    public int TotalQuizzes { get; set; }
    public int PublishedQuizzes { get; set; }
    public int Attempts { get; set; }

    // Analytics
    public decimal AverageScore { get; set; }
    public decimal HighestScore { get; set; }
    public decimal LowestScore { get; set; }
    public decimal AverageCompletionTime { get; set; }
    public int TotalViolations { get; set; }
    public int FailedDueToCheating { get; set; }
    public int PendingReviews { get; set; }
    public int TotalStudents { get; set; }
}

public class MonitoringViewModel
{
    public int QuizId { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public List<MonitoringEntryViewModel> Entries { get; set; } = new();
}

public class MonitoringEntryViewModel
{
    public int AttemptId { get; set; }
    public int QuizId { get; set; }
    public string StudentId { get; set; } = string.Empty;
    public string StudentName { get; set; } = string.Empty;
    public string? RollNumber { get; set; }
    public string QuizTitle { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public string TotalTime { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public decimal MaxScore { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ViolationCount { get; set; }
}

public class MonitoringDetailViewModel
{
    public MonitoringEntryViewModel Entry { get; set; } = new();
    public List<AnswerDetailViewModel> Answers { get; set; } = new();
    public List<ManualAnswerDetailViewModel> ManualAnswers { get; set; } = new();
    public List<ViolationDetailViewModel> Violations { get; set; } = new();
}

public class AnswerDetailViewModel
{
    public string QuestionText { get; set; } = string.Empty;
    public string? SelectedAnswer { get; set; }
    public string? CorrectAnswer { get; set; }
    public bool IsCorrect { get; set; }
    public int Marks { get; set; }
    public string QuestionType { get; set; } = "MCQ";
}

public class ManualAnswerDetailViewModel
{
    public int ManualAnswerId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string StudentAnswer { get; set; } = string.Empty;
    public int MaxMarks { get; set; }
    public decimal? AwardedMarks { get; set; }
    public string? TeacherFeedback { get; set; }
    public bool IsGraded { get; set; }
}

public class ViolationDetailViewModel
{
    public string EventType { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
    public int Count { get; set; }
    public string? Details { get; set; }
}

public class GradeViewModel
{
    public int ManualAnswerId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string StudentAnswer { get; set; } = string.Empty;
    public int MaxMarks { get; set; }
    public decimal AwardedMarks { get; set; }
    public string? TeacherFeedback { get; set; }
}

public class ReviewDashboardViewModel
{
    public List<ReviewEntryViewModel> Entries { get; set; } = new();
}

public class ReviewEntryViewModel
{
    public int AttemptId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string QuizTitle { get; set; } = string.Empty;
    public int PendingCount { get; set; }
    public int TotalSubjective { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class JoinQuizViewModel
{
    [Required]
    [Display(Name = "Join Code")]
    public string JoinCode { get; set; } = string.Empty;

    public string? Password { get; set; }
    public string? ErrorMessage { get; set; }
}

public class LogViolationRequest
{
    public int AttemptId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public int Count { get; set; }
}
