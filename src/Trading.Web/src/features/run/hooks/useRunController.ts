import { useCallback } from "react";
import { apiClient } from "@/shared/api/instance";
import { useRunStore } from "@/features/run/store";

/** Returns a callback that runs the crew, streaming role messages and the decision into the store. */
export function useRunController(): () => Promise<void> {
  const config = useRunStore((state) => state.config);
  const startRun = useRunStore((state) => state.startRun);
  const appendMessage = useRunStore((state) => state.appendMessage);
  const setDecision = useRunStore((state) => state.setDecision);
  const setError = useRunStore((state) => state.setError);

  return useCallback(async () => {
    startRun();
    try {
      await apiClient.runCrew(
        {
          symbol: config.symbol,
          interval: config.interval,
          market: config.market,
          provider: config.provider || undefined,
          model: config.model || undefined,
        },
        { onMessage: appendMessage, onDecision: setDecision, onError: setError },
      );
    } catch (err) {
      setError(err instanceof Error ? err.message : "run failed");
    }
  }, [config, startRun, appendMessage, setDecision, setError]);
}
