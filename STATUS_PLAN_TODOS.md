### Context (compact)
- Stack: ASP.NET Core (SignalR), React + Vite + TS, Tailwind/Radix
- Game: 17 Poker heads-up, now with spectator/seat model
- Stability: Fixed SignalR lifecycle and deserialization via PlayerActionDto; Vite proxy OK
- Assets: High-res card images integrated via PlayingCard.tsx with srcset
- Join link: `?join=` auto-join supported (App/Landing)

### Implementation Plan
1. UI/Asset setup
   - Use `face/` images in `frontend/public/assets/cards/`; map rank/suit → filenames; srcset 1x/2x/3x
2. Waiting room overhaul
   - Fix creator name visibility; handle `?join=` on load
   - Spectator mode: join as spectator; take/leave seat; start when 2 seated and ready
3. Gameplay logic
   - Allow 0-card discard
   - Replace antes with mandatory blind bet by starting player
   - Verify all-in logic; restrict raises when opponent all-in
   - Enforce discard-turn: only active player can discard; hide/disable for others

### Implemented
- Backend
  - Spectator/Seat model: join as spectator; `TakeSeat`/`LeaveSeat`; start only when 2 seated + ready
  - Blind bet: starting player posts `min(5, chips)`; removed antes
  - 0-card discard allowed (stand pat)
  - PlayerAction deserialization via PlayerActionDto
- Frontend
  - SignalR client: `takeSeat`/`leaveSeat`, `SeatTaken`/`SeatLeft` events
  - GameContext: manages spectators vs seats, reacts to seat/ready events, exposes seat actions
  - RoomStaging: two-seat UI, spectator list, ready only when seated
  - DrawSelector: 0-card discard enabled with "Stand Pat"
  - URL join: auto-parse and prefill

### To-Dos
- Discard turn UI (Frontend)
  - In `frontend/src/pages/GameTable.tsx`, enable `DrawSelector` only when `gameState.drawPhaseActivePlayer === myPlayerIndex`
- All-in logic review (Backend)
  - In `backend/Models/GameModels.cs` (BettingState), restrict actions when opponent is all-in (no raises; only call/fold; check if no bet)
- Comprehensive testing
  - Room create/join via link, spectator flow, seat selection, ready gating
  - Gameplay: blind bet at hand start, 0-card discard, all-in scenarios, discard-turn enforcement, match flow
