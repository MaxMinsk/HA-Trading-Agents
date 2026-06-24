import { useCallback, useEffect, useState } from "react";
import type { SettingsDto, SettingsUpdate } from "@/shared/types/api";
import { apiClient } from "@/shared/api/instance";

interface UseSettings {
  dto: SettingsDto | null;
  error: string | null;
  saving: boolean;
  save: (update: SettingsUpdate) => Promise<void>;
}

/** Loads settings on mount and exposes a save that refreshes the masked DTO. */
export function useSettings(): UseSettings {
  const [dto, setDto] = useState<SettingsDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  useEffect(() => {
    let cancelled = false;
    apiClient
      .getSettings()
      .then((value) => {
        if (!cancelled) {
          setDto(value);
        }
      })
      .catch((err: unknown) => {
        if (!cancelled) {
          setError(err instanceof Error ? err.message : "failed to load settings");
        }
      });
    return () => {
      cancelled = true;
    };
  }, []);

  const save = useCallback(async (update: SettingsUpdate) => {
    setSaving(true);
    try {
      setDto(await apiClient.saveSettings(update));
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : "failed to save settings");
    } finally {
      setSaving(false);
    }
  }, []);

  return { dto, error, saving, save };
}
