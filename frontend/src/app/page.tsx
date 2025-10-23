import Link from "next/link";

export default function Home() {
  return (
    <div className="min-h-screen bg-gradient-to-br from-blue-50 to-indigo-100 flex items-center justify-center p-4">
      <div className="text-center max-w-2xl">
        <div className="text-8xl mb-6">🃏</div>
        <h1 className="text-5xl font-bold text-gray-800 mb-4">17 Poker</h1>
        <p className="text-xl text-gray-600 mb-8 leading-relaxed">
          A unique heads-up poker variant played with just 17 cards:<br />
          4 Aces, 4 Kings, 4 Queens, 4 Jacks, and 1 wild Joker.
        </p>
        
        <div className="bg-white rounded-lg shadow-lg p-6 mb-8">
          <h2 className="text-2xl font-semibold text-gray-800 mb-4">How to Play</h2>
          <div className="text-left space-y-2 text-gray-600">
            <p>• Two players compete over 10 hands</p>
            <p>• Each player starts with 100 chips</p>
            <p>• Fixed-limit betting: 5 chip increments, 30 chip cap per street</p>
            <p>• Two betting rounds: pre-draw and post-draw</p>
            <p>• Players can discard and redraw cards between betting rounds</p>
            <p>• Winner is determined by who has the most chips after 10 hands</p>
          </div>
        </div>

        <div className="flex gap-4 justify-center">
          <Link
            href="/lobby"
            className="inline-block px-8 py-4 bg-blue-600 hover:bg-blue-700 text-white text-xl font-semibold rounded-lg transition-colors shadow-lg hover:shadow-xl transform hover:scale-105"
          >
            Play Online
          </Link>
          <Link
            href="/play"
            className="inline-block px-8 py-4 bg-gray-600 hover:bg-gray-700 text-white text-xl font-semibold rounded-lg transition-colors shadow-lg hover:shadow-xl transform hover:scale-105"
          >
            Local Hot-Seat
          </Link>
        </div>

        <div className="mt-8 text-sm text-gray-500">
          <p>Play online with a friend or local hot-seat mode</p>
        </div>
      </div>
    </div>
  );
}
