import { useCallback, useEffect, useState } from "react";
import type { AccountSnapshot } from "@/shared/types/api";
import { apiClient } from "@/shared/api/instance";

interface UseBalances {
  data: AccountSnapshot | null;
  error: string | null;
  refresh: () => Promise<void>;
}

/** Loads account balances on mount and exposes a refresh callback. */
export function useBalances(): UseBalances {
  const [data, setData] = useState<AccountSnapshot | null>(null);
  const [error, setError] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    try {
      setData(await apiClient.getBalances());
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "failed to load balances");
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  return { data, error, refresh };
}
