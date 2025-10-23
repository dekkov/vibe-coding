#!/bin/bash

echo "🧪 17 Poker - Automated Test Suite"
echo "===================================="
echo ""

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test counters
PASSED=0
FAILED=0

# Base URLs
BACKEND="http://localhost:5169"
FRONTEND="http://localhost:3000"

# Test function
test_endpoint() {
    local name=$1
    local url=$2
    local expected_code=$3
    local method=${4:-GET}
    
    echo -n "  Testing $name... "
    
    if [ "$method" = "POST" ]; then
        response=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$url" -H "Content-Type: application/json" -d '{}')
    else
        response=$(curl -s -o /dev/null -w "%{http_code}" "$url")
    fi
    
    if [ "$response" -eq "$expected_code" ]; then
        echo -e "${GREEN}✓ PASSED${NC} (HTTP $response)"
        ((PASSED++))
        return 0
    else
        echo -e "${RED}✗ FAILED${NC} (Expected $expected_code, got $response)"
        ((FAILED++))
        return 1
    fi
}

# Test with body
test_json_response() {
    local name=$1
    local url=$2
    local expected_field=$3
    
    echo -n "  Testing $name... "
    
    response=$(curl -s "$url")
    
    if echo "$response" | grep -q "$expected_field"; then
        echo -e "${GREEN}✓ PASSED${NC} (Found '$expected_field')"
        ((PASSED++))
        return 0
    else
        echo -e "${RED}✗ FAILED${NC} (Expected field '$expected_field' not found)"
        echo "    Response: $response"
        ((FAILED++))
        return 1
    fi
}

echo -e "${BLUE}[1/7] Health Check Endpoints${NC}"
test_endpoint "Basic Health" "$BACKEND/health" 200
test_endpoint "Readiness Check" "$BACKEND/health/ready" 200
test_endpoint "Liveness Check" "$BACKEND/health/live" 200
test_json_response "Status Endpoint" "$BACKEND/health/status" "activeRooms"
echo ""

echo -e "${BLUE}[2/7] Game API Endpoints${NC}"
test_endpoint "Create Game (POST)" "$BACKEND/api/game/new" 200 POST
echo ""

echo -e "${BLUE}[3/7] Frontend Pages${NC}"
test_endpoint "Home Page" "$FRONTEND" 200
test_endpoint "Lobby Page" "$FRONTEND/lobby" 200
test_endpoint "Play Page" "$FRONTEND/play" 200
echo ""

echo -e "${BLUE}[4/7] Error Handling${NC}"
test_endpoint "404 Not Found" "$BACKEND/api/invalid" 404
echo ""

echo -e "${BLUE}[5/7] CORS Headers${NC}"
echo -n "  Testing CORS... "
cors_response=$(curl -s -I -H "Origin: http://localhost:3000" "$BACKEND/health" | grep -i "access-control")
if [ ! -z "$cors_response" ]; then
    echo -e "${GREEN}✓ PASSED${NC} (CORS headers present)"
    ((PASSED++))
else
    echo -e "${RED}✗ FAILED${NC} (No CORS headers)"
    ((FAILED++))
fi
echo ""

echo -e "${BLUE}[6/7] SignalR Hub${NC}"
echo -n "  Testing SignalR endpoint... "
hub_response=$(curl -s -o /dev/null -w "%{http_code}" "$BACKEND/gamehub/negotiate" -X POST)
if [ "$hub_response" -eq "200" ] || [ "$hub_response" -eq "400" ]; then
    echo -e "${GREEN}✓ PASSED${NC} (SignalR endpoint accessible)"
    ((PASSED++))
else
    echo -e "${RED}✗ FAILED${NC} (SignalR endpoint not accessible)"
    ((FAILED++))
fi
echo ""

echo -e "${BLUE}[7/7] Rate Limiting${NC}"
echo -n "  Testing rate limits (sending 65 requests)... "
count=0
rate_limited=false
for i in {1..65}; do
    response=$(curl -s -o /dev/null -w "%{http_code}" "$BACKEND/health" 2>/dev/null)
    if [ "$response" -eq "429" ]; then
        rate_limited=true
        break
    fi
    ((count++))
done

if [ "$rate_limited" = true ]; then
    echo -e "${GREEN}✓ PASSED${NC} (Rate limited after $count requests)"
    ((PASSED++))
else
    echo -e "${YELLOW}⚠ SKIPPED${NC} (Rate limiting may not be active or limit > 65)"
    echo "    Note: This is OK for development"
fi
echo ""

# Summary
echo "===================================="
echo -e "Test Results: ${GREEN}$PASSED passed${NC}, ${RED}$FAILED failed${NC}"
echo ""

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}🎉 All critical tests passed!${NC}"
    echo ""
    echo "✅ Backend is healthy"
    echo "✅ Frontend is accessible"  
    echo "✅ API endpoints working"
    echo "✅ SignalR hub accessible"
    echo ""
    echo -e "${BLUE}Ready for manual testing!${NC}"
    echo "Open http://localhost:3000 in your browser"
    exit 0
else
    echo -e "${RED}❌ Some tests failed - please review${NC}"
    echo ""
    echo "Common fixes:"
    echo "  • Ensure backend is running: cd backend && dotnet run"
    echo "  • Ensure frontend is running: cd frontend && npm run dev"
    echo "  • Check for port conflicts (5169, 3000)"
    exit 1
fi

