# Small Bugs Fixed

## 1. ✅ FIXED: Player joining room via code can't choose ready status

**Problem:**
When a player joins a room via room code, they immediately see the game table instead of the "Waiting for Players" screen with the ready button.

**Root Cause:**
In `GameHub.cs`, when a player joins a room via `JoinRoom()`, the backend was immediately sending a `GameStateUpdated` event with the current game state, even if the room status was still `Waiting`. This caused the frontend to think the game had already started, skipping the waiting screen.

**Fix:**
Modified `GameHub.JoinRoom()` to only send `GameStateUpdated` if `room.Status == GameStatus.InProgress`. Players joining a waiting room will now see the "Waiting for Players" screen with the ready button.

**Files Changed:**
- `backend/Hubs/GameHub.cs` (lines 93-102)

**Testing:**
1. Create a room with Player 1
2. Join the room with Player 2 via room code
3. Both players should see the "Waiting for Players" screen
4. Both players can click "Click when ready"
5. When both are ready, game starts automatically

---

## 2. ✅ FIXED: Player ready status UI not showing

**Problem:**
The player ready status UI was implemented but not showing in the waiting room. Players could see the debug information showing `playersInRoom = []` (empty array).

**Root Cause:**
SignalR group timing issue. When players created or joined rooms, the `PlayerJoined` event was sent before they were added to the SignalR group, so they never received their own `PlayerJoined` event.

**Fix:**
Reordered operations in `GameHub.cs`:
- **Before**: `JoinRoom()` → `Groups.AddToGroupAsync()` (too late!)
- **After**: `Groups.AddToGroupAsync()` → `JoinRoom()` (receives events)

**Files Changed:**
- `backend/Hubs/GameHub.cs` - Fixed timing in `CreateRoom()` and `JoinRoom()` methods

**Testing:**
1. Create a room - should see "Players (1/2)" with your username
2. Join a room - should see "Players (2/2)" with both usernames
3. Ready status should update in real-time

---

## 3. ✅ ADDED: Show player ready status in waiting room

**Feature:**
Players can now see each other's ready status in the waiting room before the game starts.

**Implementation:**
1. Added `playersReady` and `playersInRoom` state tracking to `useGameHub` hook
2. Updated `PlayerReadyChanged`, `PlayerJoined`, and `PlayerLeft` event handlers to update state
3. Added a "Players Status" section in the waiting room showing:
   - List of all players in the room
   - Ready status for each player (✓ Ready or Waiting...)
   - Player count (X/2)

**Files Changed:**
- `frontend/src/hooks/useGameHub.ts` - Added state tracking and event handlers
- `frontend/src/contexts/GameHubContext.tsx` - Updated type imports
- `frontend/src/app/room/[roomId]/page.tsx` - Added UI to display player status

**UI Features:**
- 👤 Icon for each player
- Green checkmark (✓ Ready) when player is ready
- Gray "Waiting..." text when player is not ready
- Clear visual distinction between ready/not ready states

---

## 4. ✅ FIXED: Joining players only see themselves, not existing players

**Problem:**
When a second player joins a room via room code, they only see themselves in the player list (showing "Players (1/2)") instead of seeing both players.

**Root Cause:**
The `PlayerJoined` event was only sent for the newly joining player, but not for existing players in the room. So the new joiner never received information about who was already in the room.

**Fix:**
Modified `GameRoomService.JoinRoom()` to send information about all existing players to the new joiner before notifying everyone about the new player.

**Files Changed:**
- `backend/Services/GameRoomService.cs` - Added loop to send existing player info to new joiner

**Testing:**
1. Player 1 creates room - sees "Players (1/2)" with their username
2. Player 2 joins room - now sees "Players (2/2)" with both usernames
3. Both players can see each other's ready status in real-time
