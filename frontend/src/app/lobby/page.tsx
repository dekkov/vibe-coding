"use client";

import { useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { useGameHubContext } from "@/contexts/GameHubContext";

export default function LobbyPage() {
  const router = useRouter();
  const {
    connected,
    error,
    currentRoom,
    rooms,
    createRoom,
    joinRoom,
    getActiveRooms,
    clearError,
  } = useGameHubContext();

  const [username, setUsername] = useState("");
  const [joinCode, setJoinCode] = useState("");
  const [usernameSet, setUsernameSet] = useState(false);
  const [isNavigating, setIsNavigating] = useState(false);

  // Load username from localStorage on mount
  useEffect(() => {
    const savedUsername = localStorage.getItem("poker17_username");
    if (savedUsername) {
      setUsername(savedUsername);
      setUsernameSet(true);
    }
  }, []);

  // Redirect to room when joined
  useEffect(() => {
    if (currentRoom && !isNavigating) {
      console.log("Navigating to room:", currentRoom);
      setIsNavigating(true);
      router.push(`/room/${currentRoom}`);
    }
  }, [currentRoom, router, isNavigating]);

  // Fetch active rooms periodically
  useEffect(() => {
    if (connected && usernameSet) {
      getActiveRooms();
      const interval = setInterval(() => {
        getActiveRooms();
      }, 5000); // Refresh every 5 seconds
      return () => clearInterval(interval);
    }
  }, [connected, usernameSet, getActiveRooms]);

  const handleSetUsername = () => {
    if (username.trim().length < 2) {
      return;
    }
    // Save username to localStorage
    localStorage.setItem("poker17_username", username.trim());
    setUsernameSet(true);
  };

  const handleCreateRoom = async () => {
    if (!username.trim()) return;
    await createRoom(username);
  };

  const handleJoinRoom = async () => {
    if (!username.trim() || !joinCode.trim()) return;
    await joinRoom(joinCode.toUpperCase(), username);
  };

  const handleJoinFromList = async (roomId: string) => {
    if (!username.trim()) return;
    await joinRoom(roomId, username);
  };

  if (!connected) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
        <div className="text-center">
          <div className="text-6xl mb-4">🔄</div>
          <h2 className="text-2xl font-bold text-gray-800">Connecting to server...</h2>
          {error && (
            <p className="text-red-600 mt-4">{error}</p>
          )}
        </div>
      </div>
    );
  }

  if (!usernameSet) {
    const canContinue = username.trim().length >= 2;
    
    return (
      <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
        <div className="bg-white rounded-lg shadow-xl p-8 max-w-md w-full">
          <div className="text-center mb-6">
            <div className="text-6xl mb-4">🃏</div>
            <h1 className="text-3xl font-bold text-gray-800 mb-2">17 Poker</h1>
            <p className="text-gray-600">Enter your username to continue</p>
          </div>

          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                Username
              </label>
              <input
                type="text"
                value={username}
                onChange={(e) => setUsername(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === "Enter" && canContinue) {
                    handleSetUsername();
                  }
                }}
                placeholder="Enter your username"
                maxLength={20}
                className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                autoFocus
              />
              <p className="text-xs text-gray-500 mt-1">
                Username must be at least 2 characters (current: {username.trim().length})
              </p>
            </div>

            {error && (
              <div className="p-3 bg-red-50 border border-red-200 rounded-lg">
                <div className="flex justify-between items-center">
                  <p className="text-red-600 text-sm">{error}</p>
                  <button
                    onClick={clearError}
                    className="text-red-600 hover:text-red-800 font-semibold ml-2"
                  >
                    ✕
                  </button>
                </div>
              </div>
            )}

            <button
              onClick={handleSetUsername}
              disabled={!canContinue}
              className="w-full px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg transition-colors disabled:bg-gray-400 disabled:cursor-not-allowed"
            >
              Continue
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 p-4">
      <div className="max-w-6xl mx-auto py-8">
        {/* Header */}
        <div className="text-center mb-8">
          <div className="text-6xl mb-4">🃏</div>
          <h1 className="text-4xl font-bold text-gray-800 mb-2">17 Poker Lobby</h1>
          <p className="text-gray-600">Welcome, <span className="font-semibold">{username}</span>!</p>
        </div>

        {/* Error message */}
        {error && (
          <div className="mb-6 p-4 bg-red-50 border border-red-200 rounded-lg">
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
        )}

        <div className="grid grid-cols-1 lg:grid-cols-2 gap-8">
          {/* Create/Join Room */}
          <div className="space-y-6">
            {/* Create Room */}
            <div className="bg-white rounded-lg shadow-lg p-6">
              <h2 className="text-2xl font-bold text-gray-800 mb-4">Create Room</h2>
              <p className="text-gray-600 mb-4">Start a new game and invite a friend</p>
              <button
                onClick={handleCreateRoom}
                className="w-full px-6 py-3 bg-green-600 hover:bg-green-700 text-white font-semibold rounded-lg transition-colors"
              >
                🎮 Create New Room
              </button>
            </div>

            {/* Join Room */}
            <div className="bg-white rounded-lg shadow-lg p-6">
              <h2 className="text-2xl font-bold text-gray-800 mb-4">Join Room</h2>
              <p className="text-gray-600 mb-4">Enter a room code to join an existing game</p>
              <div className="space-y-3">
                <input
                  type="text"
                  value={joinCode}
                  onChange={(e) => setJoinCode(e.target.value.toUpperCase())}
                  onKeyDown={(e) => e.key === "Enter" && handleJoinRoom()}
                  placeholder="Enter room code"
                  maxLength={6}
                  className="w-full px-4 py-2 border border-gray-300 rounded-lg focus:ring-2 focus:ring-blue-500 focus:border-transparent uppercase text-center text-2xl font-mono"
                />
                <button
                  onClick={handleJoinRoom}
                  disabled={joinCode.trim().length !== 6}
                  className="w-full px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white font-semibold rounded-lg transition-colors disabled:bg-gray-400 disabled:cursor-not-allowed"
                >
                  Join Room
                </button>
              </div>
            </div>
          </div>

          {/* Active Rooms */}
          <div className="bg-white rounded-lg shadow-lg p-6">
            <h2 className="text-2xl font-bold text-gray-800 mb-4">Active Rooms</h2>
            {rooms.length === 0 ? (
              <div className="text-center py-8 text-gray-500">
                <div className="text-4xl mb-2">🎴</div>
                <p>No active rooms</p>
                <p className="text-sm">Create a room to get started!</p>
              </div>
            ) : (
              <div className="space-y-3 max-h-96 overflow-y-auto">
                {rooms.map((room) => (
                  <div
                    key={room.roomId}
                    className="border border-gray-200 rounded-lg p-4 hover:border-blue-400 transition-colors"
                  >
                    <div className="flex justify-between items-start mb-2">
                      <div>
                        <div className="font-mono text-xl font-bold text-blue-600">
                          {room.roomId}
                        </div>
                        <div className="text-sm text-gray-600">
                          {room.playerNames.join(", ") || "Waiting for players..."}
                        </div>
                      </div>
                      <div className="text-right">
                        <div className="text-sm font-semibold text-gray-700">
                          {room.playerCount}/{room.maxPlayers} players
                        </div>
                        <div className={`text-xs px-2 py-1 rounded-full ${
                          room.status === "Waiting" ? "bg-yellow-100 text-yellow-800" :
                          room.status === "InProgress" ? "bg-green-100 text-green-800" :
                          "bg-gray-100 text-gray-800"
                        }`}>
                          {room.status}
                        </div>
                      </div>
                    </div>
                    
                    {room.status === "InProgress" && (
                      <div className="text-xs text-gray-500 mb-2">
                        Hand {room.handNumber}/10 • Pot: {room.pot} chips
                      </div>
                    )}

                    <button
                      onClick={() => handleJoinFromList(room.roomId)}
                      disabled={room.playerCount >= room.maxPlayers || room.status !== "Waiting"}
                      className="w-full px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white text-sm font-semibold rounded-lg transition-colors disabled:bg-gray-300 disabled:cursor-not-allowed"
                    >
                      {room.playerCount >= room.maxPlayers ? "Full" : 
                       room.status !== "Waiting" ? "In Progress" : "Join"}
                    </button>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>

        {/* Back to home */}
        <div className="text-center mt-8">
          <Link
            href="/"
            className="text-blue-600 hover:text-blue-800 font-semibold"
          >
            ← Back to Home
          </Link>
        </div>
      </div>
    </div>
  );
}

