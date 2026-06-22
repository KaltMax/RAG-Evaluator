import { useEffect } from "react";
import { toast } from "react-toastify";
import { useJobNotifications } from "./SignalRContext";

// Per-jobType wording so each job reads naturally.
const JOBS = {
  experiment: { label: "Experiment", done: "completed", failed: "failed" },
  document: { label: "Document", done: "processed", failed: "failed to process" },
  reprocess: { label: "Reprocess", done: "completed", failed: "failed" },
};

/**
 * App-wide listener that shows a toast when any background job finishes, regardless of the
 * current route.
 */
function GlobalJobToasts() {
  const { subscribe } = useJobNotifications();

  useEffect(() => {
    const unsubscribe = subscribe((n) => {
      const job = JOBS[n.jobType];
      if (!job) return;

      const target = n.name ? `${job.label} '${n.name}'` : job.label;

      if (n.status === "Completed") {
        toast.success(`${target} ${job.done}`);
      } else if (n.status === "Failed") {
        const suffix = n.message ? `: ${n.message}` : "";
        toast.error(`${target} ${job.failed}${suffix}`);
      }
    });

    return unsubscribe;
  }, [subscribe]);

  return null;
}

export default GlobalJobToasts;
