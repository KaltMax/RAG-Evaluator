const EXPERIMENT_COLORS = [
  { hex: "#3b82f6", name: "blue" },
  { hex: "#10b981", name: "emerald" },
  { hex: "#f59e0b", name: "amber" },
  { hex: "#ef4444", name: "red" },
  { hex: "#8b5cf6", name: "violet" },
  { hex: "#06b6d4", name: "cyan" },
  { hex: "#f97316", name: "orange" },
  { hex: "#ec4899", name: "pink" },
  { hex: "#14b8a6", name: "teal" },
  { hex: "#a855f7", name: "purple" },
  { hex: "#eab308", name: "yellow" },
  { hex: "#6366f1", name: "indigo" },
];

export const getExperimentColor = (index) =>
  EXPERIMENT_COLORS[index % EXPERIMENT_COLORS.length];

export default EXPERIMENT_COLORS;
