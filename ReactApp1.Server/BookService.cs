using Microsoft.Data.Sqlite;
using System.Data;
using Dapper;

namespace ReactApp1.Server;

public interface IBookService
{
    List<Book> GetBooks();

    Book GetBook(int id);

    Book CreateBook(Book book);

    Book UpdateBook(int id, Book book);

    bool DeleteBook(int id);
}

public class Book
{
    public int Id { get; set; }

    public required string Title { get; set; }

    public required string Author { get; set; }
}

public class BookService : IBookService
{
    private readonly string _connectionString;

    public BookService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("BookAppDb")
            ?? throw new InvalidOperationException("Missing 'BookAppDb' connection string.");

        InitializeDatabase();
    }

    private void InitializeDatabase()
    {
        using var connection = CreateConnection();

        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS Books (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Title TEXT NOT NULL,
                Author TEXT NOT NULL
            );
            """);

        var bookCount = connection.ExecuteScalar<int>("SELECT COUNT(*) FROM Books");

        if (bookCount == 0)
        {
            connection.Execute(
                "INSERT INTO Books (Title, Author) VALUES (@Title, @Author);",
                new[]
                {
                    new { Title = "Dependency Injection in .NET", Author = "Mark Seemann" },
                    new { Title = "C# in Depth", Author = "Jon Skeet" },
                    new { Title = "Programming Entity Framework", Author = "Julia Lerman" },
                    new { Title = "Programming WCF Services", Author = "Juval Lowy and Michael Montgomery" }
                });
        }
    }

    private IDbConnection CreateConnection() => new SqliteConnection(_connectionString);

    public List<Book> GetBooks()
    {
        using var connection = CreateConnection();

        return connection.Query<Book>("SELECT Id, Title, Author FROM Books").ToList();
    }

    public Book GetBook(int id)
    {
        using var connection = CreateConnection();

        return connection.QueryFirstOrDefault<Book>(
            "SELECT Id, Title, Author FROM Books WHERE Id = @Id;",
            new { Id = id });
    }

    public Book CreateBook(Book book)
    {
        using var connection = CreateConnection();

        var newId = connection.ExecuteScalar<long>(
            """
            INSERT INTO Books (Title, Author) VALUES (@Title, @Author);
            SELECT last_insert_rowid();
            """,
            book);

        book.Id = (int)newId;

        return book;
    }

    public Book UpdateBook(int id, Book book)
    {
        using var connection = CreateConnection();

        var rowsAffected = connection.Execute(
            "UPDATE Books SET Title = @Title, Author = @Author WHERE Id = @Id;",
            new { book.Title, book.Author, Id = id });

        if (rowsAffected == 0)
        {
            return null;
        }

        book.Id = id;

        return book;
    }

    public bool DeleteBook(int id)
    {
        using var connection = CreateConnection();

        var rowsAffected = connection.Execute(
            "DELETE FROM Books WHERE Id = @Id;",
            new { Id = id });

        return rowsAffected > 0;
    }
}
