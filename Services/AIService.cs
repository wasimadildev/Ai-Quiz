using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AiQuiz.Data;
using AiQuiz.Models;
using AiQuiz.ViewModels;

namespace AiQuiz.Services;

public class AIService : IAIService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public AIService(ApplicationDbContext context, IConfiguration configuration, HttpClient httpClient)
    {
        _context = context;
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<Quiz> GenerateQuizAsync(GenerateQuizViewModel input, string teacherId)
    {
        var apiKey = _configuration["OpenAI:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("OpenAI API key is missing. Add it to appsettings.json or user secrets.");
        }

        var generated = await RequestQuizFromOpenAIAsync(input, apiKey);

        var quiz = new Quiz
        {
            Title = input.Title,
            Subject = input.Subject,
            Topic = input.Topic,
            Difficulty = input.Difficulty,
            IsPublished = false,
            TeacherId = teacherId,
            Questions = generated.Questions.Select(q => new Question
            {
                Text = q.Question,
                Explanation = q.Explanation,
                Options = new List<Option>
                {
                    new() { Label = "A", Text = q.OptionA, IsCorrect = q.CorrectAnswer.Equals("A", StringComparison.OrdinalIgnoreCase) },
                    new() { Label = "B", Text = q.OptionB, IsCorrect = q.CorrectAnswer.Equals("B", StringComparison.OrdinalIgnoreCase) },
                    new() { Label = "C", Text = q.OptionC, IsCorrect = q.CorrectAnswer.Equals("C", StringComparison.OrdinalIgnoreCase) },
                    new() { Label = "D", Text = q.OptionD, IsCorrect = q.CorrectAnswer.Equals("D", StringComparison.OrdinalIgnoreCase) }
                }
            }).ToList()
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        return quiz;
    }

    private async Task<GeneratedQuizDto> RequestQuizFromOpenAIAsync(GenerateQuizViewModel input, string apiKey)
    {
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        var prompt = string.Join(Environment.NewLine, new[]
        {
            $"Generate {input.NumberOfQuestions} {input.Difficulty} MCQ questions for a university quiz.",
            $"Subject: {input.Subject}",
            $"Topic: {input.Topic}",
            "Return JSON only in this shape:",
            "{\"questions\":[{\"question\":\"...\",\"optionA\":\"...\",\"optionB\":\"...\",\"optionC\":\"...\",\"optionD\":\"...\",\"correctAnswer\":\"A\",\"explanation\":\"...\"}]}",
            "correctAnswer must be exactly A, B, C, or D."
        });

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = "You create clear university-level multiple choice quiz questions and return valid JSON only." },
                new { role = "user", content = prompt }
            },
            response_format = new { type = "json_object" },
            temperature = 0.4
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI request failed: {response.StatusCode}");
        }

        using var document = JsonDocument.Parse(content);
        var json = document.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

        var result = JsonSerializer.Deserialize<GeneratedQuizDto>(json ?? string.Empty, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result?.Questions == null || result.Questions.Count == 0)
        {
            throw new InvalidOperationException("OpenAI did not return quiz questions.");
        }

        return result;
    }

    private sealed class GeneratedQuizDto
    {
        public List<GeneratedQuestionDto> Questions { get; set; } = new();
    }

    private sealed class GeneratedQuestionDto
    {
        public string Question { get; set; } = string.Empty;
        public string OptionA { get; set; } = string.Empty;
        public string OptionB { get; set; } = string.Empty;
        public string OptionC { get; set; } = string.Empty;
        public string OptionD { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = "A";
        public string? Explanation { get; set; }
    }
}



