using AiQuiz.Data;
using AiQuiz.Models;
using AiQuiz.Services;
using AiQuiz.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiQuiz.Controllers;

[Authorize(Roles = "Teacher")]
public class TeacherController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAIService _aiService;

    public TeacherController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IAIService aiService)
    {
        _context = context;
        _userManager = userManager;
        _aiService = aiService;
    }

    public async Task<IActionResult> Dashboard()
    {
        var teacherId = _userManager.GetUserId(User)!;
        var quizzes = _context.Quizzes.Where(q => q.TeacherId == teacherId);
        var attempts = _context.StudentAttempts
            .Include(a => a.Quiz)
            .Include(a => a.Result)
            .Where(a => a.Quiz != null && a.Quiz.TeacherId == teacherId);
        var violations = _context.QuizViolations
            .Where(v => v.StudentAttempt != null && v.StudentAttempt.Quiz != null && v.StudentAttempt.Quiz.TeacherId == teacherId);
        var manualAnswers = _context.ManualAnswers
            .Where(m => m.StudentAttempt != null && m.StudentAttempt.Quiz != null && m.StudentAttempt.Quiz.TeacherId == teacherId);

        var completedAttempts = await attempts.Where(a => a.Status == "Completed").ToListAsync();

        var model = new DashboardViewModel
        {
            TotalQuizzes = await quizzes.CountAsync(),
            PublishedQuizzes = await quizzes.CountAsync(q => q.IsPublished),
            Attempts = await attempts.CountAsync(),
            AverageScore = completedAttempts.Count > 0
                ? completedAttempts.Average(a => a.Result != null ? a.Result.Percentage : 0)
                : 0,
            HighestScore = completedAttempts.Count > 0
                ? completedAttempts.Max(a => a.Result != null ? a.Result.Percentage : 0)
                : 0,
            LowestScore = completedAttempts.Count > 0
                ? completedAttempts.Min(a => a.Result != null ? a.Result.Percentage : 0)
                : 0,
            AverageCompletionTime = completedAttempts.Count > 0
                ? (decimal)completedAttempts
                    .Where(a => a.SubmittedAt.HasValue)
                    .Average(a => (a.SubmittedAt!.Value - a.StartedAt).TotalMinutes)
                : 0,
            TotalViolations = await violations.CountAsync(),
            FailedDueToCheating = await attempts.CountAsync(a => a.IsTerminated),
            PendingReviews = await manualAnswers.CountAsync(m => !m.IsGraded),
            TotalStudents = await attempts.Select(a => a.StudentId).Distinct().CountAsync()
        };

        return View(model);
    }

    public async Task<IActionResult> Quizzes()
    {
        var teacherId = _userManager.GetUserId(User)!;
        var quizzes = await _context.Quizzes
            .Include(q => q.Questions)
            .Where(q => q.TeacherId == teacherId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
        return View(quizzes);
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new QuizFormViewModel
        {
            Questions = Enumerable.Range(1, 3).Select(_ => new QuestionFormViewModel()).ToList()
        };
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(QuizFormViewModel model)
    {
        model.Questions = model.Questions.Where(q => !string.IsNullOrWhiteSpace(q.Text)).ToList();
        if (model.Questions.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Add at least one question.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var teacherId = _userManager.GetUserId(User)!;
        var quiz = ToQuiz(model, teacherId);
        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        var settings = new QuizSettings
        {
            QuizId = quiz.Id,
            TimeLimitMinutes = model.Settings.TimeLimitMinutes,
            PassingPercentage = model.Settings.PassingPercentage,
            RandomizeQuestions = model.Settings.RandomizeQuestions,
            RandomizeOptions = model.Settings.RandomizeOptions,
            AllowReview = model.Settings.AllowReview,
            OneAttemptOnly = model.Settings.OneAttemptOnly,
            NegativeMarking = model.Settings.NegativeMarking,
            NegativeMarkFactor = model.Settings.NegativeMarkFactor,
            ShowResultImmediately = model.Settings.ShowResultImmediately,
            RequireFullscreen = model.Settings.RequireFullscreen,
            EnableAntiCheating = model.Settings.EnableAntiCheating,
            JoinCode = GenerateJoinCode(),
            Password = model.Settings.HasPassword ? model.Settings.QuizPassword : null,
            HasPassword = model.Settings.HasPassword,
            AvailableFrom = model.Settings.AvailableFrom,
            AvailableUntil = model.Settings.AvailableUntil,
            MaxTabSwitches = model.Settings.MaxTabSwitches,
            MaxWindowChanges = model.Settings.MaxWindowChanges,
            MaxCopyAttempts = model.Settings.MaxCopyAttempts,
            MaxPasteAttempts = model.Settings.MaxPasteAttempts,
            MaxRightClicks = model.Settings.MaxRightClicks,
            MaxFullscreenExits = model.Settings.MaxFullscreenExits
        };
        _context.QuizSettings.Add(settings);

        if (model.AssignedStudentIds != null && model.AssignedStudentIds.Count > 0)
        {
            foreach (var studentId in model.AssignedStudentIds)
            {
                _context.QuizAssignments.Add(new QuizAssignment
                {
                    QuizId = quiz.Id,
                    StudentId = studentId
                });
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Quizzes));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var quiz = await GetTeacherQuiz(id);
        if (quiz == null)
        {
            return NotFound();
        }

        var vm = ToFormViewModel(quiz);

        var settings = await _context.QuizSettings.FirstOrDefaultAsync(s => s.QuizId == id);
        if (settings != null)
        {
            vm.Settings = new QuizSettingsViewModel
            {
                TimeLimitMinutes = settings.TimeLimitMinutes,
                PassingPercentage = settings.PassingPercentage,
                RandomizeQuestions = settings.RandomizeQuestions,
                RandomizeOptions = settings.RandomizeOptions,
                AllowReview = settings.AllowReview,
                OneAttemptOnly = settings.OneAttemptOnly,
                NegativeMarking = settings.NegativeMarking,
                NegativeMarkFactor = settings.NegativeMarkFactor,
                ShowResultImmediately = settings.ShowResultImmediately,
                RequireFullscreen = settings.RequireFullscreen,
                EnableAntiCheating = settings.EnableAntiCheating,
                JoinCode = settings.JoinCode,
                QuizPassword = settings.Password,
                HasPassword = settings.HasPassword,
                AvailableFrom = settings.AvailableFrom,
                AvailableUntil = settings.AvailableUntil,
                MaxTabSwitches = settings.MaxTabSwitches,
                MaxWindowChanges = settings.MaxWindowChanges,
                MaxCopyAttempts = settings.MaxCopyAttempts,
                MaxPasteAttempts = settings.MaxPasteAttempts,
                MaxRightClicks = settings.MaxRightClicks,
                MaxFullscreenExits = settings.MaxFullscreenExits
            };
            vm.AssignedStudentIds = await _context.QuizAssignments
                .Where(a => a.QuizId == id)
                .Select(a => a.StudentId)
                .ToListAsync();
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, QuizFormViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        model.Questions = model.Questions.Where(q => !string.IsNullOrWhiteSpace(q.Text)).ToList();
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var quiz = await GetTeacherQuiz(id);
        if (quiz == null)
        {
            return NotFound();
        }

        quiz.Title = model.Title;
        quiz.Subject = model.Subject;
        quiz.Topic = model.Topic;
        quiz.Difficulty = model.Difficulty;
        quiz.IsPublished = model.IsPublished;

        var questionIds = quiz.Questions.Select(q => q.Id).ToList();
        var optionIds = quiz.Questions.SelectMany(q => q.Options).Select(o => o.Id).ToList();

        var studentAnswers = _context.StudentAnswers.Where(sa => questionIds.Contains(sa.QuestionId) || (sa.SelectedOptionId.HasValue && optionIds.Contains(sa.SelectedOptionId.Value)));
        _context.StudentAnswers.RemoveRange(studentAnswers);

        var manualAnswers = _context.ManualAnswers.Where(ma => questionIds.Contains(ma.QuestionId));
        _context.ManualAnswers.RemoveRange(manualAnswers);

        _context.Options.RemoveRange(quiz.Questions.SelectMany(q => q.Options));
        _context.Questions.RemoveRange(quiz.Questions);
        quiz.Questions = model.Questions.Select(ToQuestion).ToList();

        var existingSettings = await _context.QuizSettings.FirstOrDefaultAsync(s => s.QuizId == id);
        if (existingSettings != null)
        {
            existingSettings.TimeLimitMinutes = model.Settings.TimeLimitMinutes;
            existingSettings.PassingPercentage = model.Settings.PassingPercentage;
            existingSettings.RandomizeQuestions = model.Settings.RandomizeQuestions;
            existingSettings.RandomizeOptions = model.Settings.RandomizeOptions;
            existingSettings.AllowReview = model.Settings.AllowReview;
            existingSettings.OneAttemptOnly = model.Settings.OneAttemptOnly;
            existingSettings.NegativeMarking = model.Settings.NegativeMarking;
            existingSettings.NegativeMarkFactor = model.Settings.NegativeMarkFactor;
            existingSettings.ShowResultImmediately = model.Settings.ShowResultImmediately;
            existingSettings.RequireFullscreen = model.Settings.RequireFullscreen;
            existingSettings.EnableAntiCheating = model.Settings.EnableAntiCheating;
            existingSettings.Password = model.Settings.HasPassword ? model.Settings.QuizPassword : null;
            existingSettings.HasPassword = model.Settings.HasPassword;
            existingSettings.AvailableFrom = model.Settings.AvailableFrom;
            existingSettings.AvailableUntil = model.Settings.AvailableUntil;
            existingSettings.MaxTabSwitches = model.Settings.MaxTabSwitches;
            existingSettings.MaxWindowChanges = model.Settings.MaxWindowChanges;
            existingSettings.MaxCopyAttempts = model.Settings.MaxCopyAttempts;
            existingSettings.MaxPasteAttempts = model.Settings.MaxPasteAttempts;
            existingSettings.MaxRightClicks = model.Settings.MaxRightClicks;
            existingSettings.MaxFullscreenExits = model.Settings.MaxFullscreenExits;
        }
        else
        {
            _context.QuizSettings.Add(new QuizSettings
            {
                QuizId = id,
                TimeLimitMinutes = model.Settings.TimeLimitMinutes,
                PassingPercentage = model.Settings.PassingPercentage,
                RandomizeQuestions = model.Settings.RandomizeQuestions,
                RandomizeOptions = model.Settings.RandomizeOptions,
                AllowReview = model.Settings.AllowReview,
                OneAttemptOnly = model.Settings.OneAttemptOnly,
                NegativeMarking = model.Settings.NegativeMarking,
                NegativeMarkFactor = model.Settings.NegativeMarkFactor,
                ShowResultImmediately = model.Settings.ShowResultImmediately,
                RequireFullscreen = model.Settings.RequireFullscreen,
                EnableAntiCheating = model.Settings.EnableAntiCheating,
                JoinCode = GenerateJoinCode(),
                Password = model.Settings.HasPassword ? model.Settings.QuizPassword : null,
                HasPassword = model.Settings.HasPassword,
                AvailableFrom = model.Settings.AvailableFrom,
                AvailableUntil = model.Settings.AvailableUntil,
                MaxTabSwitches = model.Settings.MaxTabSwitches,
                MaxWindowChanges = model.Settings.MaxWindowChanges,
                MaxCopyAttempts = model.Settings.MaxCopyAttempts,
                MaxPasteAttempts = model.Settings.MaxPasteAttempts,
                MaxRightClicks = model.Settings.MaxRightClicks,
                MaxFullscreenExits = model.Settings.MaxFullscreenExits
            });
        }

        var existingAssignments = _context.QuizAssignments.Where(a => a.QuizId == id);
        _context.QuizAssignments.RemoveRange(existingAssignments);

        if (model.AssignedStudentIds != null && model.AssignedStudentIds.Count > 0)
        {
            foreach (var studentId in model.AssignedStudentIds)
            {
                _context.QuizAssignments.Add(new QuizAssignment
                {
                    QuizId = id,
                    StudentId = studentId
                });
            }
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Quizzes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var quiz = await GetTeacherQuiz(id);
        if (quiz == null)
        {
            return NotFound();
        }

        var questionIds = quiz.Questions.Select(q => q.Id).ToList();
        var optionIds = quiz.Questions.SelectMany(q => q.Options).Select(o => o.Id).ToList();
        var attemptIds = _context.StudentAttempts.Where(a => a.QuizId == id).Select(a => a.Id).ToList();

        _context.StudentAnswers.Where(sa => attemptIds.Contains(sa.StudentAttemptId)).ToList();
        var studentAnswers = _context.StudentAnswers.Where(sa => attemptIds.Contains(sa.StudentAttemptId));
        _context.StudentAnswers.RemoveRange(studentAnswers);

        _context.ManualAnswers.Where(ma => attemptIds.Contains(ma.StudentAttemptId)).ToList();
        var manualAnswers = _context.ManualAnswers.Where(ma => attemptIds.Contains(ma.StudentAttemptId));
        _context.ManualAnswers.RemoveRange(manualAnswers);

        _context.QuizViolations.Where(v => attemptIds.Contains(v.StudentAttemptId)).ToList();
        var violations = _context.QuizViolations.Where(v => attemptIds.Contains(v.StudentAttemptId));
        _context.QuizViolations.RemoveRange(violations);

        _context.Results.Where(r => attemptIds.Contains(r.StudentAttemptId)).ToList();
        var results = _context.Results.Where(r => attemptIds.Contains(r.StudentAttemptId));
        _context.Results.RemoveRange(results);

        _context.StudentAttempts.RemoveRange(_context.StudentAttempts.Where(a => a.QuizId == id));

        _context.Quizzes.Remove(quiz);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Quizzes));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(int id)
    {
        var quiz = await GetTeacherQuiz(id);
        if (quiz == null)
        {
            return NotFound();
        }

        quiz.IsPublished = true;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Quizzes));
    }

    [HttpGet]
    public IActionResult Generate() => View(new GenerateQuizViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(GenerateQuizViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var quiz = await _aiService.GenerateQuizAsync(model, _userManager.GetUserId(User)!);
            TempData["SuccessMessage"] = "Quiz generated successfully. Review it before publishing.";
            return RedirectToAction(nameof(Edit), new { id = quiz.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(model);
        }
    }

    public async Task<IActionResult> Monitor(int id)
    {
        var teacherId = _userManager.GetUserId(User)!;
        var quiz = await _context.Quizzes
            .FirstOrDefaultAsync(q => q.Id == id && q.TeacherId == teacherId);
        if (quiz == null)
        {
            return NotFound();
        }

        var entries = await _context.StudentAttempts
            .Include(a => a.Student)
            .Include(a => a.Quiz)
            .Include(a => a.Violations)
            .Where(a => a.QuizId == id)
            .OrderByDescending(a => a.StartedAt)
            .Select(a => new MonitoringEntryViewModel
            {
                AttemptId = a.Id,
                QuizId = a.QuizId,
                StudentId = a.StudentId,
                StudentName = a.Student != null ? a.Student.FullName : "Unknown",
                RollNumber = a.Student != null ? a.Student.Email : null,
                QuizTitle = a.Quiz != null ? a.Quiz.Title : string.Empty,
                StartTime = a.StartedAt,
                EndTime = a.SubmittedAt,
                TotalTime = a.SubmittedAt.HasValue
                    ? $"{(a.SubmittedAt.Value - a.StartedAt).TotalMinutes:F1} min"
                    : "In Progress",
                Score = a.Result != null ? a.Result.TotalMarksObtained : 0,
                MaxScore = a.Result != null ? a.Result.TotalMaxMarks : 0,
                Status = a.Status,
                ViolationCount = a.Violations.Count
            })
            .ToListAsync();

        var model = new MonitoringViewModel
        {
            QuizId = id,
            QuizTitle = quiz.Title,
            Entries = entries
        };

        return View(model);
    }

    public async Task<IActionResult> MonitorDetail(int attemptId)
    {
        var attempt = await _context.StudentAttempts
            .Include(a => a.Student)
            .Include(a => a.Quiz)
            .Include(a => a.Violations)
            .Include(a => a.StudentAnswers)
                .ThenInclude(sa => sa.Question)
            .Include(a => a.StudentAnswers)
                .ThenInclude(sa => sa.SelectedOption)
            .Include(a => a.ManualAnswers)
                .ThenInclude(ma => ma.Question)
            .FirstOrDefaultAsync(a => a.Id == attemptId);

        if (attempt == null || attempt.Quiz == null || attempt.Quiz.TeacherId != _userManager.GetUserId(User))
        {
            return NotFound();
        }

        var answers = attempt.StudentAnswers
            .Where(sa => sa.Question != null)
            .Select(sa => new AnswerDetailViewModel
            {
                QuestionText = sa.Question!.Text,
                SelectedAnswer = sa.SelectedOption != null ? $"{sa.SelectedOption.Label}: {sa.SelectedOption.Text}" : null,
                CorrectAnswer = sa.Question.Options.FirstOrDefault(o => o.IsCorrect) != null
                    ? $"{sa.Question.Options.First(o => o.IsCorrect).Label}: {sa.Question.Options.First(o => o.IsCorrect).Text}"
                    : null,
                IsCorrect = sa.IsCorrect,
                Marks = sa.Question.Marks,
                QuestionType = sa.Question.QuestionType
            })
            .ToList();

        var manualAnswers = attempt.ManualAnswers
            .Where(ma => ma.Question != null)
            .Select(ma => new ManualAnswerDetailViewModel
            {
                ManualAnswerId = ma.Id,
                QuestionText = ma.Question!.Text,
                StudentAnswer = ma.AnswerText,
                MaxMarks = ma.Question.Marks,
                AwardedMarks = ma.AwardedMarks,
                TeacherFeedback = ma.TeacherFeedback,
                IsGraded = ma.IsGraded
            })
            .ToList();

        var violations = attempt.Violations
            .Select(v => new ViolationDetailViewModel
            {
                EventType = v.EventType,
                OccurredAt = v.OccurredAt,
                Count = v.Count,
                Details = v.Details
            })
            .ToList();

        var entry = new MonitoringEntryViewModel
        {
            AttemptId = attempt.Id,
            QuizId = attempt.QuizId,
            StudentId = attempt.StudentId,
            StudentName = attempt.Student != null ? attempt.Student.FullName : "Unknown",
            RollNumber = attempt.Student != null ? attempt.Student.Email : null,
            QuizTitle = attempt.Quiz.Title,
            StartTime = attempt.StartedAt,
            EndTime = attempt.SubmittedAt,
            TotalTime = attempt.SubmittedAt.HasValue
                ? $"{(attempt.SubmittedAt.Value - attempt.StartedAt).TotalMinutes:F1} min"
                : "In Progress",
            Score = attempt.Result != null ? attempt.Result.TotalMarksObtained : 0,
            MaxScore = attempt.Result != null ? attempt.Result.TotalMaxMarks : 0,
            Status = attempt.Status,
            ViolationCount = attempt.Violations.Count
        };

        var model = new MonitoringDetailViewModel
        {
            Entry = entry,
            Answers = answers,
            ManualAnswers = manualAnswers,
            Violations = violations
        };

        return View(model);
    }

    public async Task<IActionResult> ReviewDashboard()
    {
        var teacherId = _userManager.GetUserId(User)!;

        var entries = await _context.ManualAnswers
            .Include(ma => ma.StudentAttempt)
                .ThenInclude(a => a.Quiz)
            .Include(ma => ma.StudentAttempt)
                .ThenInclude(a => a.Student)
            .Where(ma => ma.StudentAttempt != null
                && ma.StudentAttempt.Quiz != null
                && ma.StudentAttempt.Quiz.TeacherId == teacherId
                && !ma.IsGraded)
            .GroupBy(ma => ma.StudentAttemptId)
            .Select(g => new ReviewEntryViewModel
            {
                AttemptId = g.Key,
                StudentName = g.First().StudentAttempt != null && g.First().StudentAttempt!.Student != null
                    ? g.First().StudentAttempt!.Student!.FullName
                    : "Unknown",
                QuizTitle = g.First().StudentAttempt != null && g.First().StudentAttempt!.Quiz != null
                    ? g.First().StudentAttempt!.Quiz!.Title
                    : string.Empty,
                PendingCount = g.Count(),
                TotalSubjective = g.Count(),
                Status = "Pending"
            })
            .ToListAsync();

        var model = new ReviewDashboardViewModel
        {
            Entries = entries
        };

        return View(model);
    }

    public async Task<IActionResult> Review(int attemptId)
    {
        var teacherId = _userManager.GetUserId(User)!;

        var attempt = await _context.StudentAttempts
            .Include(a => a.Student)
            .Include(a => a.Quiz)
            .Include(a => a.ManualAnswers)
                .ThenInclude(ma => ma.Question)
            .FirstOrDefaultAsync(a => a.Id == attemptId
                && a.Quiz != null
                && a.Quiz.TeacherId == teacherId);

        if (attempt == null)
        {
            return NotFound();
        }

        var model = attempt.ManualAnswers
            .Where(ma => ma.Question != null)
            .Select(ma => new GradeViewModel
            {
                ManualAnswerId = ma.Id,
                QuestionText = ma.Question!.Text,
                StudentAnswer = ma.AnswerText,
                MaxMarks = ma.Question.Marks,
                AwardedMarks = ma.AwardedMarks ?? 0,
                TeacherFeedback = ma.TeacherFeedback
            })
            .ToList();

        ViewBag.AttemptId = attemptId;
        ViewBag.StudentName = attempt.Student?.FullName ?? "Unknown";
        ViewBag.QuizTitle = attempt.Quiz?.Title ?? string.Empty;

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Grade(GradeViewModel model, int attemptId)
    {
        var teacherId = _userManager.GetUserId(User)!;

        var manualAnswer = await _context.ManualAnswers
            .Include(ma => ma.StudentAttempt)
                .ThenInclude(a => a.Quiz)
            .FirstOrDefaultAsync(ma => ma.Id == model.ManualAnswerId
                && ma.StudentAttempt != null
                && ma.StudentAttempt.Quiz != null
                && ma.StudentAttempt.Quiz.TeacherId == teacherId
                && ma.StudentAttemptId == attemptId);

        if (manualAnswer == null)
        {
            return NotFound();
        }

        manualAnswer.AwardedMarks = model.AwardedMarks;
        manualAnswer.TeacherFeedback = model.TeacherFeedback;
        manualAnswer.IsGraded = true;
        manualAnswer.GradedAt = DateTime.UtcNow;
        manualAnswer.GradedById = teacherId;

        await _context.SaveChangesAsync();

        var pendingCount = await _context.ManualAnswers
            .CountAsync(ma => ma.StudentAttemptId == attemptId && !ma.IsGraded);

        if (pendingCount == 0)
        {
            var attempt = await _context.StudentAttempts
                .Include(a => a.StudentAnswers)
                    .ThenInclude(sa => sa.Question)
                .Include(a => a.ManualAnswers)
                .Include(a => a.Quiz)
                .Include(a => a.Result)
                .FirstOrDefaultAsync(a => a.Id == attemptId);

            if (attempt != null)
            {
                var subjectiveObtained = attempt.ManualAnswers
                    .Where(ma => ma.IsGraded && ma.AwardedMarks.HasValue)
                    .Sum(ma => ma.AwardedMarks!.Value);
                var subjectiveMax = attempt.ManualAnswers
                    .Where(ma => ma.Question != null)
                    .Sum(ma => (decimal)ma.Question!.Marks);
                var autoObtained = attempt.StudentAnswers
                    .Where(sa => sa.IsCorrect && sa.Question != null)
                    .Sum(sa => (decimal)sa.Question!.Marks);
                var autoMax = attempt.StudentAnswers
                    .Where(sa => sa.Question != null)
                    .Sum(sa => (decimal)sa.Question!.Marks);
                var totalObtained = autoObtained + subjectiveObtained;
                var totalMax = autoMax + subjectiveMax;
                var percentage = totalMax > 0 ? Math.Round(totalObtained / totalMax * 100m, 2) : 0;

                var settings = attempt.Quiz != null
                    ? await _context.QuizSettings.FirstOrDefaultAsync(s => s.QuizId == attempt.QuizId)
                    : null;
                var passPercentage = settings?.PassingPercentage ?? 50;

                if (attempt.Result != null)
                {
                    attempt.Result.SubjectiveMarksObtained = subjectiveObtained;
                    attempt.Result.TotalMarksObtained = totalObtained;
                    attempt.Result.TotalMaxMarks = totalMax;
                    attempt.Result.Percentage = percentage;
                    attempt.Result.IsPass = percentage >= passPercentage;
                    attempt.Result.IsManuallyReviewed = true;
                }
                else
                {
                    _context.Results.Add(new Result
                    {
                        StudentAttemptId = attemptId,
                        TotalQuestions = attempt.StudentAnswers.Count,
                        CorrectAnswers = attempt.StudentAnswers.Count(sa => sa.IsCorrect),
                        WrongAnswers = attempt.StudentAnswers.Count(sa => !sa.IsCorrect),
                        Percentage = percentage,
                        TotalScore = (int)totalObtained,
                        MaxAutoMarks = autoObtained,
                        AutoMarksObtained = autoObtained,
                        MaxSubjectiveMarks = subjectiveMax,
                        SubjectiveMarksObtained = subjectiveObtained,
                        TotalMaxMarks = totalMax,
                        TotalMarksObtained = totalObtained,
                        IsPass = percentage >= passPercentage,
                        IsManuallyReviewed = true
                    });
                }

                await _context.SaveChangesAsync();
            }
        }

        return RedirectToAction(nameof(Review), new { attemptId });
    }

    public IActionResult Students()
    {
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> SearchStudents(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Json(new List<object>());
        }

        var students = await _userManager.Users
            .Where(u => u.NormalizedUserName != null
                && (u.FullName.Contains(query)
                    || u.Email!.Contains(query)
                    || u.Id.Contains(query)))
            .Take(20)
            .Select(u => new
            {
                id = u.Id,
                fullName = u.FullName,
                email = u.Email
            })
            .ToListAsync();

        return Json(students);
    }

    private async Task<Quiz?> GetTeacherQuiz(int id)
    {
        var teacherId = _userManager.GetUserId(User)!;
        return await _context.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id && q.TeacherId == teacherId);
    }

    private static Quiz ToQuiz(QuizFormViewModel model, string teacherId) => new()
    {
        Title = model.Title,
        Subject = model.Subject,
        Topic = model.Topic,
        Difficulty = model.Difficulty,
        IsPublished = model.IsPublished,
        TeacherId = teacherId,
        Questions = model.Questions.Select(ToQuestion).ToList()
    };

    private static Question ToQuestion(QuestionFormViewModel model) => new()
    {
        Text = model.Text,
        Explanation = model.Explanation,
        QuestionType = model.QuestionType,
        Marks = model.Marks,
        IsRequired = model.IsRequired,
        Options = new List<Option>
        {
            new() { Label = "A", Text = model.OptionA ?? string.Empty, IsCorrect = model.CorrectAnswer == "A" },
            new() { Label = "B", Text = model.OptionB ?? string.Empty, IsCorrect = model.CorrectAnswer == "B" },
            new() { Label = "C", Text = model.OptionC ?? string.Empty, IsCorrect = model.CorrectAnswer == "C" },
            new() { Label = "D", Text = model.OptionD ?? string.Empty, IsCorrect = model.CorrectAnswer == "D" }
        }
    };

    private static QuizFormViewModel ToFormViewModel(Quiz quiz) => new()
    {
        Id = quiz.Id,
        Title = quiz.Title,
        Subject = quiz.Subject,
        Topic = quiz.Topic,
        Difficulty = quiz.Difficulty,
        IsPublished = quiz.IsPublished,
        Questions = quiz.Questions.Select(q => new QuestionFormViewModel
        {
            Id = q.Id,
            Text = q.Text,
            Explanation = q.Explanation,
            QuestionType = q.QuestionType,
            Marks = q.Marks,
            IsRequired = q.IsRequired,
            OptionA = q.Options.FirstOrDefault(o => o.Label == "A")?.Text,
            OptionB = q.Options.FirstOrDefault(o => o.Label == "B")?.Text,
            OptionC = q.Options.FirstOrDefault(o => o.Label == "C")?.Text,
            OptionD = q.Options.FirstOrDefault(o => o.Label == "D")?.Text,
            CorrectAnswer = q.Options.FirstOrDefault(o => o.IsCorrect)?.Label ?? "A"
        }).ToList()
    };

    private static string GenerateJoinCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
