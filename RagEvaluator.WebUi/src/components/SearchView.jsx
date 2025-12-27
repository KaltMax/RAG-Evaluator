import { useState } from 'react';
import { toast } from 'react-toastify';
import Searchbar from './Searchbar';
import SearchResults from './SearchResults';
import { postQuery } from '../api/PostQueryService';

function SearchView() {
  const [results, setResults] = useState(null);
  const [isLoading, setIsLoading] = useState(false);

  const handleSearch = async (question, topK) => {
    setIsLoading(true);
    setResults(null);

    try {
      const response = await postQuery(question, topK);

      // Transform the response to match our component's expected format
      const formattedResults = {
        queryId: response.queryId,
        question: response.question,
        answer: response.answer,
        sources: response.sources,
        timestamp: response.timestamp,
      };

      setResults(formattedResults);
      toast.success('Query processed successfully!');
    } catch (error) {
      console.error('Search error:', error);
      toast.error(error.message || 'Failed to process your query');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="w-full max-w-6xl mx-auto space-y-6">
      <div className="text-center mb-8">
        <h1 className="text-3xl font-bold text-white mb-2">RAG Document Search</h1>
        <p className="text-gray-400">Ask questions and get AI-powered answers from your documents</p>
      </div>

      <Searchbar onSearch={handleSearch} isLoading={isLoading} />

      {isLoading && (
        <div className="flex justify-center items-center py-12">
          <div className="text-center space-y-4">
            <div className="w-16 h-16 border-4 border-blue-500 border-t-transparent rounded-full animate-spin mx-auto" />
            <p className="text-gray-400">Processing your query...</p>
          </div>
        </div>
      )}

      {!isLoading && results && <SearchResults results={results} />}
    </div>
  );
}

export default SearchView;
