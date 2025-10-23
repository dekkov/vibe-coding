# 17 Poker - Testing Guide

This guide helps you verify that all features work as expected before moving to production.

## 🚀 Quick Start - Servers Running

✅ **Backend:** Running on http://localhost:5169  
✅ **Frontend:** Running on http://localhost:3000

---

## 📋 Pre-Flight Checklist

### 1. Health Checks
```bash
# Test backend health
curl http://localhost:5169/health

# Expected response:
# {"status":"healthy","timestamp":"...","service":"17-poker-backend"}

# Test detailed status
curl http://localhost:5169/health/status

# Should show:
# - Active rooms count
# - Memory usage
# - Uptime
```

### 2. API Endpoints
```bash
# Test game creation (hot-seat mode)
curl -X POST http://localhost:5169/api/game/new \
  -H "Content-Type: application/json"

# Should return game state with 2 players, 5 cards each
```

---

## 🎮 Manual Testing Scenarios

### **Scenario 1: Hot-Seat Local Game (Phase 1)**

#### Steps:
1. Open browser: **http://localhost:3000**
2. Click **"Local Hot-Seat"** button
3. Verify you see:
   - ✅ Both players displayed
   - ✅ Each player has 5 cards
   - ✅ Both players have 95 chips (after 5 chip ante)
   - ✅ Pot shows 10 chips
   - ✅ "Hand 1/10" displayed
   - ✅ One player marked as "Starting"

#### Test Pre-Draw Betting:
4. Click **Check** → Should switch to other player
5. Other player clicks **Bet 5** → Should show bet placed
6. First player clicks **Call** → Should advance to Draw phase

#### Test Draw Phase:
7. Starting player selects 0-5 cards to discard
8. Click **Discard** → Cards should be replaced
9. Other player discards cards → Both have 5 cards again

#### Test Post-Draw Betting:
10. Repeat betting (check/bet/call/raise)
11. Action should close when both players check or call

#### Test Showdown:
12. Verify:
    - ✅ Both hands revealed
    - ✅ Winner indicated with 🏆
    - ✅ Hand type shown (e.g., "Pair of Aces")
    - ✅ Chips awarded to winner
    - ✅ "Next Hand" button appears

#### Test Next Hand:
13. Click **"Next Hand"** → Should start Hand 2/10
14. Button should be on opposite player (alternating starting player)

#### Test Match Completion:
15. Play through 10 hands OR until one player runs out of chips
16. Verify:
    - ✅ "Match Complete!" message
    - ✅ Winner announced with chip count
    - ✅ Option to start new game

---

### **Scenario 2: Multiplayer Online Game (Phase 2)**

#### Part A: Create Room
1. Open browser: **http://localhost:3000**
2. Click **"Play Online"** button
3. Enter username (e.g., "Alice")
4. Click **"Create New Room"**
5. Verify:
   - ✅ Redirected to room page
   - ✅ 6-character room code displayed (e.g., "ABC123")
   - ✅ "Waiting for Players" message
   - ✅ "Ready" button visible

#### Part B: Join Room (Second Browser/Tab)
6. Open **INCOGNITO/PRIVATE window**: **http://localhost:3000**
7. Click **"Play Online"**
8. Enter different username (e.g., "Bob")
9. Enter the room code from step 5
10. Click **"Join Room"**
11. Verify:
    - ✅ Successfully joined
    - ✅ Both players see each other's names
    - ✅ Both see "Ready" button

#### Part C: Start Game
12. Both players click **"Ready"**
13. Verify:
    - ✅ Game starts automatically
    - ✅ Both players see the game table
    - ✅ Each player sees their own 5 cards
    - ✅ **Opponent's cards are hidden (card backs 🂠)**

#### Part D: Play a Hand
14. **Alice's browser:** Make an action (check/bet/call)
15. **Bob's browser:** Verify:
    - ✅ Game state updates in real-time
    - ✅ "Your Turn" indicator appears
16. Continue playing through:
    - ✅ Pre-draw betting
    - ✅ Draw phase (each player discards privately)
    - ✅ Post-draw betting
17. At showdown:
    - ✅ Both hands are revealed to both players
    - ✅ Winner announced
    - ✅ 5-second countdown to next hand

#### Part E: Auto-Advance
18. After showdown, verify:
    - ✅ "Next hand starting in 5... 4... 3... 2... 1..." countdown
    - ✅ "Cancel Auto-Advance" button appears
    - ✅ Hand advances automatically after 5 seconds
    - OR click "Cancel" and manually advance

