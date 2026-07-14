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
        var model = new DashboardViewModel
        {
            PublishedQuizzes = await _context.Quizzes.CountAsync(q => q.IsPublished),
            Attempts = await _context.StudentAttempts.CountAsync(a => a.StudentId == studentId)
        };
        return View(model);
    }

    public async Task<IActionResult> AvailableQuizzes()
    {
        var quizzes = await _context.Quizzes
            .Include(q => q.Questions)
            .Where(q => q.IsPublished)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
        return View(quizzes);
    }

    [HttpGet]
    public async Task<IActionResult> Attempt(int id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id && q.IsPublished);

        if (quiz == null)
        {
            return NotFound();
        }

        var model = new AttemptQuizViewModel
        {
            QuizId = quiz.Id,
            QuizTitle = quiz.Title,
            Questions = quiz.Questions.Select(q => new AttemptQuestionViewModel
            {
                QuestionId = q.Id,
                Text = q.Text,
                Options = q.Options.OrderBy(o => o.Label).ToList()
            }).ToList()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Attempt(AttemptQuizViewModel model)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == model.QuizId && q.IsPublished);

        if (quiz == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            foreach (var question in model.Questions)
            {
                question.Options = quiz.Questions.First(q => q.Id == question.QuestionId).Options.OrderBy(o => o.Label).ToList();
            }
            model.QuizTitle = quiz.Title;
            return View(model);
        }

        var attempt = new StudentAttempt
        {
            QuizId = quiz.Id,
            StudentId = _userManager.GetUserId(User)!,
            SubmittedAt = DateTime.UtcNow
        };

        var correct = 0;
        foreach (var question in quiz.Questions)
        {
            var selectedId = model.Questions.FirstOrDefault(q => q.QuestionId == question.Id)?.SelectedOptionId;
            var selectedOption = question.Options.FirstOrDefault(o => o.Id == selectedId);
            var isCorrect = selectedOption?.IsCorrect == true;
            if (isCorrect)
            {
                correct++;
            }

            attempt.StudentAnswers.Add(new StudentAnswer
            {
                QuestionId = question.Id,
                SelectedOptionId = selectedId,
                IsCorrect = isCorrect
            });
        }

        var total = quiz.Questions.Count;
        attempt.Result = new Result
        {
            TotalQuestions = total,
            CorrectAnswers = correct,
            WrongAnswers = total - correct,
            Percentage = total == 0 ? 0 : Math.Round((decimal)correct / total * 100, 2),
            TotalScore = correct
        };

        _context.StudentAttempts.Add(attempt);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Result), new { id = attempt.Id });
    }

    public async Task<IActionResult> Result(int id)
    {
        var studentId = _userManager.GetUserId(User)!;
        var attempt = await _context.StudentAttempts
            .Include(a => a.Quiz)
            .Include(a => a.Result)
            .Include(a => a.StudentAnswers)
            .ThenInclude(a => a.Question)
            .Include(a => a.StudentAnswers)
            .ThenInclude(a => a.SelectedOption)
            .FirstOrDefaultAsync(a => a.Id == id && a.StudentId == studentId);

        if (attempt == null)
        {
            return NotFound();
        }

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
}
