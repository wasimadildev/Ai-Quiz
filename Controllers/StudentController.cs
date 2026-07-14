using AiQuiz.Data;
using AiQuiz.Models;
using AiQuiz.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiQuiz.Controllers;

[Authorize(Roles = "Student")]
public class StudentController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public StudentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Dashboard()
    {
        var studentId = _userManager.GetUserId(User)!;

        var attempts = await _context.StudentAttempts
            .Include(a => a.Result)
            .Where(a => a.StudentId == studentId && a.Status == "Completed")
            .ToListAsync();

        var model = new DashboardViewModel
        {
            PublishedQuizzes = await _context.Quizzes.CountAsync(q => q.IsPublished),
            Attempts = attempts.Count,
            AverageScore = attempts.Any(a => a.Result != null)
                ? Math.Round(attempts.Where(a => a.Result != null).Average(a => a.Result!.Percentage), 2)
                : 0,
            HighestScore = attempts.Any(a => a.Result != null)
                ? attempts.Where(a => a.Result != null).Max(a => a.Result!.Percentage)
                : 0,
            LowestScore = attempts.Any(a => a.Result != null)
                ? attempts.Where(a => a.Result != null).Min(a => a.Result!.Percentage)
                : 0,
            PendingReviews = await _context.ManualAnswers
                .CountAsync(m => m.StudentAttempt!.StudentId == studentId && !m.IsGraded),
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult JoinQuiz()
    {
        return View(new JoinQuizViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> JoinQuiz(JoinQuizViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var studentId = _userManager.GetUserId(User)!;

        var quiz = await _context.Quizzes
            .Include(q => q.Settings)
            .FirstOrDefaultAsync(q => q.IsPublished
                && q.Settings != null
                && q.Settings.JoinCode == model.JoinCode);

        if (quiz == null)
        {
            model.ErrorMessage = "Invalid join code. Please try again.";
            return View(model);
        }

        var hasAssignments = await _context.QuizAssignments.AnyAsync(qa => qa.QuizId == quiz.Id);

        if (hasAssignments)
        {
            var assignment = await _context.QuizAssignments
                .FirstOrDefaultAsync(qa => qa.QuizId == quiz.Id && qa.StudentId == studentId);

            if (assignment == null)
            {
                model.ErrorMessage = "You are not assigned to this quiz.";
                return View(model);
            }

            if (!assignment.HasJoined)
            {
                assignment.HasJoined = true;
                assignment.JoinedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }
        else
        {
            var existingAssignment = await _context.QuizAssignments
                .FirstOrDefaultAsync(qa => qa.QuizId == quiz.Id && qa.StudentId == studentId);

            if (existingAssignment == null)
            {
                _context.QuizAssignments.Add(new QuizAssignment
                {
                    QuizId = quiz.Id,
                    StudentId = studentId,
                    HasJoined = true,
                    JoinedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
        }

        return RedirectToAction(nameof(Attempt), new { id = quiz.Id });
    }

    [HttpGet]
    public async Task<IActionResult> Attempt(int id)
    {
        var studentId = _userManager.GetUserId(User)!;

        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
            .Include(q => q.Settings)
            .FirstOrDefaultAsync(q => q.Id == id && q.IsPublished);

        if (quiz == null)
            return NotFound();

        var settings = quiz.Settings;

        if (settings != null)
        {
            var now = DateTime.UtcNow;
            if (settings.AvailableFrom.HasValue && now < settings.AvailableFrom.Value)
            {
                TempData["Error"] = "This quiz is not available yet.";
                return RedirectToAction(nameof(Dashboard));
            }
            if (settings.AvailableUntil.HasValue && now > settings.AvailableUntil.Value)
            {
                TempData["Error"] = "This quiz is no longer available.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        var hasAssignments = await _context.QuizAssignments.AnyAsync(qa => qa.QuizId == id);
        if (hasAssignments)
        {
            var assignment = await _context.QuizAssignments
                .FirstOrDefaultAsync(qa => qa.QuizId == id && qa.StudentId == studentId);

            if (assignment == null)
            {
                TempData["Error"] = "You are not assigned to this quiz. Use a join code to access it.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        if (settings?.OneAttemptOnly == true)
        {
            var alreadyAttempted = await _context.StudentAttempts
                .AnyAsync(a => a.QuizId == id && a.StudentId == studentId && a.Status == "Completed");
            if (alreadyAttempted)
            {
                TempData["Error"] = "You have already attempted this quiz.";
                return RedirectToAction(nameof(Attempts));
            }
        }

        var model = new AttemptQuizViewModel
        {
            QuizId = quiz.Id,
            QuizTitle = quiz.Title,
            RequiresPassword = settings?.HasPassword == true,
            Settings = settings != null ? MapSettingsToViewModel(settings) : null,
            Questions = quiz.Questions
                .OrderBy(q => q.Id)
                .Select(q => new AttemptQuestionViewModel
                {
                    QuestionId = q.Id,
                    Text = q.Text,
                    QuestionType = q.QuestionType,
                    Marks = q.Marks,
                    IsRequired = q.IsRequired,
                    Options = q.QuestionType is "MCQ" or "TrueFalse"
                        ? q.Options.OrderBy(o => o.Label).ToList()
                        : new List<Option>()
                }).ToList()
        };

        if (settings?.RandomizeQuestions == true)
        {
            var rng = new Random();
            model.Questions = model.Questions.OrderBy(_ => rng.Next()).ToList();
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Attempt(AttemptQuizViewModel model)
    {
        var studentId = _userManager.GetUserId(User)!;

        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
            .Include(q => q.Settings)
            .FirstOrDefaultAsync(q => q.Id == model.QuizId && q.IsPublished);

        if (quiz == null)
            return NotFound();

        if (!ModelState.IsValid)
        {
            foreach (var question in model.Questions)
            {
                var dbQuestion = quiz.Questions.First(q => q.Id == question.QuestionId);
                question.Options = dbQuestion.QuestionType is "MCQ" or "TrueFalse"
                    ? dbQuestion.Options.OrderBy(o => o.Label).ToList()
                    : new List<Option>();
            }
            model.QuizTitle = quiz.Title;
            model.Settings = quiz.Settings != null ? MapSettingsToViewModel(quiz.Settings) : null;
            return View(model);
        }

        var settings = quiz.Settings;
        var negativeMarking = settings?.NegativeMarking == true;
        var negativeFactor = settings?.NegativeMarkFactor ?? 0.25m;

        var attempt = new StudentAttempt
        {
            QuizId = quiz.Id,
            StudentId = studentId,
            StartedAt = DateTime.UtcNow,
            SubmittedAt = DateTime.UtcNow,
            Status = "Completed"
        };

        decimal autoMarksObtained = 0;
        decimal maxAutoMarks = 0;
        decimal maxSubjectiveMarks = 0;
        int correctCount = 0;
        int wrongCount = 0;
        bool hasSubjective = false;

        foreach (var question in quiz.Questions)
        {
            var submittedQuestion = model.Questions.FirstOrDefault(q => q.QuestionId == question.Id);

            if (question.QuestionType is "MCQ" or "TrueFalse")
            {
                maxAutoMarks += question.Marks;

                var selectedId = submittedQuestion?.SelectedOptionId;
                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);
                var isCorrect = correctOption != null && selectedId == correctOption.Id;

                if (isCorrect)
                {
                    correctCount++;
                    autoMarksObtained += question.Marks;
                }
                else if (negativeMarking && selectedId.HasValue)
                {
                    wrongCount++;
                    autoMarksObtained -= question.Marks * negativeFactor;
                }
                else if (selectedId.HasValue)
                {
                    wrongCount++;
                }

                attempt.StudentAnswers.Add(new StudentAnswer
                {
                    QuestionId = question.Id,
                    SelectedOptionId = selectedId,
                    IsCorrect = isCorrect
                });
            }
            else
            {
                hasSubjective = true;
                maxSubjectiveMarks += question.Marks;

                var textAnswer = submittedQuestion?.TextAnswer?.Trim();
                if (!string.IsNullOrEmpty(textAnswer))
                {
                    attempt.ManualAnswers.Add(new ManualAnswer
                    {
                        QuestionId = question.Id,
                        AnswerText = textAnswer,
                        IsGraded = false
                    });
                }
            }
        }

        if (autoMarksObtained < 0)
            autoMarksObtained = 0;

        var totalMaxMarks = maxAutoMarks + maxSubjectiveMarks;
        var totalObtained = autoMarksObtained;
        var percentage = totalMaxMarks > 0 ? Math.Round(totalObtained / totalMaxMarks * 100, 2) : 0;
        var passingPercentage = settings?.PassingPercentage ?? 50;

        attempt.Result = new Result
        {
            TotalQuestions = quiz.Questions.Count,
            CorrectAnswers = correctCount,
            WrongAnswers = wrongCount,
            Percentage = percentage,
            TotalScore = correctCount,
            MaxAutoMarks = maxAutoMarks,
            AutoMarksObtained = autoMarksObtained,
            MaxSubjectiveMarks = maxSubjectiveMarks,
            SubjectiveMarksObtained = 0,
            TotalMaxMarks = totalMaxMarks,
            TotalMarksObtained = totalObtained,
            IsPass = percentage >= passingPercentage,
            IsManuallyReviewed = !hasSubjective,
            CreatedAt = DateTime.UtcNow
        };

        _context.StudentAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        if (hasSubjective)
            TempData["Message"] = "Quiz submitted! Your subjective answers are pending teacher review.";

        return RedirectToAction(nameof(Result), new { id = attempt.Id });
    }

    public async Task<IActionResult> Result(int id)
    {
        var studentId = _userManager.GetUserId(User)!;

        var attempt = await _context.StudentAttempts
            .Include(a => a.Quiz)
                .ThenInclude(q => q!.Settings)
            .Include(a => a.Result)
            .Include(a => a.StudentAnswers)
                .ThenInclude(a => a.Question)
            .Include(a => a.StudentAnswers)
                .ThenInclude(a => a.SelectedOption)
            .Include(a => a.ManualAnswers)
                .ThenInclude(m => m.Question)
            .FirstOrDefaultAsync(a => a.Id == id && a.StudentId == studentId);

        if (attempt == null)
            return NotFound();

        return View(attempt);
    }

    public async Task<IActionResult> Attempts()
    {
        var studentId = _userManager.GetUserId(User)!;

        var attempts = await _context.StudentAttempts
            .Include(a => a.Quiz)
            .Include(a => a.Result)
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.SubmittedAt)
            .ToListAsync();

        return View(attempts);
    }

    public async Task<IActionResult> Notifications()
    {
        var studentId = _userManager.GetUserId(User)!;

        var notifications = await _context.StudentNotifications
            .Include(n => n.Quiz)
            .Where(n => n.StudentId == studentId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync();

        return View(notifications);
    }

    [HttpPost]
    public async Task<IActionResult> MarkNotificationRead(int id)
    {
        var studentId = _userManager.GetUserId(User)!;

        var notification = await _context.StudentNotifications
            .FirstOrDefaultAsync(n => n.Id == id && n.StudentId == studentId);

        if (notification == null)
            return NotFound();

        notification.IsRead = true;
        await _context.SaveChangesAsync();

        return Ok();
    }

    private static QuizSettingsViewModel MapSettingsToViewModel(QuizSettings s) => new()
    {
        TimeLimitMinutes = s.TimeLimitMinutes,
        PassingPercentage = s.PassingPercentage,
        RandomizeQuestions = s.RandomizeQuestions,
        RandomizeOptions = s.RandomizeOptions,
        AllowReview = s.AllowReview,
        OneAttemptOnly = s.OneAttemptOnly,
        NegativeMarking = s.NegativeMarking,
        NegativeMarkFactor = s.NegativeMarkFactor,
        ShowResultImmediately = s.ShowResultImmediately,
        RequireFullscreen = s.RequireFullscreen,
        EnableAntiCheating = s.EnableAntiCheating,
        JoinCode = s.JoinCode,
        QuizPassword = s.Password,
        HasPassword = s.HasPassword,
        AvailableFrom = s.AvailableFrom,
        AvailableUntil = s.AvailableUntil,
        MaxTabSwitches = s.MaxTabSwitches,
        MaxWindowChanges = s.MaxWindowChanges,
        MaxCopyAttempts = s.MaxCopyAttempts,
        MaxPasteAttempts = s.MaxPasteAttempts,
        MaxRightClicks = s.MaxRightClicks,
        MaxFullscreenExits = s.MaxFullscreenExits,
    };
}