#### Part F: Room List
19. Open **THIRD browser tab**: **http://localhost:3000/lobby**
20. Verify:
    - ✅ Active rooms list shows Alice & Bob's room
    - ✅ Shows "In Progress" status
    - ✅ Shows current hand number
    - ✅ Shows pot size
    - ✅ Join button is disabled (room full)

#### Part G: Player Disconnect
21. Close Bob's browser tab
22. In Alice's browser, verify:
    - ✅ "Player Left" notification
    - OR room terminates (depending on implementation)

#### Part H: Room Cleanup
23. Leave both players idle for 3+ minutes
24. Verify:
    - ✅ "Room Terminated" message appears
    - ✅ Players redirected to lobby
    - ✅ Room no longer in active rooms list

---

### **Scenario 3: Edge Cases & Error Handling**

#### Test Invalid Actions:
1. Try to bet when you should call → Should reject with error message
2. Try to discard 6 cards when deck has only 2 → Should limit to 2
3. Try to raise after cap reached → Should disable raise button

#### Test All-In Scenarios:
1. Bet all your chips → Should show "All-In"
2. Opponent calls with fewer chips → Should create side pot logic
3. Win with all-in → Chips awarded correctly

#### Test Fold Functionality:
1. One player folds during betting
2. Verify:
   - ✅ Other player wins immediately
   - ✅ No showdown occurs
   - ✅ Pot awarded to non-folded player
   - ✅ Next hand starts

#### Test Draw Phase Edge Cases:
1. Discard 0 cards (stand pat) → Should work
2. Discard all 5 cards → Should get 5 new cards
3. Deck runs out → Should limit discard to available cards

#### Test Match End Conditions:
1. One player reaches 0 chips → Match ends immediately
2. Play exactly 10 hands → Match ends, winner has most chips
3. Tie after 10 hands → Should declare tie or tiebreaker

---

### **Scenario 4: Rate Limiting (Phase 3)**

#### Test API Rate Limits:
```bash
# Send 100 requests rapidly
for i in {1..100}; do
  curl -X POST http://localhost:5169/api/game/new \
    -H "Content-Type: application/json" &
done
wait

# After ~60 requests, should get:
# HTTP 429 Too Many Requests
```

#### Expected Behavior:
- ✅ First 60 requests succeed
- ✅ Requests 61+ return 429 error
- ✅ After 1 minute, can make requests again

---

### **Scenario 5: Input Validation (Phase 3)**

#### Test Invalid Content-Type:
```bash
curl -X POST http://localhost:5169/api/game/action \
  -H "Content-Type: text/plain" \
  -d "invalid"

# Expected: HTTP 415 Unsupported Media Type
```

#### Test Large Payload:
```bash
# Create a 2MB file
dd if=/dev/zero of=/tmp/large.json bs=1M count=2

curl -X POST http://localhost:5169/api/game/action \
  -H "Content-Type: application/json" \
  -d @/tmp/large.json

# Expected: HTTP 413 Payload Too Large
```

#### Test Invalid Room Code:
```bash
curl http://localhost:5169/api/room/INVALID123

# Expected: HTTP 400 Bad Request
# "Invalid room code format"
```

---

## 🔍 Automated Testing Script

Save this as `test.sh`:

```bash
#!/bin/bash

echo "🧪 Testing 17 Poker Application"
echo "================================"

# Colors
GREEN='\033[0;32m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Test counter
PASSED=0
FAILED=0

# Test function
test_endpoint() {
    local name=$1
    local url=$2
    local expected_code=$3
    
    echo -n "Testing $name... "
    response=$(curl -s -o /dev/null -w "%{http_code}" "$url")
    
    if [ "$response" -eq "$expected_code" ]; then
        echo -e "${GREEN}✓ PASSED${NC} (HTTP $response)"
        ((PASSED++))
    else
        echo -e "${RED}✗ FAILED${NC} (Expected $expected_code, got $response)"
        ((FAILED++))
    fi
}

# Health checks
test_endpoint "Backend Health" "http://localhost:5169/health" 200
test_endpoint "Backend Ready" "http://localhost:5169/health/ready" 200
test_endpoint "Backend Live" "http://localhost:5169/health/live" 200
test_endpoint "Backend Status" "http://localhost:5169/health/status" 200

# API endpoints
echo ""
echo "Testing API endpoints..."
test_endpoint "Create Game" "http://localhost:5169/api/game/new" 200

# Frontend
test_endpoint "Frontend Home" "http://localhost:3000" 200
test_endpoint "Frontend Lobby" "http://localhost:3000/lobby" 200

# Invalid endpoints
echo ""
echo "Testing error handling..."
# These should fail gracefully
curl -s http://localhost:5169/api/game/invalid > /dev/null && ((PASSED++)) || ((FAILED++))

echo ""
echo "================================"
echo "Results: ${GREEN}$PASSED passed${NC}, ${RED}$FAILED failed${NC}"

if [ $FAILED -eq 0 ]; then
    echo -e "${GREEN}🎉 All tests passed!${NC}"
    exit 0
else
    echo -e "${RED}❌ Some tests failed${NC}"
    exit 1
fi
```

