namespace ReactApp1.Server.Domain
{
    public class EnrollmentQueryResult
    {
        public required string Question { get; set; }

        public required string Answer { get; set; }

        /// <summary>"table", "line", "bar", or "none" — a hint for how the frontend should visualize <see cref="Rows"/>.</summary>
        public string ChartType { get; set; } = "none";

        public List<EnrollmentDataPoint> Rows { get; set; } = [];
    }
}
