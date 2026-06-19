import { useEffect } from "react";
import { toast } from "react-toastify";
import { useJobNotifications } from "./SignalRContext";

const JOB_LABELS = {
  experiment: "Experiment",
  document: "Document",
  reprocess: "Reprocess",
};

/**
 * App-wide listener that shows a toast when any background job finishes, regardless of the
 * current route.
 */
function GlobalJobToasts() {
  const { subscribe } = useJobNotifications();

  useEffect(() => {
    const unsubscribe = subscribe((n) => {
      const label = JOB_LABELS[n.jobType] ?? "Job";
      const target = n.name ? `${label} '${n.name}'` : label;

      if (n.status === "Completed") {
        toast.success(`${target} completed`);
      } else if (n.status === "Failed") {
        const message = n.message ? `: ${n.message}` : "";
        toast.error(`${target} failed${message}`);
      }
    });

    return unsubscribe;
  }, [subscribe]);

  return null;
}

export default GlobalJobToasts;
