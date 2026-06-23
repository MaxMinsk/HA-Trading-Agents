import { describe, it, expect, beforeEach } from "vitest";
import { useRunStore } from "@/features/run/store";

describe("useRunStore", () => {
  beforeEach(() => {
    useRunStore.getState().reset();
  });

  it("startRun clears messages and marks running", () => {
    useRunStore.getState().appendMessage({ role: "analyst", content: "old" });
    useRunStore.getState().startRun();

    const state = useRunStore.getState();
    expect(state.status).toBe("running");
    expect(state.messages).toHaveLength(0);
    expect(state.decision).toBeNull();
  });

  it("appendMessage accumulates messages", () => {
    useRunStore.getState().startRun();
    useRunStore.getState().appendMessage({ role: "analyst", content: "a" });
    useRunStore.getState().appendMessage({ role: "bull", content: "b" });

    expect(useRunStore.getState().messages).toHaveLength(2);
  });

  it("setDecision marks done", () => {
    useRunStore.getState().setDecision({ action: "Buy", sizeFraction: 0.2, confidence: 0.7, rationale: "x", keyRisks: [] });

    const state = useRunStore.getState();
    expect(state.status).toBe("done");
    expect(state.decision?.action).toBe("Buy");
  });

  it("setError marks error", () => {
    useRunStore.getState().setError("boom");

    const state = useRunStore.getState();
    expect(state.status).toBe("error");
    expect(state.error).toBe("boom");
  });
});
