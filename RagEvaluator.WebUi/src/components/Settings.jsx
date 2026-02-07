import { useState, useEffect } from 'react';
import { toast } from 'react-toastify';
import { getSettings } from '../api/GetSettingsService';
import { updateSettings } from '../api/UpdateSettings';
import { reprocessDocuments } from '../api/ReprocessDocumentsService';
import { ArrowPathIcon } from '@heroicons/react/24/outline';

function Settings() {
  const [settings, setSettings] = useState(null);
  const [draft, setDraft] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isReprocessing, setIsReprocessing] = useState(false);
  const [error, setError] = useState(null);

  const fetchSettings = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await getSettings();
      setSettings(data);
      setDraft(data);
    } catch (err) {
      setError(err.message);
      toast.error(err.message);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    fetchSettings();
  }, []);

  const hasChanges = () => {
    if (!settings || !draft) return false;
    return (
      draft.embeddingModel !== settings.embeddingModel ||
      draft.chunkingStrategy !== settings.chunkingStrategy ||
      draft.promptTemplate !== settings.promptTemplate ||
      draft.chunkSize !== settings.chunkSize ||
      draft.chunkOverlap !== settings.chunkOverlap ||
      draft.similarityThreshold !== settings.similarityThreshold
    );
  };

  const buildPatch = () => {
    const patch = {};
    if (draft.embeddingModel !== settings.embeddingModel) patch.embeddingModel = draft.embeddingModel;
    if (draft.chunkingStrategy !== settings.chunkingStrategy) patch.chunkingStrategy = draft.chunkingStrategy;
    if (draft.promptTemplate !== settings.promptTemplate) patch.promptTemplate = draft.promptTemplate;
    if (draft.chunkSize !== settings.chunkSize) patch.chunkSize = draft.chunkSize;
    if (draft.chunkOverlap !== settings.chunkOverlap) patch.chunkOverlap = draft.chunkOverlap;
    if (draft.similarityThreshold !== settings.similarityThreshold) patch.similarityThreshold = draft.similarityThreshold;
    return patch;
  };

  const needsReprocessing = () => {
    if (!settings || !draft) return false;
    return (
      draft.embeddingModel !== settings.embeddingModel ||
      draft.chunkingStrategy !== settings.chunkingStrategy ||
      draft.chunkSize !== settings.chunkSize ||
      draft.chunkOverlap !== settings.chunkOverlap ||
      draft.similarityThreshold !== settings.similarityThreshold
    );
  };

  const handleSave = async () => {
    const shouldReprocess = needsReprocessing();
    setIsSaving(true);
    try {
      const data = await updateSettings(buildPatch());
      setSettings(data);
      setDraft(data);
      toast.success('Settings updated successfully');

      if (shouldReprocess) {
        setIsReprocessing(true);
        try {
          const result = await reprocessDocuments();
          toast.success(`Reprocessed ${result.documentsProcessed} documents (${result.totalChunksCreated} chunks)`);
        } catch (err) {
          toast.error(`Reprocessing failed: ${err.message}`);
        } finally {
          setIsReprocessing(false);
        }
      }
    } catch (err) {
      toast.error(err.message);
    } finally {
      setIsSaving(false);
    }
  };

  const handleDiscard = () => {
    setDraft({ ...settings });
  };

  if (isLoading) {
    return (
      <div className="w-full max-w-6xl mx-auto">
        <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-12 flex justify-center">
          <ArrowPathIcon className="w-8 h-8 text-gray-400 animate-spin" />
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="w-full max-w-6xl mx-auto">
        <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-12 text-center">
          <p className="text-red-400 mb-4">{error}</p>
          <button
            onClick={fetchSettings}
            className="px-4 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors"
          >
            Try Again
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="w-full max-w-6xl mx-auto space-y-6">
      <div className="flex justify-between items-center mb-8">
        <div>
          <h1 className="text-3xl font-bold text-white mb-2">Settings</h1>
          <p className="text-gray-400">Current RAG system configuration</p>
        </div>
        <button
          onClick={fetchSettings}
          disabled={isLoading}
          className="flex items-center gap-2 px-4 py-2 bg-[#2D2D2D] hover:bg-[#3D3D3D] text-gray-300 rounded-lg transition-colors disabled:opacity-50"
        >
          <ArrowPathIcon className={`w-5 h-5 ${isLoading ? 'animate-spin' : ''}`} />
          <span className="hidden md:inline">Refresh</span>
        </button>
      </div>

      {draft && settings && (
        <>
          {/* Embedding Model */}
          <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-6">
            <h2 className="text-sm font-bold text-gray-200 mb-2">Embedding Model</h2>
            <p className="text-gray-400 text-xs mb-4">Active model used for generating embeddings</p>
            <div className="flex flex-wrap gap-2">
              {settings.availableEmbeddingModels?.map((model) => (
                <button
                  key={model}
                  onClick={() => setDraft({ ...draft, embeddingModel: model })}
                  className={`px-3 py-1.5 rounded-full text-sm font-medium transition-colors ${model === draft.embeddingModel ? 'bg-blue-500/20 text-blue-400 border border-blue-500/40' : 'bg-[#1F1F1F] text-gray-300 border border-gray-700 hover:border-gray-500'}`}
                >
                  {model}
                </button>
              ))}
            </div>
          </div>

          {/* Chunking Strategy */}
          <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-6">
            <h2 className="text-sm font-bold text-gray-200 mb-2">Chunking Strategy</h2>
            <p className="text-gray-400 text-xs mb-4">How documents are split into chunks for retrieval</p>
            <div className="flex flex-wrap gap-2 mb-4">
              {settings.availableChunkingStrategies?.map((strategy) => (
                <button
                  key={strategy}
                  onClick={() => setDraft({ ...draft, chunkingStrategy: strategy })}
                  className={`px-3 py-1.5 rounded-full text-sm font-medium transition-colors ${strategy === draft.chunkingStrategy ? 'bg-blue-500/20 text-blue-400 border border-blue-500/40' : 'bg-[#1F1F1F] text-gray-300 border border-gray-700 hover:border-gray-500'}`}
                >
                  {strategy}
                </button>
              ))}
            </div>
            {draft.chunkingStrategy === 'FixedSize' && (
              <div className="grid grid-cols-2 gap-4">
                <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700">
                  <label htmlFor="chunkSize" className="text-gray-400 text-xs mb-1 block">Chunk Size</label>
                  <input
                    id="chunkSize"
                    type="number"
                    min="1"
                    max="2000"
                    value={draft.chunkSize}
                    onChange={(e) => setDraft({ ...draft, chunkSize: Number.parseInt(e.target.value) || 0 })}
                    className="w-full bg-transparent text-gray-200 text-sm font-medium outline-none"
                  />
                </div>
                <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700">
                  <label htmlFor="chunkOverlap" className="text-gray-400 text-xs mb-1 block">Chunk Overlap</label>
                  <input
                    id="chunkOverlap"
                    type="number"
                    min="0"
                    max="1000"
                    value={draft.chunkOverlap}
                    onChange={(e) => setDraft({ ...draft, chunkOverlap: Number.parseInt(e.target.value) || 0 })}
                    className="w-full bg-transparent text-gray-200 text-sm font-medium outline-none"
                  />
                </div>
              </div>
            )}
            {draft.chunkingStrategy === 'Semantic' && (
              <div className="grid grid-cols-1 max-w-xs gap-4">
                <div className="bg-[#1F1F1F] rounded-lg p-3 border border-gray-700">
                  <label htmlFor="similarityThreshold" className="text-gray-400 text-xs mb-1 block">Similarity Threshold</label>
                  <input
                    id="similarityThreshold"
                    type="number"
                    min="0"
                    max="1"
                    step="0.05"
                    value={draft.similarityThreshold}
                    onChange={(e) => setDraft({ ...draft, similarityThreshold: Number.parseFloat(e.target.value) || 0 })}
                    className="w-full bg-transparent text-gray-200 text-sm font-medium outline-none"
                  />
                </div>
              </div>
            )}
          </div>

          {/* Prompt Template */}
          <div className="bg-[#2D2D2D] rounded-lg shadow-lg p-6">
            <h2 className="text-sm font-bold text-gray-200 mb-2">Prompt Template</h2>
            <p className="text-gray-400 text-xs mb-4">System prompt strategy for cross-language evaluation</p>
            <div className="space-y-4">
              {/* Basic */}
              <button
                onClick={() => setDraft({ ...draft, promptTemplate: 'Basic' })}
                className={`w-full text-left rounded-lg p-4 border transition-colors ${draft.promptTemplate === 'Basic' ? 'bg-blue-500/5 border-blue-500/40' : 'bg-[#1F1F1F] border-gray-700 hover:border-gray-500'}`}
              >
                <span className={`px-3 py-1 rounded-full text-sm font-medium ${draft.promptTemplate === 'Basic' ? 'bg-blue-500/20 text-blue-400' : 'bg-gray-700 text-gray-300'}`}>
                  Basic
                </span>
                <p className="text-gray-300 text-sm leading-relaxed mt-3">{settings.promptBasicText}</p>
              </button>

              {/* Instructed */}
              <button
                onClick={() => setDraft({ ...draft, promptTemplate: 'Instructed' })}
                className={`w-full text-left rounded-lg p-4 border transition-colors ${draft.promptTemplate === 'Instructed' ? 'bg-blue-500/5 border-blue-500/40' : 'bg-[#1F1F1F] border-gray-700 hover:border-gray-500'}`}
              >
                <span className={`px-3 py-1 rounded-full text-sm font-medium ${draft.promptTemplate === 'Instructed' ? 'bg-blue-500/20 text-blue-400' : 'bg-gray-700 text-gray-300'}`}>
                  Instructed
                </span>
                <p className="text-gray-300 text-sm leading-relaxed mt-3">{settings.promptInstructedText}</p>
              </button>

              {/* LanguageAware */}
              <button
                onClick={() => setDraft({ ...draft, promptTemplate: 'LanguageAware' })}
                className={`w-full text-left rounded-lg p-4 border transition-colors ${draft.promptTemplate === 'LanguageAware' ? 'bg-blue-500/5 border-blue-500/40' : 'bg-[#1F1F1F] border-gray-700 hover:border-gray-500'}`}
              >
                <span className={`px-3 py-1 rounded-full text-sm font-medium ${draft.promptTemplate === 'LanguageAware' ? 'bg-blue-500/20 text-blue-400' : 'bg-gray-700 text-gray-300'}`}>
                  Language Aware
                </span>
                <div className="mt-3 space-y-2">
                  <div className="bg-[#1F1F1F] rounded-lg p-3">
                    <p className="text-gray-400 text-xs mb-1">EN</p>
                    <p className="text-gray-300 text-sm leading-relaxed">{settings.promptLanguageAwareEnText}</p>
                  </div>
                  <div className="bg-[#1F1F1F] rounded-lg p-3">
                    <p className="text-gray-400 text-xs mb-1">DE</p>
                    <p className="text-gray-300 text-sm leading-relaxed">{settings.promptLanguageAwareDeText}</p>
                  </div>
                </div>
              </button>
            </div>
          </div>

          {/* Reprocessing Indicator */}
          {isReprocessing && (
            <div className="sticky bottom-4 flex items-center gap-3 bg-[#2D2D2D] rounded-lg shadow-lg p-4 border border-blue-500/40">
              <ArrowPathIcon className="w-5 h-5 text-blue-400 animate-spin" />
              <span className="text-blue-400 text-sm font-medium">Reprocessing documents with new configuration...</span>
            </div>
          )}

          {/* Save / Discard Bar */}
          {hasChanges() && !isReprocessing && (
            <div className="sticky bottom-4 flex justify-end gap-3 bg-[#2D2D2D] rounded-lg shadow-lg p-4 border border-gray-700">
              <button
                onClick={handleDiscard}
                disabled={isSaving}
                className="px-4 py-2 text-gray-300 hover:text-white transition-colors disabled:opacity-50"
              >
                Discard
              </button>
              <button
                onClick={handleSave}
                disabled={isSaving}
                className="px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white rounded-lg transition-colors disabled:opacity-50"
              >
                {isSaving ? 'Saving...' : 'Save Changes'}
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}

export default Settings;
