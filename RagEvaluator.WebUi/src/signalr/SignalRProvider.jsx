import { useEffect, useRef } from "react";
import { PropTypes } from "prop-types";
import { HubConnectionBuilder, LogLevel } from "@microsoft/signalr";
import { SignalRContext } from "./SignalRContext";

const JOB_UPDATE_EVENT = "JobUpdate";
const HUB_URL = "/hubs/jobs";

/**
 * Maintains a single SignalR connection to the /hubs/jobs hub for the whole app and
 * fans incoming JobUpdate notifications out to any registered subscribers. Mounted once
 * at the app root so notifications are available regardless of the current route.
 */
function SignalRProvider({ children }) {
  // Subscribers persist across reconnects. The connection is rebuilt only on mount/unmount.
  const subscribersRef = useRef(new Set());

  // Stable subscribe function shared via context.
  const apiRef = useRef({
    subscribe(handler) {
      subscribersRef.current.add(handler);
      return () => subscribersRef.current.delete(handler);
    },
  });

  useEffect(() => {
    const connection = new HubConnectionBuilder()
      .withUrl(HUB_URL)
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    const dispatch = (notification) => {
      subscribersRef.current.forEach((handler) => {
        try {
          handler(notification);
        } catch (err) {
          console.error("JobUpdate handler failed:", err);
        }
      });
    };

    connection.on(JOB_UPDATE_EVENT, dispatch);

    let isActive = true;
    connection.start().catch((err) => {
      if (isActive) console.error("SignalR connection failed to start:", err);
    });

    return () => {
      isActive = false;
      connection.off(JOB_UPDATE_EVENT, dispatch);
      connection.stop();
    };
  }, []);

  return (
    <SignalRContext.Provider value={apiRef.current}>
      {children}
    </SignalRContext.Provider>
  );
}

SignalRProvider.propTypes = {
  children: PropTypes.node,
};

export default SignalRProvider;
