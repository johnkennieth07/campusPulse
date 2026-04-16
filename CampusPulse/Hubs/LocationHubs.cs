using Microsoft.AspNetCore.SignalR;
using CampusPulse.Models;

namespace CampusPulse.Hubs;

public class LocationHub : Hub
{
    // ── Called by student device to update their location ──
    public async Task UpdateStudentLocation(StudentLocationUpdate update)
    {
        Console.WriteLine($"[SignalR] {update.StudentName} → Lat:{update.Latitude}, Lng:{update.Longitude}");

        // Broadcast status + coords to all dashboard clients
        // NOTE: the REST controller also recomputes status server-side via geofence
        await Clients.All.SendAsync("ReceiveLocationUpdate", update);
    }

    // ── Called when student enters/leaves campus (geofence event) ──
    public async Task UpdateStudentStatus(int studentId, string status)
    {
        await Clients.All.SendAsync("ReceiveStatusUpdate", new
        {
            StudentId = studentId,
            Status    = status,
            Timestamp = DateTime.UtcNow
        });
    }

    // ── Notify when a new student is added ──
    public async Task NotifyStudentAdded(Student student)
    {
        await Clients.All.SendAsync("StudentAdded", student);
    }

    // ── Emergency: admin requests precise student location ──
    // Caller must have already authenticated via POST /api/students/{id}/emergency-location.
    // This hub method broadcasts the emergency alert to the requesting admin client only.
    public async Task RequestEmergencyLocation(int studentId, string adminConnectionId)
    {
        Console.WriteLine($"[SignalR] EMERGENCY location request for student {studentId}");
        await Clients.Client(adminConnectionId).SendAsync("EmergencyLocationGranted", new
        {
            StudentId = studentId,
            Timestamp = DateTime.UtcNow,
            Note      = "Emergency access granted — precise GPS available via REST"
        });
    }

    // ── Connection events ──
    public override async Task OnConnectedAsync()
    {
        Console.WriteLine($"[SignalR] Client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Console.WriteLine($"[SignalR] Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}