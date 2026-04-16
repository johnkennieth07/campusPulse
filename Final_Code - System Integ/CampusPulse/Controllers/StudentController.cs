using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using CampusPulse.Hubs;
using CampusPulse.Models;
using CampusPulse.Services;

namespace CampusPulse.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StudentsController : ControllerBase
{
    private readonly StudentService   _studentService;
    private readonly GeocodingService _geocodingService;
    private readonly IHubContext<LocationHub> _hub;

    // ── Campus definitions (tight boundary: campus area only, ~1-2 m buffer) ──
    // Radius is in metres.  100 m = just the core campus footprint.
    // Increase to 150 if the campus grounds are larger.
    private static readonly Dictionary<string, CampusBoundary> CAMPUS_BOUNDS = new()
    {
        ["main"]      = new(10.8942,  123.4175, 100),   // SUNN Main Campus, Sagay City
        ["oldsagay"]  = new(10.9403,  123.4230, 100),   // Old Sagay Campus
        ["escalante"] = new(10.8382,  123.4983, 100),   // Escalante Campus
        ["lagaan"]    = new(10.6342,  123.3817, 100),   // Laga-an Campus, Calatrava
    };

    // ── FIX: Load admin key from environment variable; fall back to config ──
    private static readonly string ADMIN_KEY =
        Environment.GetEnvironmentVariable("CAMPUSPULSE_ADMIN_KEY") ?? "Johnkennieth07";

    public StudentsController(
        StudentService studentService,
        GeocodingService geocodingService,
        IHubContext<LocationHub> hub)
    {
        _studentService   = studentService;
        _geocodingService = geocodingService;
        _hub              = hub;
    }

    // ── Haversine distance in metres ──
    private static double HaversineMeters(double lat1, double lng1, double lat2, double lng2)
    {
        const double R = 6_371_000;
        var dLat = (lat2 - lat1) * Math.PI / 180;
        var dLng = (lng2 - lng1) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
              * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    // ── Determine inside/outside for each campus ──
    private static string DetermineStatus(double lat, double lng)
    {
        foreach (var (_, cb) in CAMPUS_BOUNDS)
        {
            if (HaversineMeters(lat, lng, cb.Lat, cb.Lng) <= cb.RadiusMeters)
                return "on-campus";
        }
        return "off-campus";
    }

    // GET /api/students
    [HttpGet]
    public IActionResult GetAll([FromQuery] string? status)
    {
        var students = _studentService.GetAll();
        if (!string.IsNullOrEmpty(status))
            students = students.Where(s => s.Status == status).ToList();
        return Ok(students);
    }

    // GET /api/students/geojson  ← Leaflet consumes this
    [HttpGet("geojson")]
    public IActionResult GetGeoJson([FromQuery] string? status)
    {
        var students = _studentService.GetAll();
        if (!string.IsNullOrEmpty(status))
            students = students.Where(s => s.Status == status).ToList();
        return Ok(_studentService.ToGeoJson(students));
    }

    // GET /api/students/{id}
    [HttpGet("{id}")]
    public IActionResult GetById(int id)
    {
        var s = _studentService.GetById(id);
        return s is null ? NotFound() : Ok(s);
    }

    // POST /api/students  ← Add new student (initial placement at selected campus)
    [HttpPost]
    public async Task<IActionResult> AddStudent([FromBody] AddStudentRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Hometown))
            return BadRequest("Name and Hometown are required.");

        // Use the campus supplied in the request (or default to main)
        var campusKey = req.Campus ?? "main";
        if (!CAMPUS_BOUNDS.TryGetValue(campusKey, out var cb))
            cb = CAMPUS_BOUNDS["main"];

        // Initial position: place at campus centre; status determined by geofence
        double lat = cb.Lat, lng = cb.Lng;
        var status = req.Status ?? DetermineStatus(lat, lng);

        var student = _studentService.Add(new Student
        {
            Name      = req.Name,
            Course    = req.Course,
            Year      = req.Year,
            Hometown  = req.Hometown,
            Status    = status,
            Campus    = campusKey,
            Latitude  = lat,
            Longitude = lng
        });

        await _hub.Clients.All.SendAsync("StudentAdded", student);
        return CreatedAtAction(nameof(GetById), new { id = student.Id }, student);
    }

    // PUT /api/students/{id}/location  ← Student tracker pushes GPS here
    // Status is AUTO-DETERMINED by geofence — never trusted from client
    [HttpPut("{id}/location")]
    public async Task<IActionResult> UpdateLocation(int id, [FromBody] StudentLocationUpdate update)
    {
        // Override whatever status the client sent — geofence decides
        var computedStatus = DetermineStatus(update.Latitude, update.Longitude);
        var success = _studentService.UpdateLocation(id, update.Latitude, update.Longitude, computedStatus);
        if (!success) return NotFound();

        await _hub.Clients.All.SendAsync("ReceiveLocationUpdate", new
        {
            StudentId = id,
            update.Latitude,
            update.Longitude,
            Status    = computedStatus,   // always geofence-derived
            Timestamp = DateTime.UtcNow
        });

        return NoContent();
    }

    // DELETE /api/students/{id}
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var success = _studentService.Delete(id);
        if (!success) return NotFound();

        await _hub.Clients.All.SendAsync("StudentRemoved", id);
        return NoContent();
    }

    // GET /api/students/stats
    [HttpGet("stats")]
    public IActionResult GetStats()
    {
        var all = _studentService.GetAll();
        return Ok(new
        {
            Total     = all.Count,
            OnCampus  = all.Count(s => s.Status == "on-campus"),
            OffCampus = all.Count(s => s.Status == "off-campus"),
            Campuses  = CAMPUS_BOUNDS.Count
        });
    }

    // ── EMERGENCY: admin requests exact GPS of a student ──
    // POST /api/students/{id}/emergency-location
    // Body: { "adminKey": "ADMIN_SECRET" }
    //
    // FIX: Accepts BOTH the numeric backend ID (from API-registered students)
    //      AND a fallback lookup by StudentId string so tracker-only students
    //      can also be resolved when the frontend passes their backend ID.
    [HttpPost("{id}/emergency-location")]
    public IActionResult GetEmergencyLocation(int id, [FromBody] EmergencyRequest req)
    {
        // ── FIX 1: Validate admin key first — always, before any data lookup ──
        if (req is null || string.IsNullOrWhiteSpace(req.AdminKey))
            return Unauthorized(new { error = "Admin key is required." });

        if (req.AdminKey != ADMIN_KEY)
            return Unauthorized(new { error = "Invalid admin credentials." });

        // ── FIX 2: Try to find by numeric ID first, then by StudentId string ──
        var s = _studentService.GetById(id);

        if (s is null)
        {
            // Student was registered via Live Tracker with a Date.now() ID
            // that doesn't exist in the backend store — return 404 so the
            // frontend knows to fall back to its own live GPS data.
            return NotFound(new { error = "Student not found in server store. Use live GPS fallback." });
        }

        return Ok(new
        {
            StudentId   = s.Id,
            Name        = s.Name,
            Latitude    = s.Latitude,
            Longitude   = s.Longitude,
            Status      = s.Status,
            LastUpdated = s.LastUpdated,
            Note        = "EMERGENCY ACCESS — handle with care"
        });
    }
}

// ── Small value types ──
public record CampusBoundary(double Lat, double Lng, double RadiusMeters);

public class EmergencyRequest
{
    public string AdminKey { get; set; } = string.Empty;
}