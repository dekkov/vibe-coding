import { useState, useEffect } from "react";
import { ActionCapabilities, BettingView } from "@/lib/api";

interface ActionBarProps {
  capabilities: ActionCapabilities;
  betting: BettingView | null;
  selectedCards: number[];
  currentPlayer?: { index: number; chips: number; committedThisStreet: number };
  onCheck: () => void;
  onBet: (amount: number) => void;
  onCall: () => void;
  onRaise: (amount: number) => void;
  onFold: () => void;
  onDiscard: () => void;
  onNextHand: () => void;
  loading?: boolean;
}

export function ActionBar({
  capabilities,
  betting,
  selectedCards,
  currentPlayer,
  onCheck,
  onBet,
  onCall,
  onRaise,
  onFold,
  onDiscard,
  onNextHand,
  loading = false
}: ActionBarProps) {
  const [betAmount, setBetAmount] = useState(5);
  const [raiseAmount, setRaiseAmount] = useState(5);
  
  // Reset both bet and raise amounts to 5 when betting state changes
  useEffect(() => {
    setBetAmount(5); // Always reset bet to 5
    setRaiseAmount(5); // Always reset raise to 5
  }, [betting?.currentBet]);
  const buttonClass = "px-4 py-2 rounded-lg font-medium transition-all duration-200 disabled:opacity-50 disabled:cursor-not-allowed";
  const primaryButton = `${buttonClass} bg-blue-600 hover:bg-blue-700 text-white`;
  const secondaryButton = `${buttonClass} bg-gray-200 hover:bg-gray-300 text-gray-800`;
  const dangerButton = `${buttonClass} bg-red-600 hover:bg-red-700 text-white`;

  if (loading) {
    return (
      <div className="flex items-center justify-center p-4 bg-gray-50 rounded-lg">
        <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-blue-600"></div>
        <span className="ml-2 text-gray-600">Processing...</span>
      </div>
    );
  }

  return (
    <div className="p-4 bg-gray-50 rounded-lg">
      {/* Betting Info */}
      {betting && (
        <div className="mb-4 text-sm text-gray-600">
          <div className="flex justify-between items-center">
            <span>
              {betting.streetIndex === 0 ? "Pre-Draw" : "Post-Draw"} Betting
            </span>
            <span className="font-bold text-gray-800">
              Current Bet: <span className="text-red-600">{betting.currentBet}</span> | Cap: <span className="text-blue-600">{betting.cap}</span>
            </span>
          </div>
          {currentPlayer && betting.currentBet > 0 && (
            <div className="mt-1 text-xs font-semibold">
              You have committed: <span className="text-orange-600">{currentPlayer.committedThisStreet}</span> | 
              To call: <span className="text-green-600">{Math.max(0, betting.currentBet - currentPlayer.committedThisStreet)}</span>
            </div>
          )}
        </div>
      )}

      {/* Action Buttons */}
      <div className="flex flex-wrap gap-2">
        {/* Betting Actions */}
        {capabilities.canCheck && (
          <button
            className={secondaryButton}
            onClick={onCheck}
            disabled={loading}
          >
            Check
          </button>
        )}

        {capabilities.canBet && (
          <div className="flex items-center gap-2">
            <input
              type="number"
              min="1"
              max="30"
              value={betAmount}
              onChange={(e) => setBetAmount(Math.min(30, Math.max(1, parseInt(e.target.value) || 1)))}
              className="w-16 px-2 py-1 border rounded text-center"
              disabled={loading}
            />
            <button
              className={primaryButton}
              onClick={() => onBet(betAmount)}
              disabled={loading}
            >
              Bet
            </button>
          </div>
        )}

        {capabilities.canCall && (
          <button
            className={primaryButton}
            onClick={onCall}
            disabled={loading}
          >
            Call <span className="font-bold">{betting?.currentBet || 0}</span>
          </button>
        )}

        {capabilities.canRaise && (
          <div className="flex items-center gap-2">
            <input
              type="number"
              min={(betting?.currentBet || 0) + 1}
              max="30"
              value={raiseAmount}
              onChange={(e) => setRaiseAmount(Math.min(30, Math.max((betting?.currentBet || 0) + 1, parseInt(e.target.value) || (betting?.currentBet || 0) + 1)))}
              className="w-16 px-2 py-1 border rounded text-center"
              disabled={loading}
            />
            <button
              className={primaryButton}
              onClick={() => onRaise(raiseAmount)}
              disabled={loading}
            >
              Raise to
            </button>
          </div>
        )}

        {capabilities.canFold && (
          <button
            className={dangerButton}
            onClick={onFold}
            disabled={loading}
          >
            Fold
          </button>
        )}

        {/* Draw Actions */}
        {capabilities.canDiscard && (
          <button
            className={primaryButton}
            onClick={onDiscard}
            disabled={loading}
          >
            Discard {selectedCards.length} card{selectedCards.length !== 1 ? 's' : ''}
          </button>
        )}

        {/* Hand Progression */}
        {capabilities.canNextHand && (
          <button
            className={primaryButton}
            onClick={onNextHand}
            disabled={loading}
          >
            Next Hand
          </button>
        )}
      </div>

      {/* Helper Text */}
      <div className="mt-2 text-xs text-gray-500">
        {capabilities.canDiscard && selectedCards.length === 0 && (
          <p>Click cards to select them for discarding, then click &quot;Discard&quot;</p>
        )}
        {capabilities.canDiscard && selectedCards.length > 0 && (
          <p>Selected {selectedCards.length} card{selectedCards.length !== 1 ? 's' : ''} for discard</p>
        )}
      </div>
    </div>
  );
}
