"use client";

import { useState, useCallback } from "react";
import { GameView, GameResponse, ActionRequest, createNewGame, getGameState, processAction } from "@/lib/api";

export function useGame() {
  const [game, setGame] = useState<GameView | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleResponse = useCallback((response: GameResponse) => {
    if (response.success && response.game) {
      setGame(response.game);
      setError(null);
    } else {
      // For validation errors, show the error but keep the current game state
      setError(response.error || "Unknown error occurred");
      // Don't clear the game state for validation errors
    }
  }, []);

  const startNewGame = useCallback(async () => {
    setLoading(true);
    setError(null);
    
    try {
      const response = await createNewGame();
      handleResponse(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to create game");
    } finally {
      setLoading(false);
    }
  }, [handleResponse]);

  const refreshGame = useCallback(async (gameId: string) => {
    if (!gameId) return;
    
    setLoading(true);
    setError(null);
    
    try {
      const response = await getGameState(gameId);
      handleResponse(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to refresh game");
    } finally {
      setLoading(false);
    }
  }, [handleResponse]);

  const submitAction = useCallback(async (request: ActionRequest) => {
    setLoading(true);
    setError(null);
    
    try {
      const response = await processAction(request);
      handleResponse(response);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to process action");
    } finally {
      setLoading(false);
    }
  }, [handleResponse]);

  // Helper functions for common actions
  const check = useCallback((playerIndex: number) => {
    if (!game) return;
    submitAction({
      gameId: game.gameId,
      playerIndex,
      actionType: "check"
    });
  }, [game, submitAction]);

  const bet = useCallback((playerIndex: number, amount: number) => {
    if (!game) return;
    submitAction({
      gameId: game.gameId,
      playerIndex,
      actionType: "bet",
      amount
    });
  }, [game, submitAction]);

  const call = useCallback((playerIndex: number) => {
    if (!game) return;
    submitAction({
      gameId: game.gameId,
      playerIndex,
      actionType: "call"
    });
  }, [game, submitAction]);

  const raise = useCallback((playerIndex: number, amount: number) => {
    if (!game) return;
    submitAction({
      gameId: game.gameId,
      playerIndex,
      actionType: "raise",
      amount
    });
  }, [game, submitAction]);

  const fold = useCallback((playerIndex: number) => {
    if (!game) return;
    submitAction({
      gameId: game.gameId,
      playerIndex,
      actionType: "fold"
    });
  }, [game, submitAction]);

  const discard = useCallback((playerIndex: number, cardIndices: number[]) => {
    if (!game) return;
    submitAction({
      gameId: game.gameId,
      playerIndex,
      actionType: "discard",
      cardIndices
    });
  }, [game, submitAction]);

  const nextHand = useCallback((playerIndex: number) => {
    if (!game) return;
    submitAction({
      gameId: game.gameId,
      playerIndex,
      actionType: "nexthand"
    });
  }, [game, submitAction]);

  const clearError = useCallback(() => {
    setError(null);
  }, []);

  return {
    game,
    loading,
    error,
    startNewGame,
    refreshGame,
    submitAction,
    clearError,
    // Helper actions
    check,
    bet,
    call,
    raise,
    fold,
    discard,
    nextHand
  };
}
