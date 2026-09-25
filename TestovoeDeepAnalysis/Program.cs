using System.Text.Json;
using FluentValidation;
using Npgsql;
using TestovoeDeepAnalysis.Services;
using TestovoeDeepAnalysis.Validators;

var builder = WebApplication.CreateBuilder(args);

// controllers and swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// service
builder.Services.AddScoped<ElementsService>();

// validations
builder.Services.AddValidatorsFromAssemblyContaining<ElementsRequestValidator>();

// db connection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddScoped<NpgsqlConnection>(_ => new NpgsqlConnection(connectionString));

var app = builder.Build();

// swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.RoutePrefix = "api/swagger";
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TestovoeDeepAnalysis API");
});

//app.UseHttpsRedirection();

app.MapControllers();

app.Run();
