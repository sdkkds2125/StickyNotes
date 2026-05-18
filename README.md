# 📝 StickyNotes

A self-hosted, privacy-first note-taking web application built with **ASP.NET Core 8**, **Razor Pages**, and **SQLite**. Access your notes from any device through a secure, modern dark-mode interface.

🌐 **Live Demo:** [https://notes.myreallylongnamedomainsoitwouldbecheap.win](https://notes.myreallylongnamedomainsoitwouldbecheap.win/)

## Features

- **Text Notes** — Write freeform notes with a rich, auto-resizing editor
- **Checklists** — Create task lists with checkable items and a visual progress bar
- **Drag-and-Drop Reordering** — Reorder checklist items by dragging the ☰ handle
- **Color Coding** — Choose from 6 colors (Yellow, Pink, Blue, Green, Purple, Orange)
- **Pin Notes** — Pin important notes to the top of your dashboard
- **User Authentication** — Secure login with ASP.NET Core Identity
- **Responsive Design** — Premium glassmorphism dark-mode UI that works on desktop and mobile
- **Self-Hosted** — Your data stays on your hardware, no cloud dependency

## Tech Stack

| Layer | Technology |
|---|---|
| Backend | ASP.NET Core 8, Razor Pages |
| Database | SQLite via Entity Framework Core |
| Authentication | ASP.NET Core Identity |
| Frontend | Vanilla HTML/CSS/JS, Inter font |
| Hosting | systemd service + Cloudflare Tunnel |

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run Locally

```bash
git clone <your-repo-url>
cd StickyNotes
dotnet run
```

The app will start on `http://localhost:5000` (configured in `appsettings.json`). On first launch, the database is created automatically.

### Configuration

By default, the app creates a `notes.db` file in the project directory. To change the database location or port, edit `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=notes.db"
  },
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      }
    }
  }
}
```

To use a custom database path in production (e.g. when running as a systemd service), set an environment variable instead of editing the config file:

```bash
Environment=ConnectionStrings__DefaultConnection=Data Source=/var/lib/stickynotes/notes.db
```

## Testing

### Unit Tests

The project includes **32 unit tests** across two test files using **xUnit**, **Moq**, and an **in-memory database**:

| File | Tests | Coverage |
|---|---|---|
| `ModelTests.cs` (16 tests) | Default values, validation boundaries (title ≤200, content ≤500, color ≤7), enum values, parent-child relationships |
| `PageModelTests.cs` (14 tests) | User isolation, CRUD operations, pin toggling, security (can't access/edit/delete other users' notes), empty state handling, sort order |

Run the tests:

```bash
dotnet test tests/StickyNotes.Tests
```

### Load Tests

A custom C# console application that benchmarks your app under concurrent load. It tests multiple endpoints, supports authentication, and reports latency metrics.

```bash
# Test localhost (unauthenticated)
dotnet run --project tests/StickyNotes.LoadTests

# Test your live site with authentication
dotnet run --project tests/StickyNotes.LoadTests -- https://your-site.com username password
```

The load test simulates 50 concurrent users making 20 requests each (1,000 total) against the dashboard, note editor, and login page. Adjust `ConcurrentUsers` and `RequestsPerUser` in `Program.cs` to change intensity.

## Security

| Protection | How |
|---|---|
| SQL Injection | Entity Framework Core parameterizes all queries |
| XSS | Razor auto-encodes all rendered output |
| CSRF | Anti-forgery tokens on every form |
| Authorization | `[Authorize]` attribute + `UserId` checks on every query |


## License

Private / Personal Use
