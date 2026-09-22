using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.OpenApi.Models;
using ReactApp1.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<IBookService, BookService>();

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


app.MapGet("/books", (IBookService bookService) =>
    TypedResults.Ok(bookService.GetBooks()))
    .WithName("GetBooks")
    .WithOpenApi(x => new OpenApiOperation(x)
    {
        Summary = "Get Library Books",
        Description = "Returns information about all the available books from the library.",
        Tags = new List<OpenApiTag> { new() { Name = "Library" } }
    });


app.MapGet("/books/{id}", Results<Ok<Book>, NotFound> (IBookService bookService, int id) =>
        bookService.GetBook(id) is { } book
            ? TypedResults.Ok(book)
            : TypedResults.NotFound()
    )
    .WithName("GetBookById")
    .WithOpenApi(x => new OpenApiOperation(x)
    {
        Summary = "Get Library Book By Id",
        Description = "Returns information about selected book from the library.",
        Tags = new List<OpenApiTag> { new() { Name = "Library" } }
    });

app.MapPost("/books", (IBookService bookService, Book book) =>
{
    var created = bookService.CreateBook(book);
    return TypedResults.Created($"/books/{created.Id}", created);
})
    .WithName("CreateBook")
    .WithOpenApi(x => new OpenApiOperation(x)
    {
        Summary = "Create Library Book",
        Description = "Adds a new book to the library.",
        Tags = new List<OpenApiTag> { new() { Name = "Library" } }
    });

app.MapPut("/books/{id}", Results<Ok<Book>, NotFound> (IBookService bookService, int id, Book book) =>
        bookService.UpdateBook(id, book) is { } updated
            ? TypedResults.Ok(updated)
            : TypedResults.NotFound()
    )
    .WithName("UpdateBook")
    .WithOpenApi(x => new OpenApiOperation(x)
    {
        Summary = "Update Library Book",
        Description = "Updates an existing book in the library.",
        Tags = new List<OpenApiTag> { new() { Name = "Library" } }
    });

app.MapDelete("/books/{id}", Results<NoContent, NotFound> (IBookService bookService, int id) =>
        bookService.DeleteBook(id)
            ? TypedResults.NoContent()
            : TypedResults.NotFound()
    )
    .WithName("DeleteBook")
    .WithOpenApi(x => new OpenApiOperation(x)
    {
        Summary = "Delete Library Book",
        Description = "Removes a book from the library.",
        Tags = new List<OpenApiTag> { new() { Name = "Library" } }
    });


app.Run();

