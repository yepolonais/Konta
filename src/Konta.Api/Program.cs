using Konta.Application.Imports;
using Konta.Application.Transactions;
using Konta.Application.Accounts;
using Konta.Infrastructure;
using Konta.Infrastructure.Persistence;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.OpenApi;

const long maximumUploadBytes = 10 * 1024 * 1024;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = maximumUploadBytes);
builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddSwaggerGen();
builder.Services.AddKontaInfrastructure(builder.Configuration);
builder.Services.AddScoped<IImportPreviewService, ImportPreviewService>();
builder.Services.AddScoped<ITransactionQueryService, TransactionQueryService>();
builder.Services.AddScoped<IAccountService, AccountService>();

var app = builder.Build();
var frontendPath = app.Environment.IsDevelopment()
    ? Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "../../frontend"))
    : Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var frontendProvider = new PhysicalFileProvider(frontendPath);
app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = frontendProvider });
app.UseStaticFiles(new StaticFileOptions { FileProvider = frontendProvider });

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseStatusCodePages();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<KontaDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.MapControllers();

app.MapFallbackToFile("index.html", new StaticFileOptions { FileProvider = frontendProvider });

await app.RunAsync();

public partial class Program { }
