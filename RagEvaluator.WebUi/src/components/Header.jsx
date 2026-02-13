import { Link } from 'react-router-dom';
import logo from '../assets/rag-evaluator.svg';

function Header() {
  return (
    <header className="bg-[#181818] border-b border-gray-800 text-white shadow-lg min-h-16 flex flex-wrap items-center justify-between px-6 py-2 gap-4 relative z-10 select-none">
      <Link to="/" className="flex items-center gap-3 text-xl font-semibold hover:opacity-80 transition-opacity relative group">
        <img src={logo} alt="Leaf" className="w-10 h-10 brightness-0 invert" />
        <span>RAG-Evaluator</span>
        <span className="absolute left-1/2 -translate-x-1/2 top-full mt-2 px-2 py-1 bg-gray-800 text-white text-sm font-semibold rounded opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap pointer-events-none z-20 hidden md:block">
            Home
        </span>
      </Link>
    </header>
  )
}

export default Header