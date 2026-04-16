using System.Text.Json;
using CampusPulse.Models;

namespace CampusPulse.Services;

// ── StudentService — in-memory store (replace with DB later) ──
public class StudentService
{
    // Starts empty — all records come from student-tracker.html or the Add Student form
    private readonly List<Student> _students = new();
    private int _nextId = 1;

    public List<Student> GetAll() => _students.ToList();

    public Student? GetById(int id) => _students.FirstOrDefault(s => s.Id == id);

    public Student Add(Student student)
    {
        student.Id = _nextId++;
        student.LastUpdated = DateTime.UtcNow;
        _students.Add(student);
        return student;
    }

    public bool UpdateLocation(int id, double lat, double lng, string status)
    {
        var s = _students.FirstOrDefault(x => x.Id == id);
        if (s is null) return false;
        s.Latitude    = lat;
        s.Longitude   = lng;
        s.Status      = status;
        s.LastUpdated = DateTime.UtcNow;
        return true;
    }

    public bool Delete(int id)
    {
        var s = _students.FirstOrDefault(x => x.Id == id);
        if (s is null) return false;
        _students.Remove(s);
        return true;
    }

    // Build GeoJSON for Leaflet
    public GeoJsonFeatureCollection ToGeoJson(List<Student>? students = null)
    {
        var list = students ?? _students;
        return new GeoJsonFeatureCollection
        {
            Features = list.Select(s => new GeoJsonFeature
            {
                Geometry = new GeoJsonGeometry
                {
                    Coordinates = new[] { s.Longitude, s.Latitude }
                },
                Properties = new GeoJsonProperties
                {
                    Id       = s.Id,
                    Name     = s.Name,
                    Course   = s.Course,
                    Year     = s.Year,
                    Hometown = s.Hometown,
                    Status   = s.Status
                }
            }).ToList()
        };
    }
}

// ── GeocodingService — Nominatim (OpenStreetMap) ──
public class GeocodingService
{
    private readonly IHttpClientFactory _httpFactory;

    public GeocodingService(IHttpClientFactory httpFactory)
    {
        _httpFactory = httpFactory;
    }

    /// <summary>
    /// Geocode a place name to lat/lng using Nominatim (free, no API key).
    /// Rate limit: 1 request/second — fine for campus-scale app.
    /// </summary>
    public async Task<(double Lat, double Lng)?> GeocodeAsync(string place)
    {
        try
        {
            var client = _httpFactory.CreateClient("Nominatim");
            var query  = Uri.EscapeDataString(place + ", Philippines");
            var url    = $"search?q={query}&format=json&limit=1";

            var response = await client.GetStringAsync(url);
            var results  = JsonSerializer.Deserialize<List<NominatimResult>>(response,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (results?.Count > 0)
            {
                return (double.Parse(results[0].Lat), double.Parse(results[0].Lon));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Geocoding Error] {ex.Message}");
        }
        return null;
    }

    private class NominatimResult
    {
        public string Lat { get; set; } = "0";
        public string Lon { get; set; } = "0";
        public string Display_name { get; set; } = string.Empty;
    }
}