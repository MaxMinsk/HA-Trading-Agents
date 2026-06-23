import type { Decision } from "@/shared/types/api";
import type { BadgeVariant } from "@/shared/components/StatusBadge";

/** A decision can be submitted only if it is an actionable (non-hold) order with a positive size. */
export function isSubmittable(decision: Decision | null): boolean {
  return (
    decision !== null &&
    (decision.action === "Buy" || decision.action === "Sell") &&
    decision.sizeFraction > 0
  );
}

/** Badge color for an action label. */
export function actionVariant(action: string): BadgeVariant {
  if (action === "Buy") {
    return "ok";
  }
  if (action === "Sell") {
    return "danger";
  }
  return "info";
}
