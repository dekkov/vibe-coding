# 17 Poker

A unique heads-up poker variant played with just 17 cards: 4 Aces, 4 Kings, 4 Queens, 4 Jacks, and 1 wild Joker.

## 🎮 Game Features

- **Heads-up gameplay**: Two players compete over 10 hands
- **Fixed-limit betting**: 5 chip increments, 30 chip cap per street
- **Draw poker mechanics**: Discard and redraw cards between betting rounds
- **Smart Joker handling**: Automatically optimizes hand strength
- **Hot-seat multiplayer**: Two players take turns on the same device
- **Real-time UI**: Responsive interface with visual feedback

## 🏗️ Architecture

- **Frontend**: Next.js 15 with TypeScript and Tailwind CSS
- **Backend**: ASP.NET Core Web API with C#
- **State Management**: In-memory game sessions
- **Communication**: REST API with JSON

## 📋 Prerequisites

- Node.js 18+ and npm
- .NET SDK 8+
- Modern web browser

## 🚀 Quick Start

### 1. Start the Backend
```bash
cd backend
dotnet run
# API runs on http://localhost:5169
```

### 2. Start the Frontend
```bash
cd frontend
npm run dev
# Web app runs on http://localhost:3000
```

### 3. Play the Game
1. Visit http://localhost:3000
2. Click "Start Playing"
3. Click "Start New Game"
4. Take turns playing as Player 1 and Player 2

## 🧪 Testing

### API Integration Test
```bash
# Run comprehensive API test
./test-api.sh
```

### Manual Testing Checklist
- [ ] Game creation and initial state
- [ ] Pre-draw betting (check, bet, call, raise, fold)
- [ ] Card discard and draw with deck exhaustion
- [ ] Post-draw betting
- [ ] Showdown and hand evaluation
- [ ] Hand progression and match completion
- [ ] Error handling and edge cases

## 🎯 Game Rules

### Setup
- 2 players, 100 chips each
- 10 hands per match
- 5 chip ante per player per hand

### Deck Composition
- 4 Aces, 4 Kings, 4 Queens, 4 Jacks
- 1 Joker (wild card)
- Total: 17 cards

### Hand Rankings (High to Low)
1. **Five-of-a-Kind** (requires Joker)
2. **Four-of-a-Kind**
3. **Full House**
4. **Three-of-a-Kind**
5. **Two Pair**
6. **One Pair**

*Note: No straights or flushes possible with this deck*

### Betting Structure
- **Fixed-limit**: Bets and raises in 5 chip increments
- **Cap**: Maximum 30 chips per betting round
- **Two streets**: Pre-draw and post-draw betting

### Game Flow
1. **Ante**: Each player pays 5 chips
2. **Deal**: 5 cards to each player
3. **Pre-draw betting**: Starting player acts first
4. **Draw**: Players may discard 0-5 cards and redraw
5. **Post-draw betting**: Starting player acts first
6. **Showdown**: Best hand wins the pot
7. **Repeat**: For 10 hands total

## 🔧 Development

### Project Structure
```
poker-17/
├── backend/           # ASP.NET Core API
│   ├── Controllers/   # REST endpoints
│   ├── DTOs/         # Data transfer objects
│   ├── Models/       # Game logic and state
│   └── Services/     # Business logic
├── frontend/         # Next.js React app
│   ├── src/app/      # App router pages
│   ├── src/components/ # UI components
│   ├── src/hooks/    # React hooks
│   └── src/lib/      # API client
└── RULES.md          # Complete game specification
```

### Key Components

#### Backend
- **`GameState`**: Core game engine and state machine
- **`HandEvaluator`**: Poker hand ranking with Joker logic
- **`BettingState`**: Fixed-limit betting enforcement
- **`GameService`**: In-memory session management

#### Frontend
- **`useGame`**: React hook for game state management
- **`GameTable`**: Main game interface
- **`PlayerPanel`**: Player info and hand display
- **`ActionBar`**: Betting and action controls

### Configuration

#### Backend Ports
- Development: `http://localhost:5169`
- HTTPS: `https://localhost:7196`
- Configure in `backend/Properties/launchSettings.json`

#### Frontend Environment
- Create `frontend/.env.local`:
```
NEXT_PUBLIC_API_BASE_URL="http://localhost:5169"
```

#### CORS
- Enabled for `http://localhost:3000` in `backend/Program.cs`
- Update for production deployment

## 🚢 Deployment

### Backend
```bash
cd backend
dotnet publish -c Release
# Deploy to your preferred hosting service
```

### Frontend
```bash
cd frontend
npm run build
npm start
# Or deploy to Vercel, Netlify, etc.
```

### Production Notes
- Update CORS origins for production domain
- Configure environment variables for API base URL
- Consider using a reverse proxy for both services

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test thoroughly
5. Submit a pull request

## 📄 License

This project is open source and available under the MIT License. 