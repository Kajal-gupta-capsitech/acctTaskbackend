using MongoDB.Driver;
using BackendAcctTask.Services;
using BackendAcctTask.Settings;


var builder = WebApplication.CreateBuilder(args);

// ============================================================
// MONGODB SETTINGS
// ============================================================
builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection("MongoDbSettings")
);

var mongoConnection =
    builder.Configuration["MongoDbSettings:ConnectionString"];

var mongoDatabase =
    builder.Configuration["MongoDbSettings:DatabaseName"];

if (string.IsNullOrWhiteSpace(mongoConnection))
{
    throw new InvalidOperationException(
        "MongoDbSettings:ConnectionString is not configured."
    );
}

if (string.IsNullOrWhiteSpace(mongoDatabase))
{
    throw new InvalidOperationException(
        "MongoDbSettings:DatabaseName is not configured."
    );
}

var mongoClient = new MongoClient(mongoConnection);

var database = mongoClient.GetDatabase(mongoDatabase);

builder.Services.AddSingleton<IMongoDatabase>(database);


// ============================================================
// CONTROLLERS
// ============================================================

builder.Services.AddControllers();


// ============================================================
// OLD SERVICE-BASED ARCHITECTURE
// Keep these temporarily so existing controllers continue working.
// ============================================================

builder.Services.AddSingleton<AccountTypeService>();
builder.Services.AddSingleton<ChartAccountService>();
builder.Services.AddSingleton<AccountingPeriodService>();

// Do NOT register TrialBalanceService if TrialBalanceController
// has already been converted to direct MongoDB access.
// builder.Services.AddSingleton<TrialBalanceService>();


// ============================================================
// CORS
// ============================================================

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


// ============================================================
// SWAGGER
// ============================================================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


// ============================================================
// BUILD
// ============================================================

var app = builder.Build();


// ============================================================
// MIDDLEWARE
// ============================================================

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Frontend");

// app.UseHttpsRedirection();

app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();

app.Run();