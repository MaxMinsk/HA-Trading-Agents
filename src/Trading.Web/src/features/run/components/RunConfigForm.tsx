import type { FormEvent } from "react";
import { useRunController } from "@/features/run/hooks/useRunController";
import { useRunStore } from "@/features/run/store";

const intervals = ["1h", "4h", "1d"];
const markets = ["spot", "usdm"];
const providers = [
  { value: "", label: "server default" },
  { value: "anthropic", label: "Anthropic" },
  { value: "openai", label: "OpenAI" },
];

const fieldClass =
  "rounded border border-slate-300 px-2 py-1 text-sm focus:border-slate-500 focus:outline-none";

export function RunConfigForm() {
  const config = useRunStore((state) => state.config);
  const setConfig = useRunStore((state) => state.setConfig);
  const status = useRunStore((state) => state.status);
  const run = useRunController();

  function onSubmit(event: FormEvent) {
    event.preventDefault();
    void run();
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-wrap items-end gap-3">
      <label className="flex flex-col gap-1 text-xs text-slate-600">
        Symbol
        <input
          className={fieldClass}
          value={config.symbol}
          onChange={(e) => setConfig({ symbol: e.target.value.toUpperCase() })}
        />
      </label>
      <label className="flex flex-col gap-1 text-xs text-slate-600">
        Interval
        <select className={fieldClass} value={config.interval} onChange={(e) => setConfig({ interval: e.target.value })}>
          {intervals.map((i) => (
            <option key={i} value={i}>
              {i}
            </option>
          ))}
        </select>
      </label>
      <label className="flex flex-col gap-1 text-xs text-slate-600">
        Market
        <select className={fieldClass} value={config.market} onChange={(e) => setConfig({ market: e.target.value })}>
          {markets.map((m) => (
            <option key={m} value={m}>
              {m}
            </option>
          ))}
        </select>
      </label>
      <label className="flex flex-col gap-1 text-xs text-slate-600">
        Provider
        <select className={fieldClass} value={config.provider} onChange={(e) => setConfig({ provider: e.target.value })}>
          {providers.map((p) => (
            <option key={p.value} value={p.value}>
              {p.label}
            </option>
          ))}
        </select>
      </label>
      <label className="flex flex-col gap-1 text-xs text-slate-600">
        Model (optional)
        <input
          className={fieldClass}
          value={config.model}
          placeholder="server default"
          onChange={(e) => setConfig({ model: e.target.value })}
        />
      </label>
      <button
        type="submit"
        disabled={status === "running"}
        className="rounded bg-slate-800 px-4 py-1.5 text-sm font-medium text-white hover:bg-slate-700 disabled:opacity-50"
      >
        {status === "running" ? "Running…" : "Run crew"}
      </button>
    </form>
  );
}
