"use client";

import { useState, useEffect } from "react";
import { GameView } from "@/lib/api";
import { PlayerPanel } from "./PlayerPanel";
import { ActionBar } from "./ActionBar";

interface GameTableProps {
  game: GameView;
  onCheck: (playerIndex: number) => void;
  onBet: (playerIndex: number, amount: number) => void;
  onCall: (playerIndex: number) => void;
  onRaise: (playerIndex: number, amount: number) => void;
  onFold: (playerIndex: number) => void;
  onDiscard: (playerIndex: number, cardIndices: number[]) => void;
  onNextHand: (playerIndex: number) => void;
  loading?: boolean;
}

export function GameTable({
  game,
  onCheck,
  onBet,
  onCall,
  onRaise,
  onFold,
  onDiscard,
  onNextHand,
  loading = false
}: GameTableProps) {
  const [selectedCards, setSelectedCards] = useState<number[]>([]);
  const [selectingPlayerIndex, setSelectingPlayerIndex] = useState<number>(-1);
  const [eventLog, setEventLog] = useState<string[]>([]);

  const currentPlayerIndex = game.betting?.toActPlayerIndex ?? -1;
  
  // In draw phase, determine whose turn it is based on game logic
  const getDrawPhaseActivePlayer = () => {
    if (game.phase !== "Draw") return -1;
    
    // Use the explicit field from backend if available
    if (game.drawPhaseActivePlayer !== undefined && game.drawPhaseActivePlayer !== null) {
      return game.drawPhaseActivePlayer;
    }
    
    // Fallback to starting player (shouldn't happen with new backend)
    return game.startingPlayerIndex;
  };
  
  const activePlayerIndex = game.phase === "Draw" ? getDrawPhaseActivePlayer() : currentPlayerIndex;

  // Handle card selection for discard
  const handleCardSelect = (playerIndex: number, cardIndex: number) => {
    if (!game.actionCapabilities.canDiscard) {
      return;
    }

    // Only allow the active player to select their own cards
    if (playerIndex !== activePlayerIndex) {
      return;
    }

    // If selecting from a different player, clear previous selection
    if (selectingPlayerIndex !== playerIndex) {
      setSelectingPlayerIndex(playerIndex);
      setSelectedCards([cardIndex]);
    } else {
      setSelectedCards(prev => {
        if (prev.includes(cardIndex)) {
          // Deselecting a card - always allow
          return prev.filter(i => i !== cardIndex);
        } else {
          // Selecting a new card - check if we can select more
          if (prev.length >= game.deckRemaining) {
            // Already selected maximum cards available in deck
            return prev;
          }
          return [...prev, cardIndex];
        }
      });
    }
  };

  // Clear selection when phase changes
  useEffect(() => {
    setSelectedCards([]);
    setSelectingPlayerIndex(-1);
  }, [game.phase, game.handNumber]);

  // Track events for the log
  useEffect(() => {
    if (game.lastEvent && game.lastEvent.trim() !== "") {
      setEventLog(prev => {
        // Avoid duplicates and limit to last 10 events
        if (prev[prev.length - 1] !== game.lastEvent) {
          const newLog = [...prev, game.lastEvent].slice(-10);
          return newLog;
        }
        return prev;
      });
    }
  }, [game.lastEvent]);

  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  const handleAction = (actionFn: (playerIndex: number, ...args: any[]) => void, ...args: any[]) => {
    // Use activePlayerIndex which handles both betting and draw phases correctly
    actionFn(activePlayerIndex, ...args);
    setSelectedCards([]); // Clear selection after action
    setSelectingPlayerIndex(-1); // Clear selecting player
  };

  return (
    <div className="max-w-6xl mx-auto p-4 space-y-6">
      {/* Game Header */}
      <div className="text-center">
        <h1 className="text-3xl font-bold text-gray-800 mb-2">17 Poker</h1>
        <div className="flex justify-center items-center gap-6 text-sm text-gray-600">
          <span>Hand {game.handNumber}/10</span>
          <span>Phase: {game.phase}</span>
          <span>Pot: 💰 {game.pot}</span>
          <span>Deck: {game.deckRemaining} cards</span>
          {game.phase === "Draw" && (
            <span className="text-orange-600 font-semibold">
              Max Discard: {game.deckRemaining}
            </span>
          )}
        </div>
      </div>

      {/* Card Selection Info */}
      {game.phase === "Draw" && selectedCards.length > 0 && (
        <div className="text-center p-3 bg-orange-50 rounded-lg">
          <p className="text-orange-800">
            Selected {selectedCards.length} of {game.deckRemaining} max cards to discard
            {selectedCards.length >= game.deckRemaining && (
              <span className="ml-2 text-red-600 font-semibold">(Maximum reached)</span>
            )}
          </p>
        </div>
      )}

      {/* Last Event */}
      {game.lastEvent && (
        <div className="text-center p-3 bg-blue-50 rounded-lg">
          <p className="text-blue-800">{game.lastEvent}</p>
        </div>
      )}

      {/* Players - Hide when match is complete */}
      {!game.isMatchComplete && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {game.players.map((player) => (
            <PlayerPanel
              key={player.index}
              player={player}
              isActive={activePlayerIndex === player.index}
              isStartingPlayer={game.startingPlayerIndex === player.index}
              selectedCards={selectingPlayerIndex === player.index ? selectedCards : []}
              onCardSelect={
                game.actionCapabilities.canDiscard && player.index === activePlayerIndex
                  ? (cardIndex) => handleCardSelect(player.index, cardIndex)
                  : undefined
              }
            />
          ))}
        </div>
      )}

      {/* Showdown Results - Show only when game ends (not match complete) */}
      {game.showdown && game.phase === "HandComplete" && !game.isMatchComplete && (
        <div className="p-4 bg-green-50 rounded-lg border-2 border-green-400">
          <h3 className="text-xl font-bold text-green-800 mb-4">🏆 Hand Results</h3>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {game.showdown.hands.map((hand, index) => (
              <div
                key={index}
                className={`p-3 rounded-lg border-2 ${
                  index === game.showdown!.winnerIndex
                    ? "border-green-500 bg-green-100"
                    : "border-gray-300 bg-white"
                }`}
              >
                <div className="font-semibold text-black">
                  Player {index + 1}
                  {index === game.showdown!.winnerIndex && (
                    <span className="ml-2 text-green-600">🏆 WINNER</span>
                  )}
                </div>
                <div className="text-lg font-bold text-gray-800">{hand.description}</div>
                <div className="text-sm text-gray-600">
                  {hand.handType}
                  {hand.kickers.length > 0 && (
                    <span> • Kickers: {hand.kickers.join(", ")}</span>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Match Complete */}
      {game.isMatchComplete && game.matchWinnerIndex !== null && (
        <div className="text-center p-6 bg-yellow-50 rounded-lg border-2 border-yellow-400">
          <h2 className="text-2xl font-bold text-yellow-800 mb-2">
            🎉 Match Complete! 🎉
          </h2>
          <p className="text-lg text-yellow-700">
            Player {game.matchWinnerIndex + 1} wins with {game.players[game.matchWinnerIndex].chips} chips!
          </p>
        </div>
      )}

      {/* Action Bar */}
      {(activePlayerIndex >= 0 || game.actionCapabilities.canNextHand) && !game.isMatchComplete && (
        <ActionBar
          capabilities={game.actionCapabilities}
          betting={game.betting}
          selectedCards={selectedCards}
          currentPlayer={game.players.find(p => p.index === activePlayerIndex)}
          onCheck={() => handleAction(onCheck)}
          onBet={(amount) => handleAction(onBet, amount)}
          onCall={() => handleAction(onCall)}
          onRaise={(amount) => handleAction(onRaise, amount)}
          onFold={() => handleAction(onFold)}
          onDiscard={() => handleAction(onDiscard, selectedCards)}
          onNextHand={() => handleAction(onNextHand)}
          loading={loading}
        />
      )}

      {/* Result Log */}
      {eventLog.length > 0 && (
        <div className="max-w-6xl mx-auto mt-4 p-4 bg-gray-50 rounded-lg">
          <h3 className="text-lg font-semibold text-gray-800 mb-3">Game Log</h3>
          <div className="space-y-1 max-h-40 overflow-y-auto">
            {eventLog.map((event, index) => (
              <div
                key={index}
                className="text-sm text-gray-700 py-1 px-2 bg-white rounded border-l-4 border-blue-400"
              >
                <span className="text-xs text-gray-500 mr-2">
                  {String(index + 1).padStart(2, '0')}:
                </span>
                {event}
              </div>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