Run it:
```bash
chmod +x test.sh
./test.sh
```

---

## 🎯 Expected vs Actual Behavior Checklist

Go through this with your game:

### Game Rules
- [ ] Deck has exactly 17 cards (4A, 4K, 4Q, 4J, 1 Joker)
- [ ] Each player starts with 100 chips
- [ ] Ante is 5 chips per hand
- [ ] Betting increments are 5 chips
- [ ] Betting cap is 30 chips per street (6 raises max)
- [ ] Two betting rounds (pre-draw, post-draw)
- [ ] Players can discard 0-5 cards
- [ ] Match ends after 10 hands or when player has 0 chips

### Hand Rankings (Strongest to Weakest)
- [ ] Five of a Kind (4 + Joker)
- [ ] Four of a Kind
- [ ] Full House
- [ ] Three of a Kind
- [ ] Two Pair
- [ ] One Pair
- [ ] High Card

### Tiebreakers
- [ ] Higher rank wins (A > K > Q > J)
- [ ] If same rank, suit hierarchy: Spades > Hearts > Diamonds > Clubs
- [ ] Joker acts as highest card when tied

### UI/UX
- [ ] Cards display correctly with suits (♠ ♥ ♦ ♣)
- [ ] Joker shows as 🃏
- [ ] Opponent cards hidden in multiplayer
- [ ] Real-time updates in multiplayer
- [ ] Starting player indicator visible
- [ ] Chip counts update correctly
- [ ] Pot size accurate
- [ ] Event log shows recent actions

### Multiplayer
- [ ] Room codes are 6 characters, alphanumeric
- [ ] Room codes are unique (no collisions)
- [ ] SignalR connection works
- [ ] Both players see updates instantly
- [ ] Auto-advance works (5 second countdown)
- [ ] Room cleanup after 3 minutes inactive
- [ ] Room list updates in real-time

---

## 🐛 Common Issues & Solutions

### Issue: Cards not displaying
**Solution:** Check browser console for errors, verify API is returning card data

### Issue: SignalR not connecting
**Solution:** 
- Check CORS settings
- Verify WebSocket support
- Check browser console for connection errors

### Issue: "Your Turn" indicator wrong player
**Solution:** Check `drawPhaseActivePlayer` logic in backend

### Issue: Opponent cards visible in multiplayer
**Solution:** Backend should send masked cards (null rank/suit)

### Issue: Auto-advance not working
**Solution:** Check timer in `GameRoomService`, verify hub broadcasts

### Issue: Room not cleaning up
**Solution:** Check cleanup timer interval (should be 1 minute)

---

## 📊 Performance Benchmarks

Run these to verify performance is acceptable:

```bash
# Test response time
time curl http://localhost:5169/health

# Should be < 100ms

# Test concurrent requests
ab -n 1000 -c 10 http://localhost:5169/health

# Check:
# - Requests per second > 100
# - Mean response time < 100ms
# - No failed requests
```

---

## ✅ Production Readiness Checklist

Before deploying:

### Backend
- [ ] Health checks return 200
- [ ] All API endpoints respond correctly
- [ ] Rate limiting works (returns 429)
- [ ] Input validation rejects invalid data
- [ ] Logging captures important events
- [ ] No memory leaks (run for 1+ hour)
- [ ] Handles disconnections gracefully

### Frontend  
- [ ] All pages load correctly
- [ ] SignalR connects successfully
- [ ] UI responsive on mobile
- [ ] No console errors
- [ ] Cards render properly
- [ ] Real-time updates work

### Multiplayer
- [ ] Rooms create successfully
- [ ] Players can join with code
- [ ] Game state syncs between players
- [ ] Opponent cards hidden
- [ ] Auto-advance works
- [ ] Room cleanup works
- [ ] Handles player disconnect

### Edge Cases
- [ ] All-in scenarios work
- [ ] Fold functionality works
- [ ] Deck exhaustion handled
- [ ] Match end conditions correct
- [ ] Tie scenarios handled

---

## 🎬 Next Steps

After testing:

1. ✅ **All tests pass** → Ready for production deployment
2. ⚠️ **Some issues found** → Fix and re-test
3. ❌ **Major issues** → Review implementation

Use the [DEPLOYMENT.md](./DEPLOYMENT.md) guide to deploy to Azure/AWS once testing is complete!

---

**Happy Testing! 🎮**

