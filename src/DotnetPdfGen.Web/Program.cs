using System.Text.Json.Serialization;
using DotnetPdfGen.Web.Infrastructure;
using DotnetPdfGen.Web.Samples;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDotnetPdfGen();
builder.Services.AddSingleton<SampleTemplateStore>();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<TemplateBindingExceptionHandler>();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Serves the live preview page from wwwroot.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthorization();

app.MapControllers();

app.Run();
