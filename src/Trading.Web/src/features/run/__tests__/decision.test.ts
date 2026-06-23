import { describe, it, expect } from "vitest";
import { actionVariant, isSubmittable } from "@/features/run/lib/decision";

describe("isSubmittable", () => {
  it("is true for a buy with positive size", () => {
    expect(isSubmittable({ action: "Buy", sizeFraction: 0.2, confidence: 0.7, rationale: "", keyRisks: [] })).toBe(true);
  });

  it("is false for hold", () => {
    expect(isSubmittable({ action: "Hold", sizeFraction: 0, confidence: 0.5, rationale: "", keyRisks: [] })).toBe(false);
  });

  it("is false for zero size", () => {
    expect(isSubmittable({ action: "Buy", sizeFraction: 0, confidence: 0.7, rationale: "", keyRisks: [] })).toBe(false);
  });

  it("is false for null", () => {
    expect(isSubmittable(null)).toBe(false);
  });
});

describe("actionVariant", () => {
  it("maps actions to badge variants", () => {
    expect(actionVariant("Buy")).toBe("ok");
    expect(actionVariant("Sell")).toBe("danger");
    expect(actionVariant("Hold")).toBe("info");
  });
});
