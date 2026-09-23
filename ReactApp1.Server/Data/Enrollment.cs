namespace ReactApp1.Server.Data;

public class Enrollment
{
    public int Id { get; set; }

    public required int Year { get; set; }

    public required string Programme { get; set; }

    public required string Faculty { get; set; }

    public required int StudentCount { get; set; }
}
