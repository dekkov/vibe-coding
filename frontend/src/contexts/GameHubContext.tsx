"use client";

import { createContext, useContext, ReactNode } from "react";
import { useGameHub, GameHubContextType } from "@/hooks/useGameHub";

const GameHubContext = createContext<GameHubContextType | null>(null);

export function GameHubProvider({ children }: { children: ReactNode }) {
  const gameHub = useGameHub();

  return (
    <GameHubContext.Provider value={gameHub}>
      {children}
    </GameHubContext.Provider>
  );
}

export function useGameHubContext() {
  const context = useContext(GameHubContext);
  if (!context) {
    throw new Error("useGameHubContext must be used within GameHubProvider");
  }
  return context;
}

