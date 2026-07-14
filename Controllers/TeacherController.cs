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
        var model = new DashboardViewModel
        {
            TotalQuizzes = await quizzes.CountAsync(),
            PublishedQuizzes = await quizzes.CountAsync(q => q.IsPublished),
            Attempts = await _context.StudentAttempts.CountAsync(a => a.Quiz != null && a.Quiz.TeacherId == teacherId)
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

        var quiz = ToQuiz(model, _userManager.GetUserId(User)!);
        _context.Quizzes.Add(quiz);
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

        return View(ToFormViewModel(quiz));
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
        _context.Options.RemoveRange(quiz.Questions.SelectMany(q => q.Options));
        _context.Questions.RemoveRange(quiz.Questions);
        quiz.Questions = model.Questions.Select(ToQuestion).ToList();

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
        Options = new List<Option>
        {
            new() { Label = "A", Text = model.OptionA, IsCorrect = model.CorrectAnswer == "A" },
            new() { Label = "B", Text = model.OptionB, IsCorrect = model.CorrectAnswer == "B" },
            new() { Label = "C", Text = model.OptionC, IsCorrect = model.CorrectAnswer == "C" },
            new() { Label = "D", Text = model.OptionD, IsCorrect = model.CorrectAnswer == "D" }
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
            OptionA = q.Options.First(o => o.Label == "A").Text,
            OptionB = q.Options.First(o => o.Label == "B").Text,
            OptionC = q.Options.First(o => o.Label == "C").Text,
            OptionD = q.Options.First(o => o.Label == "D").Text,
            CorrectAnswer = q.Options.First(o => o.IsCorrect).Label
        }).ToList()
    };
}
