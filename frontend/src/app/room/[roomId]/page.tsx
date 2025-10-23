"use client";

import { useState, useEffect, useCallback } from "react";
import { useRouter, useParams } from "next/navigation";
import { useGameHubContext } from "@/contexts/GameHubContext";
import { GameTable } from "@/components/GameTable";

export default function RoomPage() {
  const router = useRouter();
  const params = useParams();
  const roomId = params.roomId as string;

  const {
    connected,
    error,
    currentRoom,
    gameState,
    matchResult,
    playersReady,
    playersInRoom,
    leaveRoom,
    setReady,
    sendAction,
    cancelAutoAdvance,
    clearError,
  } = useGameHubContext();


  const [isReady, setIsReady] = useState(false);
  const [showAutoAdvanceCountdown, setShowAutoAdvanceCountdown] = useState(false);
  const [countdown, setCountdown] = useState(5);

  // Redirect to lobby if not in a room or room mismatch
  useEffect(() => {
    if (connected && currentRoom !== roomId && currentRoom !== null) {
      router.push(`/room/${currentRoom}`);
    } else if (connected && !currentRoom) {
      router.push("/lobby");
    }
  }, [connected, currentRoom, roomId, router]);

  // Handle room termination - redirect to lobby
  useEffect(() => {
    if (error?.includes("terminated") || error?.includes("inactive")) {
      const timer = setTimeout(() => {
        router.push("/lobby");
      }, 3000);
      return () => clearTimeout(timer);
    }
  }, [error, router]);

  // Auto-advance countdown
  useEffect(() => {
    if (gameState?.phase === "HandComplete" && !gameState.isMatchComplete) {
      setShowAutoAdvanceCountdown(true);
      setCountdown(5);

      const interval = setInterval(() => {
        setCountdown((prev) => {
          if (prev <= 1) {
            setShowAutoAdvanceCountdown(false);
            return 0;
          }
          return prev - 1;
        });
      }, 1000);

      return () => clearInterval(interval);
    } else {
      setShowAutoAdvanceCountdown(false);
    }
  }, [gameState?.phase, gameState?.isMatchComplete, gameState?.handNumber]);

  const handleLeaveRoom = async () => {
    await leaveRoom();
    router.push("/lobby");
  };

  const handleToggleReady = async () => {
    const newReady = !isReady;
    setIsReady(newReady);
    await setReady(newReady);
  };

  const handleCancelAutoAdvance = async () => {
    await cancelAutoAdvance();
    setShowAutoAdvanceCountdown(false);
  };

  // Game action handlers
  const handleCheck = useCallback(async (playerIndex: number) => {
    await sendAction({
      Type: "Check",
      PlayerIndex: playerIndex,
    });
  }, [sendAction]);

  const handleBet = useCallback(async (playerIndex: number, amount: number) => {
    await sendAction({
      Type: "Bet",
      PlayerIndex: playerIndex,
      Amount: amount,
    });
  }, [sendAction]);

  const handleCall = useCallback(async (playerIndex: number) => {
    await sendAction({
      Type: "Call",
      PlayerIndex: playerIndex,
    });
  }, [sendAction]);

  const handleRaise = useCallback(async (playerIndex: number, amount: number) => {
    await sendAction({
      Type: "Raise",
      PlayerIndex: playerIndex,
      Amount: amount,
    });
  }, [sendAction]);

  const handleFold = useCallback(async (playerIndex: number) => {
    await sendAction({
      Type: "Fold",
      PlayerIndex: playerIndex,
    });
  }, [sendAction]);

  const handleDiscard = useCallback(async (playerIndex: number, cardIndices: number[]) => {
    await sendAction({
      Type: "Discard",
      PlayerIndex: playerIndex,
      CardIndices: cardIndices,
    });
  }, [sendAction]);

  const handleNextHand = useCallback(async (playerIndex: number) => {
    setShowAutoAdvanceCountdown(false);
    await sendAction({
      Type: "NextHand",
      PlayerIndex: playerIndex,
    });
  }, [sendAction]);

  if (!connected) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
        <div className="text-center">
          <div className="text-6xl mb-4">🔄</div>
          <h2 className="text-2xl font-bold text-gray-800">Connecting...</h2>
        </div>
      </div>
    );
  }

  if (!gameState) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
        <div className="bg-white rounded-lg shadow-xl p-8 max-w-md w-full">
          <div className="text-center mb-6">
            <div className="text-6xl mb-4">⏳</div>
            <h2 className="text-2xl font-bold text-gray-800 mb-2">Waiting for Players</h2>
            <div className="text-lg font-mono text-blue-600 mb-4">{roomId}</div>
            <p className="text-gray-600">Share this room code with your opponent</p>
          </div>

          {error && (
            <div className="mb-4 p-3 bg-red-50 border border-red-200 rounded-lg">
              <p className="text-red-600 text-sm">{error}</p>
            </div>
          )}

          {/* Players Status */}
          {playersInRoom.length > 0 && (
            <div className="mb-6 space-y-2">
              <h3 className="text-sm font-semibold text-gray-700 mb-2">Players ({playersInRoom.length}/2):</h3>
              {playersInRoom.map((username) => (
                <div
                  key={username}
                  className="flex items-center justify-between p-3 bg-gray-50 rounded-lg border border-gray-200"
                >
                  <div className="flex items-center gap-2">
                    <div className="text-lg">👤</div>
                    <span className="font-medium text-gray-800">{username}</span>
                  </div>
                  <div>
                    {playersReady[username] ? (
                      <span className="flex items-center gap-1 text-green-600 font-semibold">
                        <span>✓</span>
                        <span>Ready</span>
                      </span>
                    ) : (
                      <span className="text-gray-400 text-sm">Waiting...</span>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}

          <div className="space-y-3">
            <button
              onClick={handleToggleReady}
              className={`w-full px-6 py-3 font-semibold rounded-lg transition-colors ${
                isReady
                  ? "bg-green-600 hover:bg-green-700 text-white"
                  : "bg-gray-300 hover:bg-gray-400 text-gray-700"
              }`}
            >
              {isReady ? "✓ Ready" : "Click when ready"}
            </button>

            <button
              onClick={handleLeaveRoom}
              className="w-full px-6 py-3 bg-red-600 hover:bg-red-700 text-white font-semibold rounded-lg transition-colors"
            >
              Leave Room
            </button>
          </div>

          <div className="mt-6 text-center text-sm text-gray-500">
            <p>Game will start when both players are ready</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 p-4">
      {/* Room Header */}
      <div className="max-w-6xl mx-auto mb-4">
        <div className="bg-white rounded-lg shadow-lg p-4">
          <div className="flex justify-between items-center">
            <div>
              <div className="text-sm text-gray-600">Room Code</div>
              <div className="text-2xl font-mono font-bold text-blue-600">{roomId}</div>
            </div>
            <button
              onClick={handleLeaveRoom}
              className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white font-semibold rounded-lg transition-colors"
            >
              Leave Room
            </button>
          </div>
        </div>
      </div>

      {/* Error Banner */}
      {error && (
        <div className="max-w-6xl mx-auto mb-4">
          <div className="p-4 bg-red-50 border border-red-200 rounded-lg">
            <div className="flex justify-between items-center">
              <p className="text-red-600">{error}</p>
              <button
                onClick={clearError}
                className="text-red-600 hover:text-red-800 font-semibold"
              >
                ✕
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Auto-advance countdown */}
      {showAutoAdvanceCountdown && (
        <div className="max-w-6xl mx-auto mb-4">
          <div className="p-4 bg-yellow-50 border border-yellow-200 rounded-lg">
            <div className="flex justify-between items-center">
              <p className="text-yellow-800">
                Next hand starting in <span className="font-bold text-2xl">{countdown}</span> seconds...
              </p>
              <button
                onClick={handleCancelAutoAdvance}
                className="px-4 py-2 bg-yellow-600 hover:bg-yellow-700 text-white font-semibold rounded-lg transition-colors"
              >
                Cancel Auto-Advance
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Match Result */}
      {matchResult && (
        <div className="max-w-6xl mx-auto mb-4">
          <div className="p-6 bg-gradient-to-r from-yellow-100 to-yellow-200 border-2 border-yellow-400 rounded-lg shadow-lg">
            <div className="text-center">
              <div className="text-6xl mb-4">🏆</div>
              <h2 className="text-3xl font-bold text-yellow-900 mb-2">Match Complete!</h2>
              <p className="text-xl text-yellow-800 mb-4">
                <span className="font-bold">{matchResult.winnerUsername}</span> wins with{" "}
                <span className="font-bold">{matchResult.winnerChips}</span> chips!
              </p>
              <div className="text-sm text-yellow-700">
                Total hands played: {matchResult.totalHands} • Final pot: {matchResult.finalPot}
              </div>
              <button
                onClick={handleLeaveRoom}
                className="mt-6 px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg transition-colors"
              >
                Return to Lobby
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Game Table */}
      <GameTable
        game={gameState}
        onCheck={handleCheck}
        onBet={handleBet}
        onCall={handleCall}
        onRaise={handleRaise}
        onFold={handleFold}
        onDiscard={handleDiscard}
        onNextHand={handleNextHand}
        loading={false}
      />
    </div>
  );
}

