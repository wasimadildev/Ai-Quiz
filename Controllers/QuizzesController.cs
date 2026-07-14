using AiQuiz.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AiQuiz.Controllers;

public class QuizzesController : Controller
{
    private readonly ApplicationDbContext _context;

    public QuizzesController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var quizzes = await _context.Quizzes
            .Include(q => q.Questions)
            .Where(q => q.IsPublished)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();
        return View(quizzes);
    }

    public async Task<IActionResult> Details(int id)
    {
        var quiz = await _context.Quizzes
            .Include(q => q.Teacher)
            .Include(q => q.Questions)
            .ThenInclude(q => q.Options)
            .FirstOrDefaultAsync(q => q.Id == id && q.IsPublished);

        if (quiz == null)
        {
            return NotFound();
        }

        return View(quiz);
    }
}
