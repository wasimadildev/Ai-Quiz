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
        var apiKey = _configuration["Gemini:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API key is missing. Add it to appsettings.json.");
        }

        var generated = await RequestQuizFromGeminiAsync(input, apiKey);

        var quiz = new Quiz
        {
            Title = input.Title,
            Subject = input.Subject,
            Topic = input.Topic,
            Difficulty = input.Difficulty,
            IsPublished = false,
            TeacherId = teacherId,
            Questions = generated.Questions.Select(q => BuildQuestion(q, input.QuestionType)).ToList()
        };

        _context.Quizzes.Add(quiz);
        await _context.SaveChangesAsync();

        return quiz;
    }

    private static Question BuildQuestion(GeneratedQuestionDto q, string questionType)
    {
        var question = new Question
        {
            Text = q.Question,
            Explanation = q.Explanation,
            QuestionType = questionType,
            Marks = 1,
            IsRequired = true
        };

        switch (questionType)
        {
            case "TrueFalse":
                question.Options = new List<Option>
                {
                    new() { Label = "A", Text = "True", IsCorrect = q.CorrectAnswer.Equals("A", StringComparison.OrdinalIgnoreCase) },
                    new() { Label = "B", Text = "False", IsCorrect = q.CorrectAnswer.Equals("B", StringComparison.OrdinalIgnoreCase) }
                };
                break;

            case "MCQ":
                question.Options = new List<Option>
                {
                    new() { Label = "A", Text = q.OptionA ?? "", IsCorrect = q.CorrectAnswer.Equals("A", StringComparison.OrdinalIgnoreCase) },
                    new() { Label = "B", Text = q.OptionB ?? "", IsCorrect = q.CorrectAnswer.Equals("B", StringComparison.OrdinalIgnoreCase) },
                    new() { Label = "C", Text = q.OptionC ?? "", IsCorrect = q.CorrectAnswer.Equals("C", StringComparison.OrdinalIgnoreCase) },
                    new() { Label = "D", Text = q.OptionD ?? "", IsCorrect = q.CorrectAnswer.Equals("D", StringComparison.OrdinalIgnoreCase) }
                };
                break;

            case "ShortAnswer":
            case "LongAnswer":
                question.Options = new List<Option>();
                break;
        }

        return question;
    }

    private async Task<GeneratedQuizDto> RequestQuizFromGeminiAsync(GenerateQuizViewModel input, string apiKey)
    {
        var model = _configuration["Gemini:Model"] ?? "gemini-2.0-flash";
        var endpoint = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var (systemPrompt, userPrompt) = BuildPrompts(input);

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[] { new { text = systemPrompt } },
                    role = "user"
                },
                new
                {
                    parts = new[] { new { text = userPrompt } },
                    role = "user"
                }
            },
            generationConfig = new
            {
                temperature = 0.4,
                responseMimeType = "application/json"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        using var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Gemini request failed ({response.StatusCode}): {content}");
        }

        using var document = JsonDocument.Parse(content);
        var text = document.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? string.Empty;

        text = text.Trim();
        if (text.StartsWith("```"))
        {
            text = text.Replace("```json", "").Replace("```", "").Trim();
        }

        var result = JsonSerializer.Deserialize<GeneratedQuizDto>(text, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (result?.Questions == null || result.Questions.Count == 0)
        {
            throw new InvalidOperationException("Gemini did not return quiz questions. Response: " + text);
        }

        return result;
    }

    private static (string systemPrompt, string userPrompt) BuildPrompts(GenerateQuizViewModel input)
    {
        var systemPrompt = "You create clear university-level quiz questions. Return valid JSON only, no markdown formatting.";

        switch (input.QuestionType)
        {
            case "TrueFalse":
                return (systemPrompt + " Generate True/False questions where the answer is either True or False.",
                    string.Join(Environment.NewLine, new[]
                    {
                        $"Generate {input.NumberOfQuestions} {input.Difficulty} True/False questions for a university quiz.",
                        $"Subject: {input.Subject}",
                        $"Topic: {input.Topic}",
                        "Each question MUST be a true/false statement.",
                        "Return ONLY valid JSON in this exact shape:",
                        "{\"questions\":[{\"question\":\"The capital of France is Paris.\",\"correctAnswer\":\"A\",\"explanation\":\"...\"}]}",
                        "correctAnswer must be exactly A for True or B for False.",
                        "Do NOT include optionA or optionB fields, they are always A=True, B=False.",
                        "Do not include any text before or after the JSON."
                    }));

            case "ShortAnswer":
                return (systemPrompt + " Generate short answer questions that require brief text responses (1-3 sentences).",
                    string.Join(Environment.NewLine, new[]
                    {
                        $"Generate {input.NumberOfQuestions} {input.Difficulty} short answer questions for a university quiz.",
                        $"Subject: {input.Subject}",
                        $"Topic: {input.Topic}",
                        "Each question should require a brief text answer (1-3 sentences).",
                        "Return ONLY valid JSON in this exact shape:",
                        "{\"questions\":[{\"question\":\"Define what a binary search tree is.\",\"correctAnswer\":\"A binary search tree is a binary tree where each node has at most two children, with left child < parent < right child.\",\"explanation\":\"...\"}]}",
                        "In this format, correctAnswer contains the model/sample answer text, NOT a letter.",
                        "Do not include any optionA/optionB/optionC/optionD fields.",
                        "Do not include any text before or after the JSON."
                    }));

            case "LongAnswer":
                return (systemPrompt + " Generate long answer (essay) questions that require detailed explanations.",
                    string.Join(Environment.NewLine, new[]
                    {
                        $"Generate {input.NumberOfQuestions} {input.Difficulty} essay questions for a university quiz.",
                        $"Subject: {input.Subject}",
                        $"Topic: {input.Topic}",
                        "Each question should require a detailed explanation or essay response.",
                        "Return ONLY valid JSON in this exact shape:",
                        "{\"questions\":[{\"question\":\"Explain the process of normalization in database design with examples.\",\"correctAnswer\":\"Normalization is the process of organizing data to reduce redundancy...\",\"explanation\":\"...\"}]}",
                        "In this format, correctAnswer contains the model/sample answer text, NOT a letter.",
                        "Do not include any optionA/optionB/optionC/optionD fields.",
                        "Do not include any text before or after the JSON."
                    }));

            default: // MCQ
                return (systemPrompt + " Generate multiple choice questions with 4 options (A, B, C, D).",
                    string.Join(Environment.NewLine, new[]
                    {
                        $"Generate {input.NumberOfQuestions} {input.Difficulty} MCQ questions for a university quiz.",
                        $"Subject: {input.Subject}",
                        $"Topic: {input.Topic}",
                        "IMPORTANT: Each question MUST have non-empty optionA, optionB, optionC, and optionD fields with actual answer text.",
                        "Return ONLY valid JSON in this exact shape:",
                        "{\"questions\":[{\"question\":\"What is 2+2?\",\"optionA\":\"3\",\"optionB\":\"4\",\"optionC\":\"5\",\"optionD\":\"6\",\"correctAnswer\":\"B\",\"explanation\":\"2+2 equals 4.\"}]}",
                        "correctAnswer must be exactly A, B, C, or D.",
                        "Do NOT return empty strings for any option. Each option must have real answer text.",
                        "Do not include any text before or after the JSON."
                    }));
        }
    }

    private sealed class GeneratedQuizDto
    {
        public List<GeneratedQuestionDto> Questions { get; set; } = new();
    }

    private sealed class GeneratedQuestionDto
    {
        public string Question { get; set; } = string.Empty;
        public string? OptionA { get; set; }
        public string? OptionB { get; set; }
        public string? OptionC { get; set; }
        public string? OptionD { get; set; }
        public string CorrectAnswer { get; set; } = "A";
        public string? Explanation { get; set; }
    }
}
