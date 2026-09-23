using Dapper;
using Microsoft.Data.Sqlite;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Data;
using System.Text;
using System.Text.Json;

namespace ReactApp1.Server.Data;

public partial class EnrollmentService : IEnrollmentService
{
    private static readonly Uri OpenRouterEndpoint = new("https://openrouter.ai/api/v1");

    private readonly string _connectionString;
    private readonly IConfiguration _configuration;

    public EnrollmentService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("UniversityDb")
            ?? throw new InvalidOperationException("Missing 'UniversityDb' connection string.");
        _configuration = configuration;

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = CreateConnection();

        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS Enrollments (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Year INTEGER NOT NULL,
                Programme TEXT NOT NULL,
                Faculty TEXT NOT NULL,
                StudentCount INTEGER NOT NULL
            );
            """);

        var enrollmentCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Enrollments");

        if (enrollmentCount == 0)
        {
            connection.Execute(
                "INSERT INTO Enrollments (Year, Programme, Faculty, StudentCount) VALUES (@Year, @Programme, @Faculty, @StudentCount);",
                new[]
                {
                    // Computer Science — Science and Engineering
                    new { Year = 2016, Programme = "Computer Science", Faculty = "Science and Engineering", StudentCount = 120 },
                    new { Year = 2017, Programme = "Computer Science", Faculty = "Science and Engineering", StudentCount = 135 },
                    new { Year = 2018, Programme = "Computer Science", Faculty = "Science and Engineering", StudentCount = 150 },
                    new { Year = 2019, Programme = "Computer Science", Faculty = "Science and Engineering", StudentCount = 168 },
                    new { Year = 2020, Programme = "Computer Science", Faculty = "Science and Engineering", StudentCount = 175 },
                    new { Year = 2021, Programme = "Computer Science", Faculty = "Science and Engineering", StudentCount = 190 },
                    new { Year = 2022, Programme = "Computer Science", Faculty = "Science and Engineering", StudentCount = 215 },
                    new { Year = 2023, Programme = "Computer Science", Faculty = "Science and Engineering", StudentCount = 240 },
                    new { Year = 2024, Programme = "Computer Science", Faculty = "Science and Engineering", StudentCount = 268 },
                    new { Year = 2025, Programme = "Computer Science", Faculty = "Science and Engineering", StudentCount = 290 },

                    // Mechanical Engineering — Science and Engineering
                    new { Year = 2016, Programme = "Mechanical Engineering", Faculty = "Science and Engineering", StudentCount = 200 },
                    new { Year = 2017, Programme = "Mechanical Engineering", Faculty = "Science and Engineering", StudentCount = 195 },
                    new { Year = 2018, Programme = "Mechanical Engineering", Faculty = "Science and Engineering", StudentCount = 190 },
                    new { Year = 2019, Programme = "Mechanical Engineering", Faculty = "Science and Engineering", StudentCount = 182 },
                    new { Year = 2020, Programme = "Mechanical Engineering", Faculty = "Science and Engineering", StudentCount = 160 },
                    new { Year = 2021, Programme = "Mechanical Engineering", Faculty = "Science and Engineering", StudentCount = 165 },
                    new { Year = 2022, Programme = "Mechanical Engineering", Faculty = "Science and Engineering", StudentCount = 172 },
                    new { Year = 2023, Programme = "Mechanical Engineering", Faculty = "Science and Engineering", StudentCount = 180 },
                    new { Year = 2024, Programme = "Mechanical Engineering", Faculty = "Science and Engineering", StudentCount = 188 },
                    new { Year = 2025, Programme = "Mechanical Engineering", Faculty = "Science and Engineering", StudentCount = 195 },

                    // Business Administration — Business
                    new { Year = 2016, Programme = "Business Administration", Faculty = "Business", StudentCount = 280 },
                    new { Year = 2017, Programme = "Business Administration", Faculty = "Business", StudentCount = 290 },
                    new { Year = 2018, Programme = "Business Administration", Faculty = "Business", StudentCount = 295 },
                    new { Year = 2019, Programme = "Business Administration", Faculty = "Business", StudentCount = 300 },
                    new { Year = 2020, Programme = "Business Administration", Faculty = "Business", StudentCount = 270 },
                    new { Year = 2021, Programme = "Business Administration", Faculty = "Business", StudentCount = 285 },
                    new { Year = 2022, Programme = "Business Administration", Faculty = "Business", StudentCount = 300 },
                    new { Year = 2023, Programme = "Business Administration", Faculty = "Business", StudentCount = 310 },
                    new { Year = 2024, Programme = "Business Administration", Faculty = "Business", StudentCount = 320 },
                    new { Year = 2025, Programme = "Business Administration", Faculty = "Business", StudentCount = 335 },

                    // Law — Humanities and Law
                    new { Year = 2016, Programme = "Law", Faculty = "Humanities and Law", StudentCount = 110 },
                    new { Year = 2017, Programme = "Law", Faculty = "Humanities and Law", StudentCount = 115 },
                    new { Year = 2018, Programme = "Law", Faculty = "Humanities and Law", StudentCount = 120 },
                    new { Year = 2019, Programme = "Law", Faculty = "Humanities and Law", StudentCount = 128 },
                    new { Year = 2020, Programme = "Law", Faculty = "Humanities and Law", StudentCount = 118 },
                    new { Year = 2021, Programme = "Law", Faculty = "Humanities and Law", StudentCount = 125 },
                    new { Year = 2022, Programme = "Law", Faculty = "Humanities and Law", StudentCount = 135 },
                    new { Year = 2023, Programme = "Law", Faculty = "Humanities and Law", StudentCount = 150 },
                    new { Year = 2024, Programme = "Law", Faculty = "Humanities and Law", StudentCount = 160 },
                    new { Year = 2025, Programme = "Law", Faculty = "Humanities and Law", StudentCount = 172 },

                    // Nursing — Health Sciences
                    new { Year = 2016, Programme = "Nursing", Faculty = "Health Sciences", StudentCount = 90 },
                    new { Year = 2017, Programme = "Nursing", Faculty = "Health Sciences", StudentCount = 100 },
                    new { Year = 2018, Programme = "Nursing", Faculty = "Health Sciences", StudentCount = 112 },
                    new { Year = 2019, Programme = "Nursing", Faculty = "Health Sciences", StudentCount = 125 },
                    new { Year = 2020, Programme = "Nursing", Faculty = "Health Sciences", StudentCount = 140 },
                    new { Year = 2021, Programme = "Nursing", Faculty = "Health Sciences", StudentCount = 160 },
                    new { Year = 2022, Programme = "Nursing", Faculty = "Health Sciences", StudentCount = 178 },
                    new { Year = 2023, Programme = "Nursing", Faculty = "Health Sciences", StudentCount = 195 },
                    new { Year = 2024, Programme = "Nursing", Faculty = "Health Sciences", StudentCount = 210 },
                    new { Year = 2025, Programme = "Nursing", Faculty = "Health Sciences", StudentCount = 225 }
                });
        }
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    private List<Enrollment> GetEnrollments()
    {
        using var connection = CreateConnection();

        return connection.Query<Enrollment>("SELECT Id, Year, Programme, Faculty, StudentCount FROM Enrollments").ToList();
    }

    private const int MaxHistoryTurns = 10;

    private static readonly BinaryData ResponseJsonSchema = BinaryData.FromBytes("""
        {
          "type": "object",
          "properties": {
            "answer": { "type": "string" },
            "chartType": { "type": "string", "enum": ["table", "line", "bar", "none"] },
            "rows": {
              "type": "array",
              "items": {
                "type": "object",
                "properties": {
                  "year": { "type": "integer" },
                  "programme": { "type": "string" },
                  "faculty": { "type": "string" },
                  "studentCount": { "type": "integer" }
                },
                "required": ["year", "programme", "faculty", "studentCount"],
                "additionalProperties": false
              }
            }
          },
          "required": ["answer", "chartType", "rows"],
          "additionalProperties": false
        }
        """u8.ToArray());

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<EnrollmentQueryResult> AnswerQuestionAsync(string question, IReadOnlyList<ChatTurn>? history)
    {
        var apiKey = _configuration["OpenRouter:ApiKey"];

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new EnrollmentQueryResult
            {
                Question = question,
                Answer = "The AI assistant is not configured. Set 'OpenRouter:ApiKey' " +
                    "(e.g. via `dotnet user-secrets set OpenRouter:ApiKey <key>` or the OPENROUTER__ApiKey environment variable) to enable natural language answers."
            };
        }

        var enrollments = GetEnrollments();
        var context = BuildContext(enrollments);
        var model = _configuration["OpenRouter:Model"] ?? "openai/gpt-4o-mini";

        var systemPrompt =
            "You are a data analyst assistant for a university enrollment database. " +
            "Answer the user's question using ONLY the enrollment records provided below (CSV: Year,Programme,Faculty,StudentCount). " +
            "Perform any counting, summing, or comparison yourself from the raw rows. " +
            "The conversation may include earlier questions and answers — use them to resolve follow-up questions " +
            "(e.g. pronouns, \"what about ...\", implied subjects), but always re-derive numbers from the records above rather than trusting prior answers. " +
            "If the question cannot be answered from this data, say so clearly. Keep the 'answer' text concise (1-3 sentences).\n\n" +
            "You must also respond with the structured fields 'chartType' and 'rows':\n" +
            "- 'rows' must be the exact matching records (verbatim subset of the data above, not invented) that support your answer.\n" +
            "- 'chartType' = \"line\" when rows span multiple years for the same programme/faculty (a trend over time); " +
            "\"bar\" when comparing multiple programmes/faculties at one point in time; " +
            "\"table\" for a detailed multi-row breakdown that isn't a simple trend or comparison; " +
            "\"none\" when the answer is a single value or 'rows' has 0 or 1 entries.\n\n" +
            "Enrollment records:\n" + context;

        var messages = new List<ChatMessage> { new SystemChatMessage(systemPrompt) };

        if (history is { Count: > 0 })
        {
            foreach (var turn in history.TakeLast(MaxHistoryTurns))
            {
                messages.Add(turn.Role == "assistant"
                    ? new AssistantChatMessage(turn.Content)
                    : new UserChatMessage(turn.Content));
            }
        }

        messages.Add(new UserChatMessage(question));

        var chatClient = new ChatClient(
            model,
            new ApiKeyCredential(apiKey),
            new OpenAIClientOptions { Endpoint = OpenRouterEndpoint });

        var options = new ChatCompletionOptions
        {
            ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                "enrollment_answer",
                ResponseJsonSchema,
                jsonSchemaIsStrict: true)
        };

        try
        {
            var completion = await chatClient.CompleteChatAsync(messages, options);
            var raw = completion.Value.Content.FirstOrDefault()?.Text;

            if (string.IsNullOrWhiteSpace(raw))
            {
                return new EnrollmentQueryResult { Question = question, Answer = "The AI assistant did not return an answer." };
            }

            var parsed = JsonSerializer.Deserialize<LlmResponse>(raw, JsonOptions);

            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Answer))
            {
                return new EnrollmentQueryResult { Question = question, Answer = "The AI assistant did not return an answer." };
            }

            var chartType = parsed.ChartType is "table" or "line" or "bar" ? parsed.ChartType : "none";
            var rows = parsed.Rows ?? [];

            return new EnrollmentQueryResult
            {
                Question = question,
                Answer = parsed.Answer.Trim(),
                ChartType = rows.Count > 1 ? chartType : "none",
                Rows = rows
            };
        }
        catch (Exception ex)
        {
            return new EnrollmentQueryResult
            {
                Question = question,
                Answer = $"Failed to reach the AI assistant: {ex.Message}"
            };
        }
    }

    private static string BuildContext(List<Enrollment> enrollments)
    {
        var builder = new StringBuilder("Year,Programme,Faculty,StudentCount\n");

        foreach (var enrollment in enrollments)
        {
            builder.Append(enrollment.Year).Append(',')
                .Append(enrollment.Programme).Append(',')
                .Append(enrollment.Faculty).Append(',')
                .Append(enrollment.StudentCount).Append('\n');
        }

        return builder.ToString();
    }
}
