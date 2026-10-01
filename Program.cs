using BackendAcctTask.Services;
using BackendAcctTask.Settings;

var builder = WebApplication.CreateBuilder(args);

// MongoDB settings
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings"));

// Controllers
builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy
            .WithOrigins("http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });

});

// Services
builder.Services.AddSingleton<AccountTypeService>();
builder.Services.AddSingleton<ChartAccountService>();
builder.Services.AddSingleton<AccountingPeriodService>();
builder.Services.AddSingleton<TrialBalanceService>();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
// Enable Swagger UI for all environments so the API documentation is available


app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

