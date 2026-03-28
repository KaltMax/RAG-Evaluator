import { useState, useEffect } from "react";
import { toast } from "react-toastify";
import { ArrowPathIcon } from "@heroicons/react/24/outline";
import {
  getExperiments,
  getExperimentById,
  deleteExperiment,
} from "../api/experimentService";
import { getExperimentColor } from "../utils/experimentColors";
import ExperimentSelector from "./statistics/ExperimentSelector";
import OverallComparisonTable from "./statistics/OverallComparisonTable";
import RetrievalMetricsChart from "./statistics/RetrievalMetricsChart";
import ResponseQualityChart from "./statistics/ResponseQualityChart";
import LanguageComparison from "./statistics/LanguageComparison";
import PerQueryBreakdown from "./statistics/PerQueryBreakdown";

function Statistics() {
  const [experiments, setExperiments] = useState([]);
  const [selectedIds, setSelectedIds] = useState(new Set());
  const [experimentDetails, setExperimentDetails] = useState({});
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState(null);
  const [loadingDetails, setLoadingDetails] = useState(new Set());

  const fetchExperiments = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await getExperiments();
      setExperiments(data);
    } catch (err) {
      const message = `Failed to load experiments: ${err.message}`;
      setError(message);
      toast.error(message);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchExperiments();
  }, []);

  // Build a stable color map: experiment id -> color, based on order in experiments list
  const colorMap = {};
  experiments.forEach((exp, i) => {
    colorMap[exp.id] = getExperimentColor(i);
  });

  const handleToggle = async (id) => {
    setSelectedIds((prev) => {
      const newSet = new Set(prev);
      if (newSet.has(id)) newSet.delete(id);
      else newSet.add(id);
      return newSet;
    });

    // Fetch details if not cached
    if (!experimentDetails[id]) {
      setLoadingDetails((prev) => new Set([...prev, id]));
      try {
        const data = await getExperimentById(id);
        setExperimentDetails((prev) => ({ ...prev, [id]: data }));
      } catch (err) {
        toast.error(`Failed to load experiment details: ${err.message}`);
        // Undo selection on error
        setSelectedIds((prev) => {
          const newSet = new Set(prev);
          newSet.delete(id);
          return newSet;
        });
      } finally {
        setLoadingDetails((prev) => {
          const newSet = new Set(prev);
          newSet.delete(id);
          return newSet;
        });
      }
    }
  };

  const handleDelete = async (id) => {
    if (!confirm("Are you sure you want to delete this experiment?")) return;
    try {
      await deleteExperiment(id);
      toast.success("Experiment deleted successfully");
      setSelectedIds((prev) => {
        const newSet = new Set(prev);
        newSet.delete(id);
        return newSet;
      });
      setExperimentDetails((prev) => {
        const updated = { ...prev };
        delete updated[id];
        return updated;
      });
      fetchExperiments();
    } catch (err) {
      toast.error(`Failed to delete experiment: ${err.message}`);
    }
  };

  const selectedExperiments = [...selectedIds]
    .map((id) => experimentDetails[id])
    .filter(Boolean);

  const isLoadingAny = loadingDetails.size > 0;

  return (
    <div className="w-full max-w-7xl mx-auto space-y-6">
      {/* Page header */}
      <div className="flex justify-between items-center mb-8">
        <div>
          <h1 className="text-3xl font-bold text-white mb-2">Statistics</h1>
          <p className="text-gray-400">
            Compare experiment results across configurations
          </p>
        </div>
        <button
          onClick={fetchExperiments}
          disabled={isLoading}
          className="flex items-center gap-2 px-4 py-2 bg-[#2D2D2D] hover:bg-[#3D3D3D] text-gray-300 rounded-lg transition-colors disabled:opacity-50 border border-gray-700"
        >
          <ArrowPathIcon
            className={`w-5 h-5 ${isLoading ? "animate-spin" : ""}`}
          />
          <span className="hidden md:inline">Refresh</span>
        </button>
      </div>

      {/* Experiment list with loading, error, and empty states */}
      {isLoading && experiments.length === 0 ? (
        <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-12 flex items-center justify-center">
          <ArrowPathIcon className="w-8 h-8 text-gray-400 animate-spin" />
        </div>
      ) : error ? (
        <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-12 text-center">
          <p className="text-red-400 mb-4">{error}</p>
          <button
            onClick={fetchExperiments}
            className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors"
          >
            Try Again
          </button>
        </div>
      ) : experiments.length === 0 ? (
        <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-12 text-center">
          <p className="text-gray-400">No experiments found</p>
        </div>
      ) : (
        <>
          {/* Pill-style experiment toggle selector */}
          <ExperimentSelector
            experiments={experiments}
            selectedIds={selectedIds}
            onToggle={handleToggle}
            onDelete={handleDelete}
            colorMap={colorMap}
          />

          {isLoadingAny && (
            <div className="flex items-center justify-center gap-2 py-4">
              <ArrowPathIcon className="w-5 h-5 text-gray-400 animate-spin" />
              <span className="text-gray-400 text-sm">
                Loading experiment details...
              </span>
            </div>
          )}

          {/* Comparison sections: require at least 2 selected experiments */}
          {selectedExperiments.length >= 2 ? (
            <div className="space-y-6">
              <OverallComparisonTable
                selectedExperiments={selectedExperiments}
                colorMap={colorMap}
              />
              <RetrievalMetricsChart
                selectedExperiments={selectedExperiments}
                colorMap={colorMap}
              />
              <ResponseQualityChart selectedExperiments={selectedExperiments} />
              <LanguageComparison
                selectedExperiments={selectedExperiments}
                colorMap={colorMap}
              />
              <PerQueryBreakdown
                selectedExperiments={selectedExperiments}
                colorMap={colorMap}
              />
            </div>
          ) : (
            <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-12 text-center border border-gray-700">
              <p className="text-gray-400">
                Select at least 2 experiments to compare
              </p>
            </div>
          )}
        </>
      )}
    </div>
  );
}

export default Statistics;
