"use client";

import { useEffect } from "react";
import { useGame } from "@/hooks/useGame";
import { GameTable } from "@/components/GameTable";

export default function PlayPage() {
  const {
    game,
    loading,
    error,
    startNewGame,
    clearError,
    check,
    bet,
    call,
    raise,
    fold,
    discard,
    nextHand
  } = useGame();

  // Auto-dismiss error after 5 seconds
  useEffect(() => {
    if (error) {
      const timer = setTimeout(() => {
        clearError();
      }, 5000);
      return () => clearTimeout(timer);
    }
  }, [error, clearError]);

  // Don't show full error screen for validation errors when game exists
  if (error && !game) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-100">
        <div className="text-center p-8 bg-white rounded-lg shadow-lg max-w-md">
          <div className="text-red-600 text-6xl mb-4">⚠️</div>
          <h2 className="text-xl font-bold text-gray-800 mb-2">Error</h2>
          <p className="text-gray-600 mb-4">{error}</p>
          <button
            onClick={startNewGame}
            className="px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition-colors"
            disabled={loading}
          >
            {loading ? "Starting..." : "Start New Game"}
          </button>
        </div>
      </div>
    );
  }

  if (!game) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-100">
        <div className="text-center p-8 bg-white rounded-lg shadow-lg max-w-md">
          <div className="text-6xl mb-4">🃏</div>
          <h1 className="text-2xl font-bold text-gray-800 mb-4">17 Poker</h1>
          <p className="text-gray-600 mb-6">
            A heads-up poker variant played with 17 cards: 4 Aces, 4 Kings, 4 Queens, 4 Jacks, and 1 Joker.
          </p>
          <button
            onClick={startNewGame}
            className="px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white rounded-lg font-medium transition-colors text-lg"
            disabled={loading}
          >
            {loading ? (
              <div className="flex items-center">
                <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-white mr-2"></div>
                Starting Game...
              </div>
            ) : (
              "Start New Game"
            )}
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-100 py-4">
      {/* Error Notification */}
      {error && (
        <div className="max-w-6xl mx-auto mb-4 p-4 bg-red-100 border border-red-400 text-red-700 rounded-lg">
          <div className="flex items-center justify-between">
            <div className="flex items-center">
              <span className="text-red-500 mr-2">⚠️</span>
              <span>{error}</span>
            </div>
            <button
              onClick={clearError}
              className="text-red-500 hover:text-red-700 font-bold text-lg"
              title="Dismiss"
            >
              ×
            </button>
          </div>
        </div>
      )}
      
      <GameTable
        game={game}
        onCheck={check}
        onBet={bet}
        onCall={call}
        onRaise={raise}
        onFold={fold}
        onDiscard={discard}
        onNextHand={nextHand}
        loading={loading}
      />
      
      {/* New Game Button */}
      <div className="text-center mt-6">
        <button
          onClick={startNewGame}
          className="px-4 py-2 bg-gray-600 hover:bg-gray-700 text-white rounded-lg font-medium transition-colors"
          disabled={loading}
        >
          Start New Match
        </button>
      </div>
    </div>
  );
}
