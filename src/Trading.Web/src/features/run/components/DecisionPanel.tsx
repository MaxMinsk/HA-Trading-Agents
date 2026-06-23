import { useState } from "react";
import type { ExecutionOutcome } from "@/shared/types/api";
import { apiClient } from "@/shared/api/instance";
import { StatusBadge } from "@/shared/components/StatusBadge";
import { useRunStore } from "@/features/run/store";
import { actionVariant, isSubmittable } from "@/features/run/lib/decision";

export function DecisionPanel() {
  const decision = useRunStore((state) => state.decision);
  const config = useRunStore((state) => state.config);
  const [outcome, setOutcome] = useState<ExecutionOutcome | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string | null>(null);

  if (!decision) {
    return <p className="text-sm text-slate-500">No decision yet.</p>;
  }

  const submittable = isSubmittable(decision);

  async function submit() {
    if (!decision || !submittable) {
      return;
    }

    const confirmed = window.confirm(
      `Submit ${decision.action} ${decision.sizeFraction} of ${config.symbol}? This sends a real order intent to the risk gate.`,
    );
    if (!confirmed) {
      return;
    }

    setSubmitting(true);
    setSubmitError(null);
    try {
      const result = await apiClient.execute({
        symbol: config.symbol,
        action: decision.action.toLowerCase(),
        sizeFraction: decision.sizeFraction,
        interval: config.interval,
        market: config.market,
      });
      setOutcome(result);
    } catch (err) {
      setSubmitError(err instanceof Error ? err.message : "execute failed");
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <div className="flex flex-col gap-3 text-sm">
      <div className="flex items-center gap-2">
        <StatusBadge variant={actionVariant(decision.action)}>{decision.action}</StatusBadge>
        <span className="text-slate-600">
          size {decision.sizeFraction} · confidence {decision.confidence.toFixed(2)}
        </span>
      </div>
      <p className="text-slate-800">{decision.rationale}</p>
      {decision.keyRisks.length > 0 && (
        <ul className="list-disc pl-5 text-xs text-slate-600">
          {decision.keyRisks.map((risk, index) => (
            <li key={index}>{risk}</li>
          ))}
        </ul>
      )}

      <div className="flex items-center gap-3">
        <button
          type="button"
          onClick={() => void submit()}
          disabled={!submittable || submitting}
          className="rounded bg-emerald-600 px-3 py-1.5 text-sm font-medium text-white hover:bg-emerald-500 disabled:opacity-50"
        >
          {submitting ? "Submitting…" : "Submit to execution"}
        </button>
        {!submittable && <span className="text-xs text-slate-500">Hold — nothing to submit.</span>}
      </div>

      {submitError && <p className="text-sm text-red-600">Execute failed: {submitError}</p>}
      {outcome && (
        <div className="rounded border border-slate-200 bg-slate-50 p-2 text-xs">
          <div>
            verdict: <span className="font-medium">{outcome.verdict}</span> · placed:{" "}
            <span className="font-medium">{outcome.placed ? "yes" : "no"}</span>
          </div>
          <div className="text-slate-600">{outcome.reason}</div>
          {outcome.result && (
            <div className="text-slate-600">
              {outcome.result.status} {outcome.result.filledQuantity} @ {outcome.result.averagePrice}
            </div>
          )}
        </div>
      )}
    </div>
  );
}
