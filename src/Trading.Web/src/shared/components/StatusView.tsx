import type { ApiConfig } from "@/shared/types/api";
import { StatusBadge } from "@/shared/components/StatusBadge";

/** Presentational config status (pure; the container supplies config/error). */
export function StatusView({ config, error }: { config: ApiConfig | null; error: string | null }) {
  if (error) {
    return <p className="text-sm text-red-600">Config unavailable: {error}</p>;
  }
  if (!config) {
    return <p className="text-sm text-slate-500">Loading config…</p>;
  }

  return (
    <div className="flex flex-col gap-2 text-sm">
      <div className="flex items-center gap-2">
        <span className="text-slate-600">Model:</span>
        {config.llmConfigured ? (
          <StatusBadge variant="ok">
            {config.provider} / {config.model}
          </StatusBadge>
        ) : (
          <StatusBadge variant="warn">not configured</StatusBadge>
        )}
      </div>
      <div className="flex flex-wrap items-center gap-2">
        <span className="text-slate-600">Data MCP:</span>
        <code className="rounded bg-slate-100 px-1 text-xs">{config.mcpUrl}</code>
        {config.mcpBearerSet ? (
          <StatusBadge variant="ok">bearer set</StatusBadge>
        ) : (
          <StatusBadge variant="warn">no bearer</StatusBadge>
        )}
      </div>
      <p className="text-xs text-slate-500">
        Keys are configured via the Home Assistant add-on options (or environment), not here.
      </p>
    </div>
  );
}
