import { useEffect, useState } from "react";
import type { ApiConfig } from "@/shared/types/api";
import { apiClient } from "@/shared/api/instance";

interface UseConfig {
  config: ApiConfig | null;
  error: string | null;
}

/** Loads the read-only server config status once on mount. */
export function useConfig(): UseConfig {
  const [config, setConfig] = useState<ApiConfig | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    apiClient
      .getConfig()
      .then((value) => {
        if (!cancelled) {
          setConfig(value);
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "failed to load config");
        }
      });
    return () => {
      cancelled = true;
    };
  }, []);

  return { config, error };
}
