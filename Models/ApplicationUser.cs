using Microsoft.AspNetCore.Identity;

namespace AiQuiz.Models;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;

    public ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    public ICollection<StudentAttempt> StudentAttempts { get; set; } = new List<StudentAttempt>();
}
