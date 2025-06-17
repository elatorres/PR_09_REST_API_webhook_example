// ServerApp/Program.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddControllers(); // Add the created Controllers
var app = builder.Build();
// Add Swagger middleware if in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger(); // enables Swagger middleware
    app.UseSwaggerUI(); // Shows Swagger UI in browser
}

app.MapControllers(); // Needs AddControllers() above to work.  Maps controller routes

var documentStore = new Dictionary<Guid, string>();

app.MapPost("/request-document", async (HttpContext context) =>
{
    var data = await JsonSerializer.DeserializeAsync<RequestData>(context.Request.Body);

    var docId = Guid.NewGuid();
    Console.WriteLine($"\n[SERVER] Received request from {data.CallbackUrl}, generating document...");

    // Simulate processing time
    _ = Task.Run(async () =>
    {
        await Task.Delay(5000); // Simulate work
        documentStore[docId] = $"Generated document at {DateTime.Now}"; // <= THIS IS THE DOCUMENT!!

        var notification = new { DownloadUrl = $"http://localhost:5000/document/{docId}" };

        var content = new StringContent(
            JsonSerializer.Serialize(notification),
            Encoding.UTF8,
            "application/json");
        
        using var httpClient = new HttpClient();
        await httpClient.PostAsync($"{data.CallbackUrl}?id={docId}", content);
        Console.WriteLine($"[SERVER] Callback sent to {data.CallbackUrl}");
    });

    return Results.Accepted();
});

app.MapGet("/document/{id}", (Guid id) =>
{
    if (documentStore.TryGetValue(id, out var doc))
        return Results.Ok(doc);
    return Results.NotFound("Document not ready");
});

app.Run();

record RequestData(string CallbackUrl);