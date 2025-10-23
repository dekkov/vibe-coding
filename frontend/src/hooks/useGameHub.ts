"use client";

import { useEffect, useRef, useState, useCallback } from "react";
import * as signalR from "@microsoft/signalr";
import { GameView } from "@/lib/api";

export interface RoomInfo {
  roomId: string;
  playerCount: number;
  maxPlayers: number;
  status: string;
  handNumber: number;
  pot: number;
  createdAt: string;
  playerNames: string[];
}

export interface MatchResult {
  winnerIndex: number;
  winnerUsername: string;
  winnerChips: number;
  finalPot: number;
  totalHands: number;
  completedAt: string;
}

export interface PlayerReadyStatus {
  username: string;
  isReady: boolean;
}

export function useGameHub() {
  // Keep reference, no need to track connection in React state
  // (state change not needed and triggers unnecessary rerenders)
  const [/* connection */, setConnection] = useState<signalR.HubConnection | null>(null);
  const [connected, setConnected] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [currentRoom, setCurrentRoom] = useState<string | null>(null);
  const [gameState, setGameState] = useState<GameView | null>(null);
  const [rooms, setRooms] = useState<RoomInfo[]>([]);
  const [matchResult, setMatchResult] = useState<MatchResult | null>(null);
  const [playersReady, setPlayersReady] = useState<Record<string, boolean>>({});
  const [playersInRoom, setPlayersInRoom] = useState<string[]>([]);
  
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const isConnectingRef = useRef<boolean>(false);

  // Initialize connection
  useEffect(() => {
    // Skip if already connecting or connected
    if (isConnectingRef.current || connectionRef.current?.state === signalR.HubConnectionState.Connected) {
      return;
    }
    
    isConnectingRef.current = true;

    const hubUrl = process.env.NEXT_PUBLIC_HUB_URL ?? "http://localhost:5169/gamehub";
    console.log("🔌 Connecting to hub:", hubUrl);
    
    // Builder that allows toggling negotiation/transport
    const buildConnection = (
      transport: signalR.HttpTransportType,
      skipNegotiation: boolean
    ) =>
      new signalR.HubConnectionBuilder()
        .withUrl(hubUrl, {
          skipNegotiation,
          transport,
          withCredentials: true,
        })
        .withAutomaticReconnect()
        .configureLogging(signalR.LogLevel.Information)
        .build();

    // Start with WebSockets only and skip negotiation to avoid negotiate failures
    const newConnection = buildConnection(signalR.HttpTransportType.WebSockets, true);

    connectionRef.current = newConnection;
    setConnection(newConnection);

    // Setup event handlers
    newConnection.on("RoomCreated", (roomId: string) => {
      console.log("Room created:", roomId);
      setCurrentRoom(roomId);
      setError(null);
    });

    newConnection.on("RoomJoined", (roomId: string) => {
      console.log("Joined room:", roomId);
      setCurrentRoom(roomId);
      setError(null);
    });

    newConnection.on("RoomLeft", () => {
      console.log("Left room");
      setCurrentRoom(null);
      setGameState(null);
      setMatchResult(null);
      setPlayersReady({});
      setPlayersInRoom([]);
    });

    newConnection.on("PlayerJoined", (username: string, playerIndex: number) => {
      console.log(`Player ${username} joined as player ${playerIndex}`);
      setPlayersInRoom((prev) => {
        if (!prev.includes(username)) {
          return [...prev, username];
        }
        return prev;
      });
    });

    newConnection.on("PlayerLeft", (username: string) => {
      console.log(`Player ${username} left`);
      setPlayersInRoom((prev) => prev.filter((u) => u !== username));
      setPlayersReady((prev) => {
        const { [username]: _, ...rest } = prev;
        return rest;
      });
    });

    newConnection.on("PlayerReadyChanged", (username: string, isReady: boolean) => {
      console.log(`Player ${username} ready: ${isReady}`);
      setPlayersReady((prev) => ({ ...prev, [username]: isReady }));
    });

    newConnection.on("GameStarted", () => {
      console.log("Game started!");
      setMatchResult(null);
      setPlayersReady({}); // Clear ready status when game starts
    });

    newConnection.on("GameStateUpdated", (game: GameView) => {
      console.log("Game state updated:", game.phase);
      setGameState(game);
    });

    newConnection.on("MatchComplete", (result: MatchResult) => {
      console.log("Match complete:", result);
      setMatchResult(result);
    });

    newConnection.on("RoomsUpdated", (roomList: RoomInfo[]) => {
      console.log("Rooms updated:", roomList.length);
      setRooms(roomList);
    });

    newConnection.on("RoomTerminated", (message: string) => {
      console.log("Room terminated:", message);
      setError(message);
      setCurrentRoom(null);
      setGameState(null);
      setMatchResult(null);
    });

    newConnection.on("AutoAdvanceCancelled", () => {
      console.log("Auto-advance cancelled");
    });

    newConnection.on("Error", (errorMessage: string) => {
      console.error("Hub error:", errorMessage);
      setError(errorMessage);
    });

    // Handle connection errors
    newConnection.onclose((error) => {
      console.log("SignalR connection closed", error);
      setConnected(false);
      if (error) {
        setError(`Connection lost: ${error.message}`);
      }
    });

    newConnection.onreconnecting((error) => {
      console.log("SignalR reconnecting...", error);
      setError("Reconnecting to server...");
    });

    newConnection.onreconnected(() => {
      console.log("SignalR reconnected");
      setError(null);
      setConnected(true);
    });

    // Start connection with fallback to negotiated LongPolling
    const startWithFallback = async () => {
      try {
        await newConnection.start();
        console.log("✅ SignalR connected successfully (WebSockets, skipNegotiation)");
        setConnected(true);
        setError(null);
        isConnectingRef.current = false;
      } catch (err: unknown) {
        console.error("❌ SignalR connection error (primary):", err);
        console.log("🔁 Retrying with negotiated LongPolling...");

        try {
          // Build fresh connection with LongPolling (do not stop the previous one while it is starting)
          const fallbackConnection = buildConnection(
            signalR.HttpTransportType.LongPolling,
            false
          );
          
          // Re-register all event handlers for fallback connection
          fallbackConnection.on("RoomCreated", (roomId: string) => {
            console.log("Room created:", roomId);
            setCurrentRoom(roomId);
            setError(null);
          });

          fallbackConnection.on("RoomJoined", (roomId: string) => {
            console.log("Joined room:", roomId);
            setCurrentRoom(roomId);
            setError(null);
          });

          fallbackConnection.on("RoomLeft", () => {
            console.log("Left room");
            setCurrentRoom(null);
            setGameState(null);
            setMatchResult(null);
            setPlayersReady({});
            setPlayersInRoom([]);
          });

          fallbackConnection.on("PlayerJoined", (username: string, playerIndex: number) => {
            console.log(`Player ${username} joined as player ${playerIndex}`);
            setPlayersInRoom((prev) => {
              if (!prev.includes(username)) {
                return [...prev, username];
              }
              return prev;
            });
          });

          fallbackConnection.on("PlayerLeft", (username: string) => {
            console.log(`Player ${username} left`);
            setPlayersInRoom((prev) => prev.filter((u) => u !== username));
            setPlayersReady((prev) => {
              const { [username]: _, ...rest } = prev;
              return rest;
            });
          });

          fallbackConnection.on("PlayerReadyChanged", (username: string, isReady: boolean) => {
            console.log(`Player ${username} ready: ${isReady}`);
            setPlayersReady((prev) => ({ ...prev, [username]: isReady }));
          });

          fallbackConnection.on("GameStarted", () => {
            console.log("Game started!");
            setMatchResult(null);
            setPlayersReady({}); // Clear ready status when game starts
          });

          fallbackConnection.on("GameStateUpdated", (game: GameView) => {
            console.log("Game state updated:", game.phase);
            setGameState(game);
          });

          fallbackConnection.on("MatchComplete", (result: MatchResult) => {
            console.log("Match complete:", result);
            setMatchResult(result);
          });

          fallbackConnection.on("RoomsUpdated", (roomList: RoomInfo[]) => {
            console.log("Rooms updated:", roomList.length);
            setRooms(roomList);
          });

          fallbackConnection.on("RoomTerminated", (message: string) => {
            console.log("Room terminated:", message);
            setError(message);
            setCurrentRoom(null);
            setGameState(null);
            setMatchResult(null);
          });

          fallbackConnection.on("AutoAdvanceCancelled", () => {
            console.log("Auto-advance cancelled");
          });

          fallbackConnection.on("Error", (errorMessage: string) => {
            console.error("Hub error:", errorMessage);
            setError(errorMessage);
          });

          fallbackConnection.onclose((error) => {
            console.log("SignalR connection closed", error);
            setConnected(false);
            isConnectingRef.current = false;
            if (error) {
              setError(`Connection lost: ${error.message}`);
            }
          });

          fallbackConnection.onreconnecting((error) => {
            console.log("SignalR reconnecting...", error);
            setError("Reconnecting to server...");
          });

          fallbackConnection.onreconnected(() => {
            console.log("SignalR reconnected");
            setError(null);
            setConnected(true);
          });

          connectionRef.current = fallbackConnection;
          setConnection(fallbackConnection);

          await fallbackConnection.start();
          console.log("✅ SignalR connected successfully (LongPolling)");
          setConnected(true);
          setError(null);
          isConnectingRef.current = false;
        } catch (err2: unknown) {
          console.error("❌ SignalR connection error (fallback):", err2);
          setError(
            `Failed to connect to game server: ${
              (err2 as Error)?.message || "unknown"
            }`
          );
          setConnected(false);
          isConnectingRef.current = false;
        }
      }
    };

    startWithFallback();

    // Cleanup on unmount - only if connection is established or connecting
    return () => {
      const conn = connectionRef.current;
      if (conn) {
        const state = conn.state;
        // Only attempt to stop if we're in a stable state
        if (state === signalR.HubConnectionState.Connected || 
            state === signalR.HubConnectionState.Reconnecting) {
          console.log("🔌 Stopping SignalR connection...");
          conn.stop().catch((err) => {
            console.error("Error stopping connection:", err);
          });
        }
      }
      // Don't reset isConnectingRef here - let it persist across remounts
    };
  }, []);

  // Hub methods
  const createRoom = useCallback(async (username: string) => {
    if (!connectionRef.current) return;
    try {
      await connectionRef.current.invoke("CreateRoom", username);
    } catch (err) {
      console.error("Error creating room:", err);
      setError("Failed to create room");
    }
  }, []);

  const joinRoom = useCallback(async (roomId: string, username: string) => {
    if (!connectionRef.current) return;
    try {
      await connectionRef.current.invoke("JoinRoom", roomId, username);
    } catch (err) {
      console.error("Error joining room:", err);
      setError("Failed to join room");
    }
  }, []);

  const leaveRoom = useCallback(async () => {
    if (!connectionRef.current || !currentRoom) return;
    try {
      await connectionRef.current.invoke("LeaveRoom", currentRoom);
      setCurrentRoom(null);
      setGameState(null);
      setMatchResult(null);
    } catch (err) {
      console.error("Error leaving room:", err);
    }
  }, [currentRoom]);

  const setReady = useCallback(async (isReady: boolean) => {
    if (!connectionRef.current || !currentRoom) return;
    try {
      await connectionRef.current.invoke("PlayerReady", currentRoom, isReady);
    } catch (err) {
      console.error("Error setting ready:", err);
      setError("Failed to set ready status");
    }
  }, [currentRoom]);

  type HubAction = {
    Type: "Check" | "Bet" | "Call" | "Raise" | "Fold" | "Discard" | "NextHand";
    PlayerIndex: number;
    Amount?: number;
    CardIndices?: number[];
  };

  const sendAction = useCallback(async (action: HubAction) => {
    if (!connectionRef.current || !currentRoom) return;
    try {
      await connectionRef.current.invoke("PlayerAction", currentRoom, action);
    } catch (err) {
      console.error("Error sending action:", err);
      setError("Failed to send action");
    }
  }, [currentRoom]);

  const getActiveRooms = useCallback(async () => {
    if (!connectionRef.current) return;
    try {
      await connectionRef.current.invoke("GetActiveRooms");
    } catch (err) {
      console.error("Error getting rooms:", err);
    }
  }, []);

  const cancelAutoAdvance = useCallback(async () => {
    if (!connectionRef.current || !currentRoom) return;
    try {
      await connectionRef.current.invoke("CancelAutoAdvance", currentRoom);
    } catch (err) {
      console.error("Error cancelling auto-advance:", err);
    }
  }, [currentRoom]);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  return {
    connected,
    error,
    currentRoom,
    gameState,
    rooms,
    matchResult,
    playersReady,
    playersInRoom,
    createRoom,
    joinRoom,
    leaveRoom,
    setReady,
    sendAction,
    getActiveRooms,
    cancelAutoAdvance,
    clearError,
  };
}

export type GameHubContextType = ReturnType<typeof useGameHub>;

