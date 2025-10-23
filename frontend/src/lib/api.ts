export const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5169";

// Game API Types
export type CardView = {
  rank: string | null;
  suit: string | null;
  isJoker: boolean;
};

export type PlayerView = {
  index: number;
  username?: string;
  chips: number;
  hasFolded: boolean;
  hand: CardView[];
  committedThisStreet: number;
};

export type BettingView = {
  streetIndex: number;
  currentBet: number;
  increment: number;
  cap: number;
  toActPlayerIndex: number;
  isClosed: boolean;
};

export type ShowdownView = {
  winnerIndex: number;
  hands: {
    handType: string;
    description: string;
    primaryRanks: string[];
    kickers: string[];
  }[];
};

export type ActionCapabilities = {
  canCheck: boolean;
  canBet: boolean;
  canCall: boolean;
  canRaise: boolean;
  canFold: boolean;
  canDiscard: boolean;
  canNextHand: boolean;
};

export type GameView = {
  gameId: string;
  phase: string;
  handNumber: number;
  startingPlayerIndex: number;
  pot: number;
  deckRemaining: number;
  players: PlayerView[];
  betting: BettingView | null;
  showdown: ShowdownView | null;
  actionCapabilities: ActionCapabilities;
  lastEvent: string;
  isMatchComplete: boolean;
  matchWinnerIndex: number | null;
  drawPhaseActivePlayer?: number | null;
};

export type GameResponse = {
  success: boolean;
  error?: string;
  game?: GameView;
};

export type ActionRequest = {
  gameId: string;
  playerIndex: number;
  actionType: string;
  amount?: number;
  cardIndices?: number[];
};

// API Functions
export async function createNewGame(): Promise<GameResponse> {
  const res = await fetch(`${API_BASE_URL}/api/game/new`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({}),
  });
  
  if (!res.ok) {
    throw new Error(`Failed to create game: ${res.status}`);
  }
  
  return res.json();
}

export async function getGameState(gameId: string): Promise<GameResponse> {
  const res = await fetch(`${API_BASE_URL}/api/game/state?gameId=${encodeURIComponent(gameId)}`, {
    cache: "no-store",
  });
  
  if (!res.ok) {
    throw new Error(`Failed to get game state: ${res.status}`);
  }
  
  return res.json();
}

export async function processAction(request: ActionRequest): Promise<GameResponse> {
  const res = await fetch(`${API_BASE_URL}/api/game/action`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  
  if (!res.ok) {
    throw new Error(`Failed to process action: ${res.status}`);
  }
  
  return res.json();
} 