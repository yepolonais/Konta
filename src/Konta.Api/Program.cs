using Konta.Application.Imports;
using Konta.Application.Transactions;
using Konta.Infrastructure;
using Konta.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

const long maximumUploadBytes = 10 * 1024 * 1024;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = maximumUploadBytes);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddKontaInfrastructure(builder.Configuration);
builder.Services.AddScoped<ImportPreviewService>();
builder.Services.AddScoped<TransactionQueryService>();

var app = builder.Build();
var frontendPath = app.Environment.IsDevelopment()
    ? Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "../../frontend"))
    : Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var frontendProvider = new PhysicalFileProvider(frontendPath);
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = frontendProvider });
app.UseStaticFiles(new StaticFileOptions { FileProvider = frontendProvider });

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<KontaDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
    .WithName("GetHealth")
    .WithOpenApi();

app.MapGet("/api/transactions", async (TransactionQueryService service, CancellationToken cancellationToken) =>
{
    var transactions = await service.ListAsync(cancellationToken);
    return Results.Ok(transactions);
})
.WithName("GetTransactions")
.WithOpenApi();

app.MapPost("/api/imports/preview", async (HttpRequest request, ImportPreviewService service) =>
{
    if (!request.HasFormContentType)
    {
        return Results.BadRequest(new { error = "Envoyez le fichier dans un formulaire multipart." });
    }

    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("file");
    if (file is null)
    {
        return Results.BadRequest(new { error = "Aucun fichier CSV fourni." });
    }

    if (file.Length == 0)
    {
        return Results.BadRequest(new { error = "Le fichier est vide." });
    }

    if (file.Length > maximumUploadBytes)
    {
        return Results.BadRequest(new { error = "Le fichier dépasse la limite de 10 Mo." });
    }

    if (!string.Equals(Path.GetExtension(file.FileName), ".csv", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "Sélectionnez un fichier CSV." });
    }

    await using var stream = file.OpenReadStream();
    var preview = service.Preview(stream);
    return Results.Ok(preview);
})
.WithName("PreviewImport")
.WithOpenApi();

app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = frontendProvider });

await app.RunAsync();

public partial class Program { }
