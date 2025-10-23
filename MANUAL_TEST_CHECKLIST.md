# 🎮 Manual Testing Checklist

Use this checklist to verify the game works as you expect before production deployment.

---

## ✅ Automated Tests Status

Run `./test.sh` first - **ALL TESTS PASSED! ✓**

- ✅ Health checks working
- ✅ API endpoints responding
- ✅ Frontend accessible
- ✅ SignalR hub connected
- ✅ Rate limiting active
- ✅ CORS configured

---

## 🎯 Manual Test Scenarios

### **Test 1: Hot-Seat Local Game** (5 minutes)

Open: **http://localhost:3000**

#### Setup:
- [ ] Home page loads with "Play Online" and "Local Hot-Seat" buttons
- [ ] Click "Local Hot-Seat"
- [ ] Game table appears with 2 players

#### Initial State:
- [ ] **Player 1** has 5 cards visible
- [ ] **Player 2** has 5 cards visible
- [ ] Both players have **95 chips** (100 - 5 ante)
- [ ] **Pot shows 10 chips**
- [ ] **Hand 1/10** displayed
- [ ] One player marked as **(Starting)**
- [ ] Active player has **"Your Turn"** indicator

#### Pre-Draw Betting Round:
- [ ] Click **Check** → Switches to other player
- [ ] Other player clicks **Bet 5** → Bet placed, pot increases
- [ ] First player clicks **Call** → Both have 5 committed
- [ ] Phase advances to **Draw**

#### Draw Phase:
- [ ] Starting player has "Your Turn"
- [ ] Select 2 cards to discard (cards highlight when selected)
- [ ] Click **Discard** → Cards are replaced with new ones
- [ ] Still have 5 cards total
- [ ] Turn switches to other player
- [ ] Other player discards 3 cards → Gets 3 new cards
- [ ] Phase advances to **Post-Draw Betting**

#### Post-Draw Betting:
- [ ] Can check/bet/call/raise/fold
- [ ] Betting cap enforced (max 30 chips per street)
- [ ] When both check or one calls, advances to **Showdown**

#### Showdown:
- [ ] Both hands revealed
- [ ] Winner indicated with **🏆**
- [ ] Hand type shown (e.g., "Pair of Aces beats High Card King")
- [ ] Chips awarded correctly
- [ ] **"Next Hand"** button appears

#### Next Hand:
- [ ] Click "Next Hand"
- [ ] **Hand 2/10** displayed
- [ ] Starting player alternates
- [ ] Both players have fresh 5-card hands
- [ ] Antes deducted

#### Match Completion:
- [ ] Play until 10 hands OR one player has 0 chips
- [ ] **"Match Complete!"** message appears
- [ ] Winner announced with final chip count
- [ ] Option to restart game

**Expected Time:** ~5-10 minutes to play through

---

### **Test 2: Multiplayer Online Game** (10 minutes)

**You'll need TWO browser windows (regular + incognito)**

#### Part A: Create Room (Window 1)
Open: **http://localhost:3000**

- [ ] Click **"Play Online"**
- [ ] Enter username: **"Alice"**
- [ ] Click **"Create New Room"**
- [ ] Redirected to room page
- [ ] **6-character room code** displayed (e.g., "ABC123")
- [ ] "Waiting for Players" message
- [ ] **"Ready"** button visible

**Copy the room code!**

---

#### Part B: Join Room (Window 2 - Incognito)
Open: **http://localhost:3000** (in incognito/private window)

- [ ] Click **"Play Online"**
- [ ] Enter username: **"Bob"**
- [ ] Paste the room code
- [ ] Click **"Join Room"**
- [ ] Successfully joined
- [ ] Both windows show "2/2 players"

---

#### Part C: Start Game
**In BOTH windows:**

- [ ] Click **"Ready"** button
- [ ] Game starts automatically when both ready
- [ ] Game table appears

**Alice's Window:**
- [ ] Sees own 5 cards clearly (suits and ranks)
- [ ] Sees Bob's 5 cards as **CARD BACKS** (🂠)
- [ ] Cannot see Bob's card values

**Bob's Window:**
- [ ] Sees own 5 cards clearly
- [ ] Sees Alice's 5 cards as **CARD BACKS** (🂠)
- [ ] Cannot see Alice's card values

**This is CRITICAL - opponent cards must be hidden!**

---

#### Part D: Real-Time Gameplay

**Alice's turn (Window 1):**
- [ ] "Your Turn" indicator shows
- [ ] Click **Check**

**Bob's window (Window 2):**
- [ ] Updates **instantly** (< 1 second)
- [ ] "Your Turn" indicator appears
- [ ] Sees Alice checked in event log
- [ ] Click **Bet 5**

**Alice's window:**
- [ ] Updates instantly
- [ ] Shows Bob bet 5
- [ ] Can now Call/Raise/Fold

Continue playing:
- [ ] Both players can act in sequence
- [ ] Updates are instant
- [ ] No refresh needed

---

#### Part E: Draw Phase Privacy

**Alice discards 2 cards (Window 1):**
- [ ] Selects 2 cards
- [ ] Clicks "Discard"
- [ ] Gets 2 new cards

**Bob's window (Window 2):**
- [ ] Sees event: "Alice discarded 2 cards"
- [ ] **CANNOT see which cards** Alice got
- [ ] Alice's hand still shows as card backs

**Bob discards 3 cards (Window 2):**
- [ ] Selects 3 cards
- [ ] Gets 3 new cards

