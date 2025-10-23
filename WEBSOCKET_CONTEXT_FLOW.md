# WebSocket Context Flow - Architecture Documentation

This document explains how SignalR WebSocket connections are managed and shared across the application using React Context.

---

## 📋 Table of Contents
1. [Overview](#overview)
2. [React Context Basics](#react-context-basics)
3. [Code Location Reference](#code-location-reference)
4. [Data Flow Diagram](#data-flow-diagram)
5. [Example: Creating a Room](#example-creating-a-room)
6. [WebSocket Connection Lifecycle](#websocket-connection-lifecycle)

---

## Overview

### The Problem We Solved
Each page was creating its own WebSocket connection by calling `useGameHub()` independently. When navigating from `/lobby` to `/room/[roomId]`, the lobby's connection would close, destroying the room on the server.

### The Solution
We use **React Context** to create ONE persistent WebSocket connection at the app level that stays alive across page navigations.

```
Before (❌):
/lobby → useGameHub() → Connection A → Create Room
  ↓ Navigate
/room → useGameHub() → Connection B → Room doesn't exist!
        (Connection A closed when lobby unmounted)

After (✅):
App Root → useGameHub() → Connection A (persistent)
  ├─ /lobby → uses Connection A → Create Room
  └─ /room → uses Connection A → Room exists!
```

---

## React Context Basics

### What is React Context?
Context is React's way to share data across components without passing props through every level (avoiding "prop drilling").

### Our Context Structure
```
RootLayout (layout.tsx)
  └─ <GameHubProvider>              ← Creates ONE WebSocket connection
      ├─ HomePage
      ├─ LobbyPage                  ← Access connection via useGameHubContext()
      └─ RoomPage                   ← Access same connection via useGameHubContext()
```

### Key Files
- **`frontend/src/hooks/useGameHub.ts`** - The hook that manages WebSocket connection
- **`frontend/src/contexts/GameHubContext.tsx`** - Context wrapper
- **`frontend/src/app/layout.tsx`** - App root that provides context
- **`frontend/src/app/lobby/page.tsx`** - Consumes context
- **`frontend/src/app/room/[roomId]/page.tsx`** - Consumes context

---

## Code Location Reference

### File: `frontend/src/hooks/useGameHub.ts`

#### 1. State Variables (Lines 30-36) - Shared Data
```typescript
const [connected, setConnected] = useState(false);           // Connection status
const [error, setError] = useState<string | null>(null);     // Error messages
const [currentRoom, setCurrentRoom] = useState<string | null>(null); // Active room ID
const [gameState, setGameState] = useState<GameView | null>(null);   // Game state
const [rooms, setRooms] = useState<RoomInfo[]>([]);         // Room list
const [matchResult, setMatchResult] = useState<MatchResult | null>(null); // Match result
```

#### 2. Connection Reference (Line 38) - The WebSocket
```typescript
const connectionRef = useRef<signalR.HubConnection | null>(null);
// ↑ This is the actual WebSocket connection object
// ↑ It persists across renders and is used by all action functions
```

#### 3. Connection Initialization (Lines 42-310)
```typescript
useEffect(() => {
  // Create SignalR connection
  const newConnection = buildConnection(signalR.HttpTransportType.WebSockets, true);
  
  // Setup event handlers (Lines 75-141)
  newConnection.on("RoomCreated", (roomId) => setCurrentRoom(roomId));
  newConnection.on("GameStateUpdated", (game) => setGameState(game));
  newConnection.on("RoomsUpdated", (list) => setRooms(list));
  // ... more handlers
  
  // Start connection (Lines 164-292)
  await newConnection.start();
  
  // Cleanup on unmount (Lines 295-309)
  return () => {
    if (conn.state === Connected || Reconnecting) {
      conn.stop();
    }
  };
}, []);
```

#### 4. Server Event Handlers (Lines 75-141) - Server → Client
These update state when the server sends events:

| Line | Event | Updates |
|------|-------|---------|
| 75-79 | `RoomCreated` | `setCurrentRoom(roomId)` |
| 81-85 | `RoomJoined` | `setCurrentRoom(roomId)` |
| 87-92 | `RoomLeft` | Clear room & game state |
| 111-114 | `GameStateUpdated` | `setGameState(game)` |
| 116-119 | `MatchComplete` | `setMatchResult(result)` |
| 121-124 | `RoomsUpdated` | `setRooms(roomList)` |
| 126-132 | `RoomTerminated` | Clear all state |
| 138-141 | `Error` | `setError(errorMessage)` |

#### 5. Action Functions (Lines 313-388) - Client → Server
These send commands to the server:

| Line | Function | Purpose | Server Method |
|------|----------|---------|---------------|
| 313-321 | `createRoom` | Create a new game room | `CreateRoom` |
| 323-331 | `joinRoom` | Join an existing room | `JoinRoom` |
| 333-343 | `leaveRoom` | Leave current room | `LeaveRoom` |
| 345-353 | `setReady` | Toggle ready status | `PlayerReady` |
| 362-370 | `sendAction` | Send game action (bet, fold, etc.) | `PlayerAction` |
| 372-379 | `getActiveRooms` | Fetch room list | `GetActiveRooms` |
| 381-388 | `cancelAutoAdvance` | Cancel auto-advance timer | `CancelAutoAdvance` |

**Example Action Function:**
```typescript
// LINE 313-321
const createRoom = useCallback(async (username: string) => {
  if (!connectionRef.current) return;
  try {
    await connectionRef.current.invoke("CreateRoom", username);
    //    ↑ Sends message through WebSocket to server
  } catch (err) {
    console.error("Error creating room:", err);
    setError("Failed to create room");
  }
}, []);
```

#### 6. Return Object (Lines 394-409) - What Gets Shared
```typescript
return {
  // Connection State
  connected,        // Is WebSocket connected?
  error,            // Any error message?
  
  // Room Data
  currentRoom,      // Current room ID (e.g., "ABC123")
  gameState,        // Current game state (cards, bets, phase)
  rooms,            // List of active rooms
  matchResult,      // Match winner data
  
  // Action Functions
  createRoom,       // Create a new room
  joinRoom,         // Join existing room
  leaveRoom,        // Leave current room
  setReady,         // Toggle ready status
  sendAction,       // Send game action
  getActiveRooms,   // Fetch room list
  cancelAutoAdvance,// Cancel auto-advance timer
  clearError,       // Clear error message
};
```

---

### File: `frontend/src/contexts/GameHubContext.tsx`

This file creates the Context and Provider:

```typescript
// Create the context container
const GameHubContext = createContext<GameHubContextType | null>(null);

// Provider: Wraps the app and creates ONE connection
export function GameHubProvider({ children }: { children: ReactNode }) {
  const gameHub = useGameHub();  // ← Called ONCE when app loads
  
  return (
    <GameHubContext.Provider value={gameHub}>
      {children}  {/* All pages can access gameHub */}
    </GameHubContext.Provider>
  );
}

// Hook: Access the connection from any page
export function useGameHubContext() {
  const context = useContext(GameHubContext);
  if (!context) {
    throw new Error("useGameHubContext must be used within GameHubProvider");
  }
  return context;  // Returns the same gameHub object
}
```

---

### File: `frontend/src/app/layout.tsx`

The root layout wraps everything with the Provider:

```typescript
export default function RootLayout({ children }) {
  return (
    <html lang="en">
      <body>
        <GameHubProvider>  {/* ← Creates connection here */}
          {children}       {/* All pages share this connection */}
        </GameHubProvider>
      </body>
    </html>
  );
}
```

---

### Files: Pages that consume the context

Both `lobby/page.tsx` and `room/[roomId]/page.tsx` use the same pattern:

```typescript
import { useGameHubContext } from "@/contexts/GameHubContext";

export default function LobbyPage() {
  const { 
    connected, 
    createRoom, 
    currentRoom,
    rooms,
    // ... more properties
  } = useGameHubContext();  // ← Gets the shared connection
  
  // Use the connection and state
}
```

---

## Data Flow Diagram

```
┌───────────────────────────────────────────────────────────────────┐
│  Application Root (layout.tsx)                                    │
│  ┌─────────────────────────────────────────────────────────────┐ │
│  │ <GameHubProvider>                                           │ │
│  │   Calls useGameHub() ONCE                                   │ │
│  │   ┌───────────────────────────────────────────────────┐    │ │
│  │   │ useGameHub() Hook                                 │    │ │
│  │   │                                                    │    │ │
│  │   │  ┌─────────────────────────────────────┐          │    │ │
│  │   │  │ WebSocket Connection                │          │    │ │
│  │   │  │ connectionRef.current                │          │    │ │
│  │   │  │ (ws://localhost:5169/gamehub)       │          │    │ │
│  │   │  └──────┬────────────────────┬─────────┘          │    │ │
│  │   │         │                    │                     │    │ │
│  │   │    ┌────▼────────┐    ┌─────▼──────┐             │    │ │
│  │   │    │ Send        │    │ Receive    │             │    │ │
│  │   │    │ Commands    │    │ Events     │             │    │ │
│  │   │    └────┬────────┘    └─────┬──────┘             │    │ │
│  │   │         │                    │                     │    │ │
│  │   │  createRoom()        GameStateUpdated             │    │ │
│  │   │  joinRoom()          RoomCreated                  │    │ │
│  │   │  sendAction()        MatchComplete                │    │ │
│  │   │  leaveRoom()         RoomsUpdated                 │    │ │
│  │   │  setReady()          PlayerJoined                 │    │ │
│  │   │         │                    │                     │    │ │
│  │   │         │            ┌───────▼───────────┐        │    │ │
│  │   │         │            │ Update State      │        │    │ │
│  │   │         │            │ setGameState()    │        │    │ │
│  │   │         │            │ setCurrentRoom()  │        │    │ │
│  │   │         │            │ setRooms()        │        │    │ │
│  │   │         │            │ setMatchResult()  │        │    │ │
│  │   │         │            └───────┬───────────┘        │    │ │
│  │   │         │                    │                     │    │ │
│  │   │  ┌──────▼────────────────────▼──────────┐        │    │ │
│  │   │  │ Return Object                        │        │    │ │
│  │   │  │ {                                    │        │    │ │
│  │   │  │   connected, error, currentRoom,     │        │    │ │
│  │   │  │   gameState, rooms, matchResult,     │        │    │ │
│  │   │  │   createRoom, joinRoom, sendAction,  │        │    │ │
│  │   │  │   leaveRoom, setReady, ...           │        │    │ │
│  │   │  │ }                                    │        │    │ │
│  │   │  └────────────────┬─────────────────────┘        │    │ │
│  │   │                   │                               │    │ │
│  │   └───────────────────┼───────────────────────────────┘    │ │
│  │                       │                                     │ │
│  │      ┌────────────────▼────────────────┐                   │ │
│  │      │ Context.Provider                │                   │ │
│  │      │ value={return object above}     │                   │ │
│  │      └────────────────┬────────────────┘                   │ │
│  └───────────────────────┼───────────────────────────────────┘ │
└──────────────────────────┼───────────────────────────────────────┘
                           │
              ┌────────────┴────────────┐
              │                         │
    ┌─────────▼─────────┐    ┌─────────▼─────────┐
    │ Lobby Page        │    │ Room Page         │
    │ /lobby            │    │ /room/[roomId]    │
    ├───────────────────┤    ├───────────────────┤
    │ useGameHubContext │    │ useGameHubContext │
    │ ↓                 │    │ ↓                 │
    │ Gets:             │    │ Gets:             │
    │ - connected       │    │ - connected       │
    │ - currentRoom     │    │ - gameState       │
    │ - rooms           │    │ - matchResult     │
    │ - createRoom()    │    │ - sendAction()    │
    │ - joinRoom()      │    │ - setReady()      │
    └───────────────────┘    └───────────────────┘
         ↑                            ↑
         │                            │
         └────────────────┬───────────┘
                          │
                    SAME CONNECTION
                    SAME STATE
```

---

## Example: Creating a Room

Let's trace exactly what happens when you click "Create Room" in the lobby:

### Step 1: User Action (Lobby Page)
```typescript
// frontend/src/app/lobby/page.tsx
const handleCreateRoom = async () => {
  if (!username.trim()) return;
  await createRoom(username);  // ← Calls function from context
};
```

### Step 2: Function Execution (useGameHub Hook)
```typescript
// frontend/src/hooks/useGameHub.ts, lines 313-321
const createRoom = useCallback(async (username: string) => {
  if (!connectionRef.current) return;
  try {
    await connectionRef.current.invoke("CreateRoom", username);
    // ↑ Sends message through WebSocket to server
  } catch (err) {
    console.error("Error creating room:", err);
    setError("Failed to create room");
  }
}, []);
```

### Step 3: Server Processing (Backend)
```csharp
// backend/Hubs/GameHub.cs
public async Task CreateRoom(string username)
{
    var roomCode = _gameRoomService.CreateRoom(username, Context.ConnectionId);
    await Clients.Caller.SendAsync("RoomCreated", roomCode);
    // ↑ Sends event back to client
}
```

### Step 4: Event Received (useGameHub Hook)
```typescript
// frontend/src/hooks/useGameHub.ts, lines 75-79
newConnection.on("RoomCreated", (roomId: string) => {
  console.log("Room created:", roomId);
  setCurrentRoom(roomId);  // ← Updates state
  setError(null);
});
```

### Step 5: State Update Triggers Navigation (Lobby Page)
```typescript
// frontend/src/app/lobby/page.tsx, lines 36-43
useEffect(() => {
  if (currentRoom && !isNavigating) {
    console.log("Navigating to room:", currentRoom);
    setIsNavigating(true);
    router.push(`/room/${currentRoom}`);  // ← Navigate to room
  }
}, [currentRoom, router, isNavigating]);
// ↑ Watches currentRoom from context
```

### Step 6: Room Page Loads
```typescript
// frontend/src/app/room/[roomId]/page.tsx
const { 
  connected, 
  currentRoom, 
  gameState,
  // ... 
} = useGameHubContext();  // ← Uses SAME connection!

// The connection is still alive, room still exists
// Can now interact with the game
```

### Visual Timeline
```
Time  │ Action
──────┼───────────────────────────────────────────────────
  0s  │ User clicks "Create Room" button
      │   ↓
  0s  │ createRoom(username) called
      │   ↓
  0s  │ connectionRef.current.invoke("CreateRoom", username)
      │   ↓ (WebSocket message sent)
      │
 50ms │ Server receives "CreateRoom" request
      │   ↓
 50ms │ Server creates room "ABC123"
      │   ↓
 50ms │ Server sends "RoomCreated" event with "ABC123"
      │   ↓ (WebSocket message received)
      │
100ms │ Event handler: setCurrentRoom("ABC123")
      │   ↓
100ms │ React re-renders (currentRoom changed)
      │   ↓
100ms │ useEffect triggers: router.push("/room/ABC123")
      │   ↓
150ms │ Room page loads (connection still alive!)
      │   ↓
150ms │ Room page accesses same gameState, sendAction, etc.
```

---

## WebSocket Connection Lifecycle

### 1. Connection Creation (App Load)
```typescript
// When app loads, GameHubProvider mounts
// ↓
// useGameHub() is called
// ↓
const newConnection = new HubConnectionBuilder()
  .withUrl("http://localhost:5169/gamehub")
  .withAutomaticReconnect()
  .build();
// ↓
await newConnection.start();  // Opens WebSocket
// ↓
console.log("✅ SignalR connected successfully");
```

### 2. Connection Active (During Usage)
```
Browser ⟷ WebSocket ⟷ Server
   ↑                      ↑
   │   Two-way channel    │
   │   Always open        │
   └──────────────────────┘

- User navigates between pages: Connection stays open ✅
- Server sends updates: Client receives immediately
- Client sends actions: Server processes immediately
```

### 3. Event Types

**Client → Server (invoke):**
```typescript
await connection.invoke("CreateRoom", username);
await connection.invoke("PlayerAction", roomId, action);
await connection.invoke("GetActiveRooms");
```

**Server → Client (on):**
```typescript
connection.on("RoomCreated", (roomId) => { ... });
connection.on("GameStateUpdated", (game) => { ... });
connection.on("MatchComplete", (result) => { ... });
```

### 4. Connection Closure (App Close/Refresh)
```typescript
// When app unmounts (browser closes or refresh)
// ↓
return () => {
  const conn = connectionRef.current;
  if (conn && conn.state === Connected) {
    conn.stop();  // Closes WebSocket
  }
};
// ↓
// Server's OnDisconnectedAsync is called
// Server cleans up rooms, removes player
```

### 5. Automatic Reconnection
```typescript
// If connection drops (network issue)
connection.onreconnecting((error) => {
  console.log("SignalR reconnecting...");
  setError("Reconnecting to server...");
});

// When reconnection succeeds
connection.onreconnected(() => {
  console.log("SignalR reconnected");
  setConnected(true);
  setError(null);
});
```

---

## Key Takeaways

### 1. One Connection, Multiple Pages
- ✅ Connection created once in `GameHubProvider`
- ✅ All pages access the same connection via `useGameHubContext()`
- ✅ Navigation doesn't close the connection

### 2. Real-time State Synchronization
- Server sends events → State updates → All pages re-render
- Multiple tabs/windows would need separate connections (current implementation)

### 3. Context Benefits
- No prop drilling
- Centralized connection management
- Persistent state across navigation
- Easy to add new pages that need the connection

### 4. WebSocket Advantages
- Real-time updates (no polling needed)
- Two-way communication
- Low latency (typically <100ms)
- Automatic reconnection on network issues

---

## Related Files

| File | Purpose |
|------|---------|
| `frontend/src/hooks/useGameHub.ts` | WebSocket connection logic |
| `frontend/src/contexts/GameHubContext.tsx` | Context provider |
| `frontend/src/app/layout.tsx` | App root with provider |
| `frontend/src/app/lobby/page.tsx` | Lobby page (consumer) |
| `frontend/src/app/room/[roomId]/page.tsx` | Room page (consumer) |
| `backend/Hubs/GameHub.cs` | Server-side SignalR hub |
| `backend/Services/GameRoomService.cs` | Room management logic |

---

## Additional Resources

- [React Context Documentation](https://react.dev/reference/react/useContext)
- [SignalR JavaScript Client](https://learn.microsoft.com/en-us/aspnet/core/signalr/javascript-client)
- [WebSocket Protocol](https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API)
- [ASP.NET Core SignalR](https://learn.microsoft.com/en-us/aspnet/core/signalr/introduction)

---

*Last updated: October 21, 2025*

