import { CardView } from "@/lib/api";

interface CardProps {
  card: CardView;
  selected?: boolean;
  onClick?: () => void;
  className?: string;
}

const suitSymbols: Record<string, string> = {
  "Spades": "♠",
  "Hearts": "♥", 
  "Diamonds": "♦",
  "Clubs": "♣"
};

const suitColors: Record<string, string> = {
  "Spades": "text-black",
  "Hearts": "text-red-600",
  "Diamonds": "text-red-600", 
  "Clubs": "text-black"
};

export function Card({ card, selected = false, onClick, className = "" }: CardProps) {
  // Show card back if rank and suit are null (masked opponent card)
  if (card.rank === null && card.suit === null && !card.isJoker) {
    return (
      <div
        className={`
          w-16 h-24 bg-gradient-to-br from-blue-600 to-blue-800 
          border-2 border-blue-900 rounded-lg flex items-center justify-center
          transition-all duration-200
          ${className}
        `}
      >
        <div className="text-white font-bold text-3xl">🂠</div>
      </div>
    );
  }

  if (card.isJoker) {
    return (
      <div
        className={`
          w-16 h-24 bg-gradient-to-br from-purple-500 to-purple-700 
          border-2 border-purple-800 rounded-lg flex items-center justify-center
          cursor-pointer transition-all duration-200 hover:scale-105
          ${selected ? "ring-4 ring-blue-400 ring-offset-2" : ""}
          ${className}
        `}
        onClick={onClick}
      >
        <div className="text-white font-bold text-sm text-center">
          <div>🃏</div>
          <div className="text-xs">JOKER</div>
        </div>
      </div>
    );
  }

  const suit = card.suit || "";
  const rank = card.rank || "";
  const suitSymbol = suitSymbols[suit] || suit;
  const suitColor = suitColors[suit] || "text-black";

  return (
    <div
      className={`
        w-16 h-24 bg-white border-2 border-gray-300 rounded-lg 
        flex flex-col items-center justify-between p-1
        cursor-pointer transition-all duration-200 hover:scale-105
        ${selected ? "ring-4 ring-blue-400 ring-offset-2" : ""}
        ${className}
      `}
      onClick={onClick}
    >
      <div className={`text-sm font-bold ${suitColor}`}>
        <div className="text-center">{rank.charAt(0)}</div>
        <div className="text-center text-lg leading-none">{suitSymbol}</div>
      </div>
      <div className={`text-lg ${suitColor} transform rotate-180`}>
        {suitSymbol}
      </div>
    </div>
  );
}
