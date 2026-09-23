using ReactApp1.Server.Domain;

namespace ReactApp1.Server.Data
{
    public interface IEnrollmentService
    {
        Task<EnrollmentQueryResult> AnswerQuestionAsync(string question, IReadOnlyList<ChatTurn>? history);
    }
}
