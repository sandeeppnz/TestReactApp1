using ReactApp1.Server.Domain;

namespace ReactApp1.Server.Data
{
    public partial class EnrollmentService
    {
        private sealed record LlmResponse(string Answer, string ChartType, List<EnrollmentDataPoint>? Rows);
    }
}
