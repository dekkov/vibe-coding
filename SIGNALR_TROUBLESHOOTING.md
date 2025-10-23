# SignalR Connection Troubleshooting Guide

## 🔧 Issue Fixed

**Problem:** SignalR connection was failing with "The connection was stopped during negotiation"

**Root Causes:**
1. ❌ WebSockets not enabled in backend
2. ❌ CORS middleware in wrong order
3. ❌ SignalR client missing transport configuration

**Solution Applied:**

### Backend (Program.cs)
```csharp
// Added WebSocket support
app.UseWebSockets();

// Moved CORS BEFORE other middleware
app.UseCors(CorsPolicyName);  // Must be early in pipeline
```

### Frontend (useGameHub.ts)
```typescript
// Added explicit transport configuration
.withUrl(hubUrl, {
  skipNegotiation: false,
  transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
  withCredentials: true,
})
```

---

## ✅ How to Verify It's Fixed

### 1. Wait for Servers to Start (10 seconds)
```bash
sleep 10
```

### 2. Open Browser Console
1. Open http://localhost:3000/lobby
2. Press **F12** to open DevTools
3. Go to **Console** tab

### 3. Look for Success Message
You should see:
```
✅ SignalR connected successfully
```

### 4. Check Network Tab
1. Go to **Network** tab in DevTools
2. Filter by **WS** (WebSockets)
3. You should see a connection to `localhost:5169/gamehub`
4. Status should be **101 Switching Protocols**

---

## 🐛 If Still Getting Errors

### Error 1: "Failed to complete negotiation"

**Check:**
```bash
# Test the negotiate endpoint
curl -X POST http://localhost:5169/gamehub/negotiate \
  -H "Content-Type: application/json"

# Should return JSON with connectionId
```

**Solution:**
- Ensure backend is running: `lsof -i :5169`
- Restart backend: `pkill -f "dotnet run" && cd backend && dotnet run &`

---

### Error 2: "CORS policy blocked"

**Check Browser Console:**
```
Access to XMLHttpRequest at 'http://localhost:5169/gamehub/negotiate' 
from origin 'http://localhost:3000' has been blocked by CORS policy
```

**Solution:**
Check backend CORS configuration in `Program.cs`:
```csharp
.WithOrigins("http://localhost:3000")  // Must match frontend URL
.AllowCredentials()  // Required for SignalR
```

---

### Error 3: "WebSocket connection failed"

**Check:**
```bash
# Test WebSocket endpoint (using websocat if installed)
websocat ws://localhost:5169/gamehub

# Or check with curl
curl -i -N \
  -H "Connection: Upgrade" \
  -H "Upgrade: websocket" \
  -H "Sec-WebSocket-Version: 13" \
  -H "Sec-WebSocket-Key: dGhlIHNhbXBsZSBub25jZQ==" \
  http://localhost:5169/gamehub
```

**Solution:**
Ensure `app.UseWebSockets();` is in `Program.cs` BEFORE `app.UseCors()`

---

### Error 4: Connection drops immediately

**Browser Console:**
```
SignalR connection closed Error: ...
```

**Check:**
1. Backend logs for errors
2. Firewall blocking WebSockets
3. Antivirus interfering

**Solution:**
```bash
# Check backend logs
tail -f /path/to/backend/logs

# Try with long-polling only (in useGameHub.ts):
transport: signalR.HttpTransportType.LongPolling  // Remove WebSockets
```

---

## 📊 Connection Lifecycle

### Normal Flow:
```
1. Client → POST /gamehub/negotiate → Get connectionId
2. Client → Upgrade to WebSocket → ws://localhost:5169/gamehub
3. Client → Send handshake
4. Server → Accept connection
5. ✅ Connected
```

### What You Should See in Browser Console:
```
Information: WebSocket connected to ws://localhost:5169/gamehub
Information: HubConnection connected successfully
✅ SignalR connected successfully
```

---

## 🔍 Debug Mode

To get more detailed logs, update `useGameHub.ts`:

```typescript
.configureLogging(signalR.LogLevel.Debug)  // Change from Information
```

Then in browser console, you'll see:
- Negotiation details
- Transport selection
- Handshake messages
- Keep-alive pings

---

## 🚀 Test SignalR is Working

### Test 1: Create Room
```typescript
// In browser console (on lobby page):
const testCreate = async () => {
  // Wait for page to load, then check:
  console.log('Testing room creation...');
};
```

1. Enter username
2. Click "Create Room"
3. Should redirect to room page
4. Room code should appear

**If it redirects back to username screen:**
- SignalR not connected
- Check console for errors

### Test 2: Room List Updates
1. Create a room in one tab
2. Open lobby in another tab
3. Room should appear in list within 1 second

**If room doesn't appear:**
- SignalR not broadcasting
- Check backend logs

---

## 🔧 Quick Fixes

### Fix 1: Clean Restart
```bash
# Stop everything
pkill -f "dotnet run"
pkill -f "next dev"

# Clear build cache
cd backend && dotnet clean
cd ../frontend && rm -rf .next

# Start fresh
cd ../backend && dotnet run &
cd ../frontend && npm run dev &
```

### Fix 2: Check Ports
```bash
# Ensure ports are free
lsof -i :5169  # Backend
lsof -i :3000  # Frontend

# Kill conflicting processes
kill -9 <PID>
```

### Fix 3: Disable Rate Limiting Temporarily
In `Program.cs`, comment out:
```csharp
// app.UseIpRateLimiting();  // Temporarily disabled for debugging
```

---

## ✅ Success Indicators

### In Browser Console:
- ✅ "SignalR connected successfully"
- ✅ No red error messages
- ✅ Network tab shows WS connection

### In Backend Console:
- ✅ "Client connected: {ConnectionId}"
- ✅ No SignalR errors

### In Application:
- ✅ Can create room
- ✅ Can join room
- ✅ Room list updates
- ✅ No redirect loops

---

## 📞 Still Having Issues?

### Collect Debug Info:
```bash
# Backend health
curl http://localhost:5169/health

# SignalR negotiate
curl -X POST http://localhost:5169/gamehub/negotiate \
  -H "Content-Type: application/json"

# Check CORS
curl -i -H "Origin: http://localhost:3000" \
  http://localhost:5169/health

# Browser console screenshot
# Network tab screenshot (filtered to WS)
```

---

## 🎉 Expected Working State

When everything is working:

1. **Open lobby:** http://localhost:3000/lobby
2. **Console shows:**
   ```
   ✅ SignalR connected successfully
   ```
3. **Can create room** → Redirects to room page
4. **Can join room** → Game starts
5. **Real-time updates** → Actions sync instantly

**Ready to test!** 🚀

