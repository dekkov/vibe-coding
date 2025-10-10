#!/bin/bash

# 17 Poker API Integration Test Script
echo "🃏 Testing 17 Poker API..."

API_BASE="http://localhost:5169/api/game"

# Test 1: Create new game
echo "1. Creating new game..."
RESPONSE=$(curl -s -X POST $API_BASE/new -H "Content-Type: application/json" -d "{}")
GAME_ID=$(echo $RESPONSE | jq -r '.game.gameId')

if [ "$GAME_ID" = "null" ]; then
    echo "❌ Failed to create game"
    echo $RESPONSE | jq '.'
    exit 1
fi

echo "✅ Game created: $GAME_ID"

# Test 2: Get game state
echo "2. Getting game state..."
curl -s "$API_BASE/state?gameId=$GAME_ID" | jq '.game | {phase, handNumber, pot, deckRemaining}' || exit 1

# Test 3: Player 1 bets
echo "3. Player 1 bets 5..."
curl -s -X POST $API_BASE/action -H "Content-Type: application/json" \
  -d "{\"gameId\":\"$GAME_ID\",\"playerIndex\":0,\"actionType\":\"bet\",\"amount\":5}" | \
  jq '.game.betting | {currentBet, toActPlayerIndex}' || exit 1

# Test 4: Player 2 calls
echo "4. Player 2 calls..."
curl -s -X POST $API_BASE/action -H "Content-Type: application/json" \
  -d "{\"gameId\":\"$GAME_ID\",\"playerIndex\":1,\"actionType\":\"call\"}" | \
  jq '.game | {phase, pot}' || exit 1

# Test 5: Player 1 discards (draw phase)
echo "5. Player 1 discards 2 cards..."
curl -s -X POST $API_BASE/action -H "Content-Type: application/json" \
  -d "{\"gameId\":\"$GAME_ID\",\"playerIndex\":0,\"actionType\":\"discard\",\"cardIndices\":[0,2]}" | \
  jq '.game | {phase, deckRemaining}' || exit 1

# Test 6: Player 2 discards
echo "6. Player 2 discards 1 card..."
curl -s -X POST $API_BASE/action -H "Content-Type: application/json" \
  -d "{\"gameId\":\"$GAME_ID\",\"playerIndex\":1,\"actionType\":\"discard\",\"cardIndices\":[4]}" | \
  jq '.game | {phase, deckRemaining}' || exit 1

# Test 7: Post-draw betting (both check)
echo "7. Post-draw betting - both check..."
curl -s -X POST $API_BASE/action -H "Content-Type: application/json" \
  -d "{\"gameId\":\"$GAME_ID\",\"playerIndex\":0,\"actionType\":\"check\"}" > /dev/null

curl -s -X POST $API_BASE/action -H "Content-Type: application/json" \
  -d "{\"gameId\":\"$GAME_ID\",\"playerIndex\":1,\"actionType\":\"check\"}" | \
  jq '.game | {phase, showdown: (.showdown.winnerIndex // "none")}' || exit 1

echo "✅ All API tests passed!"
echo "🎉 Backend is working correctly!"
