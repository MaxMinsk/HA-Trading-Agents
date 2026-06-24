import { useEffect, useState } from "react";
import type { ChangeEvent, FormEvent } from "react";
import { useSettings } from "@/shared/hooks/useSettings";
import { buildSettingsUpdate, formFromDto, type SettingsForm } from "@/shared/lib/settings";
import { StatusBadge } from "@/shared/components/StatusBadge";
import type { SecretStatus } from "@/shared/types/api";

const providers = [
  { value: "", label: "(unset)" },
  { value: "anthropic", label: "Anthropic" },
  { value: "openai", label: "OpenAI" },
];

const fieldClass =
  "rounded border border-slate-300 px-2 py-1 text-sm focus:border-slate-500 focus:outline-none";

function secretPlaceholder(status: SecretStatus): string {
  return status.set ? `set (${status.hint ?? "••••"}) — leave blank to keep` : "not set";
}

/** Editable settings: provider/model/MCP URL plus write-only secret fields. Keys stay server-side. */
export function SettingsPanel() {
  const { dto, error, saving, save } = useSettings();
  const [form, setForm] = useState<SettingsForm | null>(null);

  useEffect(() => {
    if (dto) {
      setForm(formFromDto(dto));
    }
  }, [dto]);

  if (error && !dto) {
    return <p className="text-sm text-red-600">Settings unavailable: {error}</p>;
  }
  if (!dto || !form) {
    return <p className="text-sm text-slate-500">Loading settings…</p>;
  }

  const current = form;

  function set(field: keyof SettingsForm) {
    return (e: ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
      setForm((f) => (f ? { ...f, [field]: e.target.value } : f));
  }

  function onSubmit(e: FormEvent) {
    e.preventDefault();
    void save(buildSettingsUpdate(current));
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-3 text-sm">
      <label className="flex flex-col gap-1 text-xs text-slate-600">
        Provider
        <select className={fieldClass} value={current.llmProvider} onChange={set("llmProvider")}>
          {providers.map((p) => (
            <option key={p.value} value={p.value}>
              {p.label}
            </option>
          ))}
        </select>
      </label>
      <label className="flex flex-col gap-1 text-xs text-slate-600">
        Model
        <input className={fieldClass} value={current.llmModel} placeholder="provider default" onChange={set("llmModel")} />
      </label>
      <label className="flex flex-col gap-1 text-xs text-slate-600">
        LLM API key
        <input
          className={fieldClass}
          type="password"
          value={current.llmApiKey}
          placeholder={secretPlaceholder(dto.llmApiKey)}
          onChange={set("llmApiKey")}
        />
      </label>
      <label className="flex flex-col gap-1 text-xs text-slate-600">
        Data MCP URL
        <input className={fieldClass} value={current.mcpUrl} placeholder="http://host:8080/mcp" onChange={set("mcpUrl")} />
      </label>
      <label className="flex flex-col gap-1 text-xs text-slate-600">
        MCP bearer
        <input
          className={fieldClass}
          type="password"
          value={current.mcpBearer}
          placeholder={secretPlaceholder(dto.mcpBearer)}
          onChange={set("mcpBearer")}
        />
      </label>

      <div className="flex items-center gap-3">
        <button
          type="submit"
          disabled={saving}
          className="rounded bg-slate-800 px-3 py-1.5 text-sm font-medium text-white hover:bg-slate-700 disabled:opacity-50"
        >
          {saving ? "Saving…" : "Save settings"}
        </button>
        {dto.llmConfigured ? (
          <StatusBadge variant="ok">crew ready</StatusBadge>
        ) : (
          <StatusBadge variant="warn">no model configured</StatusBadge>
        )}
        {error && <span className="text-xs text-red-600">{error}</span>}
      </div>
      <p className="text-xs text-slate-500">Secrets are stored on the server (masked here), or set them via the add-on options.</p>
    </form>
  );
}
