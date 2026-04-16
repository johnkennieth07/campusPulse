using CampusPulse.Hubs;
using CampusPulse.Services;

var builder = WebApplication.CreateBuilder(args);

// ── Services ──
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSignalR();

builder.Services.AddHttpClient("Nominatim", client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    client.DefaultRequestHeaders.Add("User-Agent", "CampusPulse/1.0");
    client.DefaultRequestHeaders.Add("Accept-Language", "en");
});

builder.Services.AddSingleton<StudentService>();
builder.Services.AddScoped<GeocodingService>();

// ── CORS: allow all origins for cloud deployment ──
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();
app.MapControllers();
app.MapHub<LocationHub>("/locationHub");
app.MapFallbackToFile("index.html");

// ── Use Render's PORT environment variable ──
var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
app.Run($"http://0.0.0.0:{port}");