import { PropTypes } from "prop-types";
import { TrashIcon } from "@heroicons/react/24/outline";
import { getExperimentColor } from "../../utils/experimentColors";
import {
  experimentSummaryShape,
  colorEntryShape,
} from "../../utils/statisticsPropTypes";

function ExperimentSelector({
  experiments,
  selectedIds,
  onToggle,
  onDelete,
  colorMap,
}) {
  const allSelectableIds = experiments
    .filter(
      (e) =>
        e.status === "Completed" && e.progress.annotated === e.progress.total,
    )
    .map((e) => e.id);

  const allSelected = allSelectableIds.every((id) => selectedIds.has(id));

  const handleSelectAll = () => {
    allSelectableIds.forEach((id) => {
      if (!selectedIds.has(id)) onToggle(id);
    });
  };

  const handleClear = () => {
    [...selectedIds].forEach((id) => onToggle(id));
  };

  return (
    <div className="bg-[#2D2D2D] rounded-lg p-6 border border-gray-700">
      {/* Header with Select All / Clear controls */}
      <div className="flex items-center justify-between mb-4">
        <h2 className="text-lg font-semibold text-white">Select Experiments</h2>
        <div className="flex gap-2">
          <button
            onClick={handleSelectAll}
            disabled={allSelected}
            className="px-3 py-1.5 text-xs font-medium rounded-lg bg-blue-600/20 text-blue-400 hover:bg-blue-600/30 transition-colors disabled:opacity-40"
          >
            Select All
          </button>
          <button
            onClick={handleClear}
            disabled={selectedIds.size === 0}
            className="px-3 py-1.5 text-xs font-medium rounded-lg bg-gray-600/20 text-gray-400 hover:bg-gray-600/30 transition-colors disabled:opacity-40"
          >
            Clear
          </button>
        </div>
      </div>
      {/* Experiment pills: color dot, name, config, annotation progress */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-3">
        {experiments.map((exp) => {
          const isSelectable =
            exp.status === "Completed" &&
            exp.progress.annotated === exp.progress.total;
          const isSelected = selectedIds.has(exp.id);
          const color = colorMap[exp.id] || getExperimentColor(0);

          return (
            <div key={exp.id} className="relative">
              <button
                onClick={() => isSelectable && onToggle(exp.id)}
                disabled={!isSelectable}
                className={`w-full text-left p-3 rounded-lg border transition-all ${
                  isSelected
                    ? "border-2 bg-[#1F1F1F]"
                    : isSelectable
                      ? "border border-gray-700 hover:border-gray-500 bg-[#1F1F1F]"
                      : "border border-gray-800 bg-[#1A1A1A] opacity-50 cursor-not-allowed"
                }`}
                style={isSelected ? { borderColor: color.hex } : undefined}
                title={exp.name}
              >
                <div className="flex items-center gap-2 mb-1 pr-6">
                  <span
                    className="w-3 h-3 rounded-full shrink-0"
                    style={{ backgroundColor: color.hex }}
                  />
                  <span className="text-sm font-medium text-white truncate flex-1">
                    {exp.name}
                  </span>
                </div>
                <p className="text-xs text-gray-400 truncate">
                  <span className="font-semibold text-gray-300">
                    Chat Model:
                  </span>{" "}
                  {exp.chatModel}
                </p>
                <p className="text-xs text-gray-400 truncate">
                  <span className="font-semibold text-gray-300">
                    Embedding Model:
                  </span>{" "}
                  {exp.embeddingModel}
                </p>
                <p className="text-xs text-gray-400 truncate">
                  <span className="font-semibold text-gray-300">
                    Chunking Strategy:
                  </span>{" "}
                  {exp.chunkingStrategy}
                </p>
                <p className="text-xs text-gray-400 truncate">
                  <span className="font-semibold text-gray-300">
                    Prompt Template:
                  </span>{" "}
                  {exp.promptTemplate}
                </p>
                <p className="text-xs text-gray-400 truncate">
                  <span className="font-semibold text-gray-300">
                    Repeat Count:
                  </span>{" "}
                  {exp.repeatCount}
                </p>
                <p className="text-xs text-gray-500 mt-1">
                  {exp.progress.annotated}/{exp.progress.total} annotated
                  {!isSelectable &&
                    exp.status !== "Completed" &&
                    ` · ${exp.status}`}
                </p>
              </button>
              {/* Delete button — outside the disabled button so it's always clickable */}
              <TrashIcon
                className="absolute top-3 right-3 w-4 h-4 text-gray-500 hover:text-red-400 shrink-0 transition-colors cursor-pointer"
                onClick={() => onDelete(exp.id)}
              />
            </div>
          );
        })}
      </div>
    </div>
  );
}

ExperimentSelector.propTypes = {
  experiments: PropTypes.arrayOf(experimentSummaryShape).isRequired,
  selectedIds: PropTypes.instanceOf(Set).isRequired,
  onToggle: PropTypes.func.isRequired,
  onDelete: PropTypes.func.isRequired,
  colorMap: PropTypes.objectOf(colorEntryShape).isRequired,
};

export default ExperimentSelector;
