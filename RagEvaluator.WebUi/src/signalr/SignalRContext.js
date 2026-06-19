import { createContext, useContext } from "react";

/**
 * Context value: { subscribe }.
 * `subscribe(handler)` registers a callback for incoming JobUpdate notifications
 * and returns an unsubscribe function.
 */
export const SignalRContext = createContext(null);

export function useJobNotifications() {
  return useContext(SignalRContext);
}
