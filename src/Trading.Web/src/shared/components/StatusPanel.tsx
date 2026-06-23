import { useConfig } from "@/shared/hooks/useConfig";
import { StatusView } from "@/shared/components/StatusView";

/** Container: loads the server config status and renders it read-only. */
export function StatusPanel() {
  const { config, error } = useConfig();
  return <StatusView config={config} error={error} />;
}
