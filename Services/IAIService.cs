using AiQuiz.Models;
using AiQuiz.ViewModels;

namespace AiQuiz.Services;

public interface IAIService
{
    Task<Quiz> GenerateQuizAsync(GenerateQuizViewModel input, string teacherId);
}
