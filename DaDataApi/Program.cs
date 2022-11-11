using Dadata;
using System.Security.Authentication;

// Кол-во запросов к сервису DaData Api в день
int count_requests = 0;

// Текущая дата
DateTime currentDate = DateTime.Now;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseKestrel(kestrelOptions =>
{
	kestrelOptions.ConfigureHttpsDefaults(httpsOptions =>
	{
		httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
	});
});

// Add services to the container.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseSwagger();
	app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.MapGet("/count", () =>
{
	app.Logger.LogDebug($"Get count requests ({count_requests})");
	return count_requests;
});

app.MapPost("/suggest_address", async (SuggestionRequest suggestion, IConfiguration conf) =>
{
	try
	{
		count_requests++;
		app.Logger.LogInformation($"[{DateTime.Now}] [{count_requests}] Get request to DaData.Api ...");

		var token = conf.GetSection("Token").Value;
		var api = new SuggestClientAsync(token);

		var result = await api.SuggestAddress(suggestion.Query);

		if (currentDate.Date < DateTime.Now.Date)
			count_requests = 0;

		app.Logger.LogInformation($"[{DateTime.Now}] [{count_requests}] DaData.Api => Count suggestions - {result.suggestions.Count}.");

		if (result.suggestions.Any())
			return Results.Ok(result.suggestions);
	}
	catch(Exception ex)
	{
		app.Logger.LogError($"[{DateTime.Now}] [{count_requests}] DaData.Api => ERROR: {ex.Message}");
		return Results.Problem(ex.Message);
	}

	return Results.NotFound("Не найдено.");
});

app.Run();

public class SuggestionRequest
{
	/// <summary>
	/// Текст запроса
	/// </summary>
	public string Query { get; set; }
}