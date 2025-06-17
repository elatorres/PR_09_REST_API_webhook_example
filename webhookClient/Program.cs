// ClientApp/Program.cs
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using System.Net.Http;
using System.Text.Json;
// Creates and configures a minimal ASP.NET Core web app.
var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();
// Initializes a Guid to later store the document's ID.
Guid documentId = Guid.Empty;

// Webhook endpoint the server will call
app.MapPost("/webhook", async (HttpContext context) =>
{
    // This maps the POST endpoint/webhook. The server will use it for callback when the document is ready.
    // The server is expected to call it with a query string like ?id=<GUID>.
    var idStr = context.Request.Query["id"];
    // The client: Parses the document ID. Retrieves the document from the server via GET http://localhost:5000/document/{id}. And prints the document content.
    if (Guid.TryParse(idStr, out var id))
    {
        documentId = id;
        Console.WriteLine($"[CLIENT] Received callback with document ID: {documentId}");

        // Retrieve document from server
        using var httpClient = new HttpClient();
        var doc = await httpClient.GetStringAsync($"http://localhost:5000/document/{documentId}");
        Console.WriteLine($"[CLIENT] Document content: {doc}");
    }
    else
    {
        Console.WriteLine("[CLIENT] Invalid callback received.");
    }

    return Results.Ok();
});

// Client startup action. It runs once the app has started.
app.Lifetime.ApplicationStarted.Register(async () =>
{
    // Sends a POST request to the main server's /request-document endpoint, saying:
    // "Hey, generate a document and call me back at http://localhost:5001/webhook when it’s ready."
    using var httpClient = new HttpClient();
    var data = new { CallbackUrl = "http://localhost:5001/webhook" };
    var content = new StringContent(JsonSerializer.Serialize(data), System.Text.Encoding.UTF8, "application/json");

    Console.WriteLine("[CLIENT] Requesting document generation...");
    await httpClient.PostAsync("http://localhost:5000/request-document", content);
    Console.WriteLine($"[CLIENT] document received: {content}");
});

app.Run("http://localhost:5001");