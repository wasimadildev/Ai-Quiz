using System.ComponentModel.DataAnnotations;

namespace AiQuiz.Models;

public class QuizSettings
{
    public int Id { get; set; }

    public int QuizId { get; set; }
    public Quiz? Quiz { get; set; }

    [Display(Name = "Time Limit (minutes)")]
    public int TimeLimitMinutes { get; set; } = 30;

    [Display(Name = "Passing Percentage")]
    public decimal PassingPercentage { get; set; } = 50;

    [Display(Name = "Randomize Questions")]
    public bool RandomizeQuestions { get; set; }

    [Display(Name = "Randomize MCQ Options")]
    public bool RandomizeOptions { get; set; }

    [Display(Name = "Allow Review Before Submit")]
    public bool AllowReview { get; set; } = true;

    [Display(Name = "One Attempt Only")]
    public bool OneAttemptOnly { get; set; } = true;

    [Display(Name = "Negative Marking")]
    public bool NegativeMarking { get; set; }

    [Display(Name = "Negative Mark Factor")]
    public decimal NegativeMarkFactor { get; set; } = 0.25m;

    [Display(Name = "Show Result Immediately")]
    public bool ShowResultImmediately { get; set; } = true;

    [Display(Name = "Require Fullscreen")]
    public bool RequireFullscreen { get; set; } = true;

    [Display(Name = "Enable Anti-Cheating")]
    public bool EnableAntiCheating { get; set; } = true;

    [MaxLength(20)]
    public string? JoinCode { get; set; }

    [MaxLength(100)]
    public string? Password { get; set; }

    [Display(Name = "Has Password")]
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
