import { PlayerView } from "@/lib/api";
import { Card } from "./Card";

interface PlayerPanelProps {
  player: PlayerView;
  isActive: boolean;
  isStartingPlayer: boolean;
  selectedCards: number[];
  onCardSelect?: (cardIndex: number) => void;
  className?: string;
}

export function PlayerPanel({ 
  player, 
  isActive, 
  isStartingPlayer,
  selectedCards, 
  onCardSelect,
  className = "" 
}: PlayerPanelProps) {
  return (
    <div className={`
      p-4 rounded-lg border-2 transition-all duration-300
      ${isActive ? "border-blue-500 bg-blue-50" : "border-gray-300 bg-white"}
      ${player.hasFolded ? "opacity-50" : ""}
      ${className}
    `}>
      {/* Player Info */}
      <div className="flex items-center justify-between mb-3">
        <div className="flex items-center gap-2">
          <h3 className="font-black text-xl text-gray-900">
            Player {player.index + 1}
            {isStartingPlayer && <span className="text-sm text-blue-700 font-bold ml-1">(Starting)</span>}
          </h3>
          {player.hasFolded && (
            <span className="px-2 py-1 bg-red-100 text-red-800 text-xs rounded-full">
              FOLDED
            </span>
          )}
        </div>
        <div className="text-right">
          <div className="font-black text-lg text-green-700">
            💰 {player.chips} chips
          </div>
          {player.committedThisStreet > 0 && (
            <div className="text-sm font-bold text-orange-600">
              Committed: {player.committedThisStreet}
            </div>
          )}
        </div>
      </div>

      {/* Hand */}
      <div className="mb-2">
        <div className="text-sm text-gray-600 mb-2">Hand:</div>
        <div className="flex gap-2 flex-wrap">
          {player.hand.map((card, index) => (
            <Card
              key={index}
              card={card}
              selected={selectedCards.includes(index)}
              onClick={onCardSelect ? () => onCardSelect(index) : undefined}
              className={onCardSelect ? "hover:ring-2 hover:ring-gray-400" : ""}
            />
          ))}
        </div>
      </div>

      {/* Status Indicator */}
      {isActive && (
        <div className="text-center">
          <div className="inline-flex items-center px-3 py-1 bg-blue-500 text-white text-sm rounded-full">
            <div className="w-2 h-2 bg-white rounded-full mr-2 animate-pulse"></div>
            Your Turn
          </div>
        </div>
      )}
    </div>
  );
}
