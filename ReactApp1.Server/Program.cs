using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi.Models;
using ReactApp1.Server.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IEnrollmentService, EnrollmentService>();

var app = builder.Build();

// configure exception middleware
app.UseStatusCodePages(async statusCodeContext
    => await Results.Problem(statusCode: statusCodeContext.HttpContext.Response.StatusCode)
        .ExecuteAsync(statusCodeContext.HttpContext));

app.UseDefaultFiles();
app.UseStaticFiles();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapFallbackToFile("/index.html");


app.MapPost("/enrollments/query", async Task<Results<Ok<EnrollmentQueryResult>, BadRequest<string>>> (IEnrollmentService enrollmentService, EnrollmentQuestion request) =>
        string.IsNullOrWhiteSpace(request.Question)
            ? TypedResults.BadRequest("Question must not be empty.")
            : TypedResults.Ok(await enrollmentService.AnswerQuestionAsync(request.Question, request.History))
    )
    .WithName("QueryEnrollments")
    .WithOpenApi(x => new OpenApiOperation(x)
    {
        Summary = "Ask a Natural Language Question",
        Description = "Answers a natural language question about university enrollments, e.g. \"How many students were in Business Administration?\".",
        Tags = new List<OpenApiTag> { new() { Name = "Enrollment" } }
    });


app.Run();

public record EnrollmentQuestion(string Question, List<ChatTurn>? History = null);

