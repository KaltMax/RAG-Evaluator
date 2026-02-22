import { useState } from "react";
import { MagnifyingGlassIcon } from "@heroicons/react/24/outline";
import { PropTypes } from "prop-types";

function Searchbar({ onSearch, isLoading }) {
  const [question, setQuestion] = useState("");
  const [topK, setTopK] = useState(3);
  const [language, setLanguage] = useState("en");

  const handleSubmit = (e) => {
    e.preventDefault();
    if (question.trim().length >= 3) {
      onSearch(question, topK, language);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="w-full">
      <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-6 space-y-4">
        {/* Question input, top-k selector, language selector, and search button */}
        <div className="flex flex-col md:flex-row gap-4">
          <div className="flex-1">
            <label
              htmlFor="question"
              className="block text-sm font-medium text-gray-300 mb-2"
            >
              Ask a Question
            </label>
            <input
              type="text"
              id="question"
              value={question}
              onChange={(e) => setQuestion(e.target.value)}
              placeholder="Enter your question here..."
              className="w-full px-4 py-3 bg-[#1F1F1F] border border-gray-700 rounded-lg text-white placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              disabled={isLoading}
              minLength={3}
              required
            />
          </div>

          <div className="w-full md:w-32">
            <label
              htmlFor="topK"
              className="block text-sm font-medium text-gray-300 mb-2"
            >
              Top Results
            </label>
            <select
              id="topK"
              value={topK}
              onChange={(e) => setTopK(Number(e.target.value))}
              className="w-full px-4 py-3 bg-[#1F1F1F] border border-gray-700 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              disabled={isLoading}
            >
              <option value={1}>1</option>
              <option value={3}>3</option>
              <option value={5}>5</option>
              <option value={10}>10</option>
            </select>
          </div>

          <div className="w-full md:w-32">
            <label
              htmlFor="language"
              className="block text-sm font-medium text-gray-300 mb-2"
            >
              Language
            </label>
            <select
              id="language"
              value={language}
              onChange={(e) => setLanguage(e.target.value)}
              className="w-full px-4 py-3 bg-[#1F1F1F] border border-gray-700 rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
              disabled={isLoading}
            >
              <option value="en">English</option>
              <option value="de">German</option>
            </select>
          </div>

          <div className="flex items-end">
            <button
              type="submit"
              disabled={isLoading || question.trim().length < 3}
              className="w-full md:w-auto px-6 py-3 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-600 disabled:cursor-not-allowed text-white font-medium rounded-lg transition-colors flex items-center justify-center gap-2"
            >
              {isLoading ? (
                <>
                  <div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin" />
                  <span>Searching...</span>
                </>
              ) : (
                <>
                  <MagnifyingGlassIcon className="w-5 h-5" />
                  <span>Search</span>
                </>
              )}
            </button>
          </div>
        </div>
      </div>
    </form>
  );
}

Searchbar.propTypes = {
  onSearch: PropTypes.func.isRequired,
  isLoading: PropTypes.bool.isRequired,
};

export default Searchbar;
