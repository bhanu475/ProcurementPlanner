# Docker Setup for Procurement Planner

This guide explains how to run the entire Procurement Planner application stack using Docker Compose.

## 🏗️ Architecture

The Docker setup includes:

- **Frontend**: React app served by Nginx (Port 3000)
- **Backend**: .NET 9 Web API (Port 5000)
- **Database**: SQL Server 2022 Express (Port 1433)
- **Cache**: Redis (Port 6379)

## 🚀 Quick Start

### Prerequisites

- Docker Desktop installed and running
- At least 4GB RAM available for containers
- Ports 3000, 5000, 1433, and 6379 available

### Start the Application

```bash
# Make scripts executable (first time only)
chmod +x start-docker.sh stop-docker.sh

# Start all services
./start-docker.sh
```

### Stop the Application

```bash
./stop-docker.sh
```

## 📋 Manual Commands

### Build and Start

```bash
# Build and start all services
docker-compose up --build -d

# View logs
docker-compose logs -f

# View logs for specific service
docker-compose logs -f backend
docker-compose logs -f frontend
```

### Stop and Clean Up

```bash
# Stop services
docker-compose down

# Stop and remove volumes (clears database)
docker-compose down -v

# Stop and remove images
docker-compose down --rmi all

# Complete cleanup
docker-compose down -v --rmi all --remove-orphans
```

## 🌐 Access Points

Once running, access the application at:

- **Frontend**: http://localhost:3000
- **Backend API**: http://localhost:5000
- **Health Check**: http://localhost:5000/health
- **OpenAPI Docs**: http://localhost:5000/openapi/v1.json

## 🗄️ Database Access

**SQL Server Connection:**
- Server: `localhost,1433`
- Username: `sa`
- Password: `YourStrong@Passw0rd`
- Database: `ProcurementPlannerDb`

**Redis Connection:**
- Host: `localhost`
- Port: `6379`

## 🔧 Configuration

### Environment Variables

The Docker Compose file sets up the following key configurations:

**Backend:**
- Database connection to SQL Server container
- Redis connection to Redis container
- JWT settings for authentication
- CORS enabled for frontend

**Frontend:**
- API base URL pointing to backend container
- Nginx proxy for API calls

### Volumes

- `sqlserver_data`: Persists SQL Server database files
- `redis_data`: Persists Redis cache data
- `./backend/logs`: Backend application logs

## 🐛 Troubleshooting

### Common Issues

1. **Port conflicts**: Ensure ports 3000, 5000, 1433, 6379 are available
2. **Memory issues**: Increase Docker Desktop memory allocation
3. **Build failures**: Clear Docker cache with `docker system prune -a`

### Health Checks

All services include health checks. Check status with:

```bash
docker-compose ps
```

### View Logs

```bash
# All services
docker-compose logs

# Specific service
docker-compose logs backend
docker-compose logs frontend
docker-compose logs sqlserver
docker-compose logs redis
```

### Restart Single Service

```bash
docker-compose restart backend
docker-compose restart frontend
```

## 🔄 Development Workflow

### Code Changes

**Backend Changes:**
- Rebuild: `docker-compose up --build backend`
- The container will reflect changes after rebuild

**Frontend Changes:**
- Rebuild: `docker-compose up --build frontend`
- For development, consider running frontend locally with `npm run dev`

### Database Changes

- Database schema changes require rebuilding the backend
- To reset database: `docker-compose down -v && docker-compose up -d`

## 📊 Monitoring

### Container Status

```bash
# View running containers
docker ps

# View resource usage
docker stats

# View container details
docker inspect procurement-backend
```

### Application Logs

```bash
# Follow all logs
docker-compose logs -f

# Follow specific service logs
docker-compose logs -f backend

# View last 100 lines
docker-compose logs --tail=100 backend
```

## 🔒 Security Notes

- Default passwords are used for development
- Change passwords in production environments
- SQL Server uses Express edition (development only)
- JWT secret should be changed for production

## 🚀 Production Considerations

For production deployment:

1. Use environment-specific configuration files
2. Set up proper secrets management
3. Configure SSL/TLS certificates
4. Use production-grade database
5. Set up monitoring and logging
6. Configure backup strategies
7. Implement proper security measures

## 📝 Additional Commands

```bash
# View Docker Compose configuration
docker-compose config

# Pull latest images
docker-compose pull

# Remove unused Docker resources
docker system prune

# View Docker networks
docker network ls

# View Docker volumes
docker volume ls
```