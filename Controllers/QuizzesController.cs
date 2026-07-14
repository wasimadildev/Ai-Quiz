using AiQuiz.Data;
using AiQuiz.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiQuiz.Controllers;

[Authorize]
public class QuizzesController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public QuizzesController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        if (User.IsInRole("Teacher"))
        {
            var teacherId = _userManager.GetUserId(User)!;
            var quizzes = await _context.Quizzes
                .Include(q => q.Questions)
                .Include(q => q.Settings)
                .Where(q => q.TeacherId == teacherId)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
            return View(quizzes);
        }

        return RedirectToAction("JoinQuiz", "Student");
    }

    public async Task<IActionResult> Details(int id)
    {
        var userId = _userManager.GetUserId(User)!;

        if (User.IsInRole("Teacher"))
        {
            var teacherQuiz = await _context.Quizzes
                .Include(q => q.Teacher)
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .Include(q => q.Settings)
                .FirstOrDefaultAsync(q => q.Id == id && q.TeacherId == userId);

            if (teacherQuiz == null) return NotFound();
            return View(teacherQuiz);
        }

        var hasAccess = await _context.QuizAssignments
            .AnyAsync(a => a.QuizId == id && a.StudentId == userId && a.HasJoined);

        if (!hasAccess)
        {
            TempData["SuccessMessage"] = "Please join the quiz first using the Join Code.";
            return RedirectToAction("JoinQuiz", "Student");
        }

        var quiz = await _context.Quizzes
            .Include(q => q.Teacher)
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .Include(q => q.Settings)
            .FirstOrDefaultAsync(q => q.Id == id && q.IsPublished);

        if (quiz == null) return NotFound();
        return View(quiz);
    }
}