**Alice's window:**
- [ ] Sees "Bob discarded 3 cards"
- [ ] **CANNOT see Bob's new cards**

---

#### Part F: Showdown Reveal

After final betting round:

**Both windows simultaneously:**
- [ ] **ALL CARDS REVEALED** to both players
- [ ] Can see opponent's full hand
- [ ] Winner determined
- [ ] Hand types shown (e.g., "Three of a Kind")
- [ ] Chips awarded

---

#### Part G: Auto-Advance

After showdown:

**Both windows:**
- [ ] "Next hand starting in **5... 4... 3... 2... 1...**" countdown
- [ ] **"Cancel Auto-Advance"** button appears
- [ ] Hand advances automatically after 5 seconds

**Alternative:**
- [ ] Click "Cancel Auto-Advance"
- [ ] Countdown stops
- [ ] Must manually click "Next Hand"

---

#### Part H: Room List (Window 3 - NEW TAB)

Open: **http://localhost:3000/lobby** (new regular tab)

- [ ] Active rooms list shows Alice & Bob's room
- [ ] Shows **"In Progress"** status
- [ ] Shows current hand number (e.g., "Hand 3/10")
- [ ] Shows pot size
- [ ] **Join button disabled** (room full)

---

#### Part I: Disconnect Handling

**Close Bob's window (Window 2)**

**Alice's window:**
- [ ] "Player Left" notification appears
- [ ] OR room terminates (both are acceptable)

**Lobby (Window 3):**
- [ ] Room disappears from list OR shows 1/2 players

---

#### Part J: Room Cleanup (Optional - Takes 3+ mins)

Leave both players idle for 3+ minutes:

**Both player windows:**
- [ ] After 3 minutes: "Room Terminated" message
- [ ] Auto-redirected to lobby
- [ ] Room no longer in active rooms list

---

### **Test 3: Edge Cases** (5 minutes)

#### Fold Scenario:
- [ ] Start a hand
- [ ] One player clicks **Fold**
- [ ] Other player wins immediately (no showdown)
- [ ] Pot awarded correctly
- [ ] Next hand starts

#### All-In Scenario:
- [ ] Bet all remaining chips
- [ ] Shows "All-In" indicator
- [ ] Opponent can only call/fold
- [ ] Pot awarded correctly

#### Deck Exhaustion:
- [ ] Try to discard 5 cards when deck has only 2 remaining
- [ ] Should limit discard to 2 cards
- [ ] Message: "Max Discard: 2" or "Selected X of 2 max cards"

#### Invalid Actions:
- [ ] Try to bet when you should call → Error or button disabled
- [ ] Try to raise after cap → Raise button disabled
- [ ] Try to act on opponent's turn → No action happens

---

## 🎯 What You're Testing For

### Game Rules Accuracy:
- ✅ Deck: 4A, 4K, 4Q, 4J, 1 Joker (17 cards total)
- ✅ Starting chips: 100 each
- ✅ Ante: 5 per hand
- ✅ Betting: 5-chip increments, 30-chip cap
- ✅ Hand rankings correct
- ✅ Tiebreakers work (rank > suit)

### Multiplayer Privacy:
- ✅ Opponent cards are HIDDEN during play
- ✅ Opponent cards REVEALED at showdown
- ✅ Can't see opponent's discards

### Real-Time Sync:
- ✅ Actions update instantly (< 1 second)
- ✅ Both players see same game state
- ✅ Turn indicator accurate

### UX Quality:
- ✅ Buttons enable/disable appropriately
- ✅ Event log shows recent actions
- ✅ Chip counts update correctly
- ✅ No UI glitches

---

## 🐛 Common Issues to Watch For

### ❌ **CRITICAL Issues (Must Fix):**
- Opponent cards visible in multiplayer
- Game state doesn't sync between players
- Actions don't switch turns
- Showdown doesn't reveal cards
- Chips not awarded correctly

### ⚠️ **Important Issues (Should Fix):**
- Slow updates (> 2 seconds)
- "Your Turn" indicator wrong
- Auto-advance doesn't work
- Room cleanup doesn't trigger
- Event log doesn't update

### 📝 **Minor Issues (Nice to Fix):**
- UI alignment off
- Colors not ideal
- Messages unclear
- Missing visual feedback

---

## ✅ Sign-Off Checklist

Before moving to production:

### Functionality:
- [ ] Local hot-seat mode works end-to-end
- [ ] Multiplayer mode works end-to-end
- [ ] Cards properly hidden in multiplayer
- [ ] All betting actions work
- [ ] Draw phase works correctly
- [ ] Showdown reveals cards
- [ ] Match completes correctly

### Performance:
- [ ] Updates happen in < 1 second
- [ ] No lag during gameplay
- [ ] Page loads in < 3 seconds
- [ ] No memory leaks (play 10+ hands)

### Edge Cases:
- [ ] Fold works
- [ ] All-in works
- [ ] Deck exhaustion handled
- [ ] Invalid actions rejected
- [ ] Disconnect handled gracefully

### Production Readiness:
- [ ] Health checks return 200
- [ ] Rate limiting works
- [ ] CORS configured correctly
- [ ] No console errors
- [ ] Mobile responsive (test on phone)

---

## 🎉 Ready for Production?

If you checked ✅ on all critical items:

**YES! Deploy using [DEPLOYMENT.md](./DEPLOYMENT.md)**

If you found issues:

**FIX FIRST**, then re-test before deploying.

---

**Good luck testing! 🎮**

*Report any bugs you find and we'll fix them before production deployment.*

