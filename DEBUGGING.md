# Debugging Camus API in Docker Container

This setup enables **hot reload** and **VS Code debugging** for the containerized API with breakpoints support.

## 🎯 Features

- ✅ **Hot reload** - Code changes rebuild automatically (no container restart)
- ✅ **VS Code breakpoints** - Full debugging support in container
- ✅ **Keep containers running** - Only API rebuilds, observability stack stays up
- ✅ **Source maps** - Breakpoints work correctly with volume-mounted code

## 🚀 Quick Start

### 1. Start Full Stack with Hot Reload
```bash
docker-compose -f docker-compose.dev.yml up --build
```

**What happens:**
- API runs with `dotnet watch` (auto-rebuild on file changes)
- Source code mounted as volume from `./src` → `/src` in container
- Observability stack starts (Postgres, Jaeger, Grafana, etc.)

### 2. Attach Debugger from VS Code

**Option A: Use VS Code UI**
1. Press `F5` or click **Run and Debug**
2. Select **".NET Attach to Container"**
3. Pick the `dotnet` process from the list
4. Set breakpoints and debug! 🎉

**Option B: Command Palette**
- `Cmd+Shift+P` → **"Debug: Select and Start Debugging"** → **".NET Attach to Container"**

### 3. Make Code Changes

**Edit any file** in `src/` → Container automatically rebuilds → Debugger reconnects

**No need to:**
- ❌ Restart containers
- ❌ Rebuild images manually
- ❌ Detach/reattach debugger

## 📁 File Structure

```
├── Dockerfile              # Production image (optimized, multi-stage)
├── Dockerfile.dev          # Development image (SDK, debugger, dotnet watch)
├── docker-compose.dev.yml  # Dev stack with volume mounts
├── docker-compose.prod.yml # Production stack (minimal)
└── .vscode/
    ├── launch.json         # Debug configurations
    └── tasks.json          # Build/run tasks
```

## 🔧 How It Works

### Dockerfile.dev
- Uses full SDK image (not aspnet runtime)
- Installs `vsdbg` (VS Code debugger for .NET)
- Runs `dotnet watch` for hot reload
- **No source code copied** (mounted as volume instead)

### docker-compose.dev.yml
- Mounts `./src:/src:cached` for hot reload
- Excludes `bin/obj` folders to avoid conflicts
- Exposes port `5001` for debugger (optional)

### launch.json
- **".NET Launch API (Host)"** - Run API directly on host (faster for quick dev)
- **".NET Attach to Container"** - Debug containerized API with breakpoints

## 🎛️ VS Code Tasks

Available tasks (`Cmd+Shift+P` → **Tasks: Run Task**):

| Task | Purpose |
|------|---------|
| `docker-compose-up-dev` | Start full stack (API + observability) |
| `docker-compose-up-observability` | Start only observability (use with `watch-api` on host) |
| `docker-compose-down` | Stop and remove containers |
| `watch-api` | Run API on host with hot reload (fastest iteration) |

## 💡 Development Workflows

### Workflow 1: Full Docker (Recommended)
**Best for:** Testing complete containerized setup

```bash
# Start everything
docker-compose -f docker-compose.dev.yml up --build

# Attach debugger from VS Code (F5 → ".NET Attach to Container")
# Edit code → Auto-rebuild → Debug continues

# Stop when done
docker-compose -f docker-compose.dev.yml down
```

### Workflow 2: Hybrid (Fastest)
**Best for:** Day-to-day coding with fastest iteration

```bash
# Start observability only
docker-compose -f docker-compose.dev.yml up postgres otel-collector jaeger loki prometheus grafana -d

# Run API on host with hot reload
# VS Code: Run Task → "watch-api"

# Debug on host (F5 → ".NET Launch API (Host)")
```

### Workflow 3: Production Test
**Best for:** Testing before deployment

```bash
# Build production image
docker build -t camus-api:latest .

# Run with production config
docker-compose -f docker-compose.prod.yml up
```

## 🐛 Troubleshooting

### Debugger won't attach
**Solution:** Ensure container is running and has `vsdbg` installed
```bash
docker exec -it camus-api-dev ls /vsdbg
```

### Hot reload not working
**Solution:** Check volume mount permissions
```bash
docker-compose -f docker-compose.dev.yml down -v
docker-compose -f docker-compose.dev.yml up --build
```

### Breakpoints not hitting
**Solution:** Verify source file mapping in launch.json
```json
"sourceFileMap": {
    "/src": "${workspaceFolder}/src"
}
```

### Build errors after code changes
**Solution:** Clean and rebuild
```bash
docker-compose -f docker-compose.dev.yml down
docker-compose -f docker-compose.dev.yml up --build --force-recreate api
```

## 📊 Access Points

| Service | URL | Description |
|---------|-----|-------------|
| **API** | http://localhost:5001 | Camus API with hot reload |
| **Grafana** | http://localhost:3000 | Dashboards (admin/admin) |
| **Jaeger** | http://localhost:16686 | Distributed tracing UI |
| **Prometheus** | http://localhost:9090 | Metrics query UI |

## 🔐 Environment Variables

Override in `docker-compose.dev.yml` or `.env` file:

```env
ASPNETCORE_ENVIRONMENT=Development
POSTGRES_PASSWORD=camus_dev_password
OpenTelemetry__Tracing__Exporter=otlp
```

## 📝 Notes

- **Volumes persist data** - Database and Grafana dashboards survive `down`
- **Images cached** - Subsequent `up` commands are fast (~3-5s)
- **Port 5001** reserved for debugger (may not be needed with vsdbg)
- **macOS users:** `:cached` volume option optimizes performance
