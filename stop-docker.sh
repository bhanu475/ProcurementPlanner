#!/bin/bash

echo "🛑 Stopping Procurement Planner services..."

# Stop all services
docker-compose down

echo "✅ All services stopped!"
echo ""
echo "💡 Options:"
echo "   - To restart: ./start-docker.sh"
echo "   - To clean up volumes: docker-compose down -v"
echo "   - To remove images: docker-compose down --rmi all"