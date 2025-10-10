#!/bin/bash

# 17 Poker Development Startup Script
echo "🃏 Starting 17 Poker development servers..."

# Check prerequisites
if ! command -v dotnet &> /dev/null; then
    echo "❌ .NET SDK not found. Please install .NET 8+"
    exit 1
fi

if ! command -v npm &> /dev/null; then
    echo "❌ npm not found. Please install Node.js 18+"
    exit 1
fi

# Create frontend env file if it doesn't exist
if [ ! -f "frontend/.env.local" ]; then
    echo "📝 Creating frontend/.env.local..."
    echo 'NEXT_PUBLIC_API_BASE_URL="http://localhost:5169"' > frontend/.env.local
fi

# Start backend in background
echo "🚀 Starting backend API (http://localhost:5169)..."
cd backend
dotnet run > ../backend.log 2>&1 &
BACKEND_PID=$!
cd ..

# Wait for backend to start
echo "⏳ Waiting for backend to start..."
sleep 5

# Test backend health
if curl -s http://localhost:5169/api/game/new > /dev/null; then
    echo "✅ Backend is running"
else
    echo "❌ Backend failed to start. Check backend.log"
    kill $BACKEND_PID 2>/dev/null
    exit 1
fi

# Start frontend in background
echo "🚀 Starting frontend (http://localhost:3000)..."
cd frontend
npm run dev > ../frontend.log 2>&1 &
FRONTEND_PID=$!
cd ..

# Wait for frontend to start
echo "⏳ Waiting for frontend to start..."
sleep 10

echo ""
echo "🎉 17 Poker is ready!"
echo "🌐 Frontend: http://localhost:3000"
echo "🔧 Backend API: http://localhost:5169"
echo ""
echo "📋 To stop servers:"
echo "   kill $BACKEND_PID $FRONTEND_PID"
echo ""
echo "📊 Logs:"
echo "   Backend: tail -f backend.log"
echo "   Frontend: tail -f frontend.log"
echo ""
echo "🧪 Test API: ./test-api.sh"

# Keep script running
wait
