import { describe, it, expect, beforeEach } from "vitest";
import { render, screen } from "@testing-library/react";
import { DecisionPanel } from "@/features/run/components/DecisionPanel";
import { useRunStore } from "@/features/run/store";

describe("DecisionPanel", () => {
  beforeEach(() => {
    useRunStore.getState().reset();
  });

  it("shows a placeholder when there is no decision", () => {
    render(<DecisionPanel />);

    expect(screen.getByText("No decision yet.")).toBeInTheDocument();
  });

  it("renders a hold decision with submit disabled", () => {
    useRunStore.getState().setDecision({ action: "Hold", sizeFraction: 0, confidence: 0.5, rationale: "flat", keyRisks: [] });
    render(<DecisionPanel />);

    expect(screen.getByText("Hold")).toBeInTheDocument();
    expect(screen.getByText(/nothing to submit/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /submit/i })).toBeDisabled();
  });
});
