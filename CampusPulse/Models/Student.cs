namespace CampusPulse.Models;

public class Student
{
    public int    Id          { get; set; }
    public string Name        { get; set; } = string.Empty;
    public string Course      { get; set; } = string.Empty;
    public string Year        { get; set; } = string.Empty;
    public string Hometown    { get; set; } = string.Empty;
    public string Campus      { get; set; } = "main";              // campus key
    public string Status      { get; set; } = "off-campus";        // "on-campus" | "off-campus"
    public double Latitude    { get; set; }
    public double Longitude   { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class StudentLocationUpdate
{
    public int    StudentId   { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public double Latitude    { get; set; }
    public double Longitude   { get; set; }
    // Status intentionally kept for SignalR payloads but server always recomputes from geofence
    public string Status      { get; set; } = "off-campus";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class AddStudentRequest
{
    public string Name     { get; set; } = string.Empty;
    public string Course   { get; set; } = string.Empty;
    public string Year     { get; set; } = string.Empty;
    public string Hometown { get; set; } = string.Empty;
    public string Campus   { get; set; } = "main";
    public string Status   { get; set; } = "off-campus";
}

// ── GeoJSON Models for Leaflet ──
public class GeoJsonFeatureCollection
{
    public string Type     { get; set; } = "FeatureCollection";
    public List<GeoJsonFeature> Features { get; set; } = new();
}

public class GeoJsonFeature
{
    public string Type       { get; set; } = "Feature";
    public GeoJsonGeometry   Geometry   { get; set; } = new();
    public GeoJsonProperties Properties { get; set; } = new();
}

public class GeoJsonGeometry
{
    public string   Type        { get; set; } = "Point";
    public double[] Coordinates { get; set; } = Array.Empty<double>();
}

public class GeoJsonProperties
{
    public int    Id       { get; set; }
    public string Name     { get; set; } = string.Empty;
    public string Course   { get; set; } = string.Empty;
    public string Year     { get; set; } = string.Empty;
    public string Hometown { get; set; } = string.Empty;
    public string Campus   { get; set; } = string.Empty;
    public string Status   { get; set; } = string.Empty;
}