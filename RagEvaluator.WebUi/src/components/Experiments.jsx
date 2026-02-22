import { useState } from "react";
import { useDropzone } from "react-dropzone";
import { toast } from "react-toastify";
import {
  DocumentArrowUpIcon,
  XMarkIcon,
  BeakerIcon,
} from "@heroicons/react/24/outline";
import { createExperiment } from "../api/experimentService";
import { formatLanguage } from "../utils/formatLanguage";

function Experiments() {
  const [experimentData, setExperimentData] = useState(null);
  const [fileName, setFileName] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [result, setResult] = useState(null);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    accept: {
      "application/json": [".json"],
    },
    maxFiles: 1,
    onDrop: (acceptedFiles, rejectedFiles) => {
      if (rejectedFiles.length > 0) {
        toast.error("Only .json files are allowed");
        return;
      }
      if (acceptedFiles.length === 0) return;

      const file = acceptedFiles[0];
      file.text().then((text) => {
        try {
          const parsed = JSON.parse(text);
          setExperimentData(parsed);
          setFileName(file.name);
          setResult(null);
        } catch {
          toast.error("Invalid JSON file: could not parse");
        }
      });
    },
  });

  const handleClear = () => {
    setExperimentData(null);
    setFileName("");
    setResult(null);
  };

  const handleSubmit = async () => {
    if (!experimentData) return;

    setIsSubmitting(true);
    setResult(null);

    try {
      const response = await createExperiment(experimentData);
      setResult({ success: true, data: response });
      toast.success("Experiment created successfully!");
      setExperimentData(null);
      setFileName("");
    } catch (error) {
      setResult({ success: false, error: error.message });
      toast.error(error.message);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="w-full max-w-4xl mx-auto space-y-6">
      <div className="text-center mb-8">
        <h1 className="text-3xl font-bold text-white mb-2">Experiments</h1>
        <p className="text-gray-400">
          Upload a JSON file to create a batch experiment
        </p>
      </div>

      {/* Drag-and-drop zone */}
      <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-6">
        <div
          {...getRootProps()}
          className={`border-2 border-dashed rounded-lg p-12 text-center cursor-pointer transition-all min-h-[240px] flex flex-col items-center justify-center ${
            isDragActive
              ? "border-blue-500 bg-blue-500/10"
              : "border-gray-600 hover:border-blue-400 hover:bg-blue-500/5 bg-[#1F1F1F]"
          }`}
        >
          <input {...getInputProps()} />
          <DocumentArrowUpIcon
            className={`w-16 h-16 mx-auto mb-4 transition-colors ${isDragActive ? "text-blue-400" : "text-gray-400"}`}
          />
          <div className="min-h-[56px] flex flex-col justify-center">
            {isDragActive ? (
              <p className="text-lg text-blue-400">
                Drop the JSON file here...
              </p>
            ) : (
              <>
                <p className="text-lg text-gray-300 mb-2">
                  Drag and drop a JSON experiment file here, or click to select
                </p>
                <p className="text-sm text-gray-500">
                  Only .json files are supported
                </p>
              </>
            )}
          </div>
        </div>

        {/* Preview card */}
        {experimentData && (
          <div className="mt-6 space-y-4">
            <div className="flex items-center justify-between">
              <p className="text-gray-300 font-medium">Preview: {fileName}</p>
              <button
                onClick={handleClear}
                className="text-gray-400 hover:text-red-400 transition-colors"
                disabled={isSubmitting}
              >
                <XMarkIcon className="w-6 h-6" />
              </button>
            </div>

            <div className="bg-[#1F1F1F] rounded-lg p-4 border border-gray-700">
              <div className="grid grid-cols-3 gap-4 text-sm">
                <div>
                  <span className="text-gray-400">Name</span>
                  <p className="text-white font-medium mt-1">
                    {experimentData.Name}
                  </p>
                </div>
                <div>
                  <span className="text-gray-400">Repeat Count</span>
                  <p className="text-white font-medium mt-1">
                    {experimentData.RepeatCount}
                  </p>
                </div>
                <div>
                  <span className="text-gray-400">Queries</span>
                  <p className="text-white font-medium mt-1">
                    {experimentData.Queries.length}
                  </p>
                </div>
              </div>
            </div>

            {/* Queries table */}
            <div className="bg-[#1F1F1F] rounded-lg border border-gray-700 overflow-hidden">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-700">
                    <th className="text-left text-gray-400 font-medium px-4 py-3">
                      #
                    </th>
                    <th className="text-left text-gray-400 font-medium px-4 py-3">
                      Question
                    </th>
                    <th className="text-left text-gray-400 font-medium px-4 py-3">
                      Language
                    </th>
                    <th className="text-left text-gray-400 font-medium px-4 py-3">
                      Top-K
                    </th>
                    <th className="text-left text-gray-400 font-medium px-4 py-3">
                      Ground Truth
                    </th>
                  </tr>
                </thead>
                <tbody>
                  {experimentData.Queries.map((query, index) => (
                    <tr
                      key={`${query.Question}-${query.Language}`}
                      className="border-b border-gray-700/50 last:border-b-0"
                    >
                      <td className="px-4 py-3 text-gray-500">{index + 1}</td>
                      <td className="px-4 py-3 text-gray-200">
                        {query.Question}
                      </td>
                      <td className="px-4 py-3 text-gray-200">
                        {formatLanguage(query.Language)}
                      </td>
                      <td className="px-4 py-3 text-gray-200">
                        {query.TopK ?? "-"}
                      </td>
                      <td className="px-4 py-3 text-gray-200">
                        {query.RelevantDocumentIds?.length > 0
                          ? `${query.RelevantDocumentIds.length} doc(s)`
                          : "-"}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>

            {/* Submit button */}
            <button
              onClick={handleSubmit}
              disabled={isSubmitting}
              className="w-full px-6 py-3 bg-blue-600 hover:bg-blue-700 disabled:bg-gray-600 disabled:cursor-not-allowed text-white font-medium rounded-lg transition-colors flex items-center justify-center gap-2"
            >
              {isSubmitting ? (
                <>
                  <div className="w-5 h-5 border-2 border-white border-t-transparent rounded-full animate-spin" />
                  <span>Creating Experiment...</span>
                </>
              ) : (
                <>
                  <BeakerIcon className="w-5 h-5" />
                  <span>Create Experiment</span>
                </>
              )}
            </button>
          </div>
        )}
      </div>

      {/* Result display */}
      {result && (
        <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-6">
          <h2 className="text-xl font-semibold text-white mb-4">Result</h2>
          <div
            className={`bg-[#1F1F1F] rounded-lg p-4 border ${
              result.success ? "border-green-700" : "border-red-700"
            }`}
          >
            {result.success ? (
              <div className="space-y-2">
                <div className="flex items-center gap-2 mb-3">
                  <div className="w-2 h-2 bg-green-500 rounded-full"></div>
                  <span className="text-green-400 font-medium">
                    Experiment Created
                  </span>
                </div>
                <div className="grid grid-cols-2 gap-2 text-sm">
                  <span className="text-gray-400">Experiment ID:</span>
                  <span className="text-gray-200 font-mono text-xs">
                    {result.data.id}
                  </span>
                  <span className="text-gray-400">Name:</span>
                  <span className="text-gray-200">{result.data.name}</span>
                </div>
              </div>
            ) : (
              <div>
                <div className="flex items-center gap-2 mb-2">
                  <div className="w-2 h-2 bg-red-500 rounded-full"></div>
                  <span className="text-red-400 font-medium">Failed</span>
                </div>
                <p className="text-red-400 text-sm mt-1">{result.error}</p>
              </div>
            )}
          </div>
        </div>
      )}
    </div>
  );
}

export default Experiments;
