import { describe, it, expect } from "vitest";
import { buildSettingsUpdate, formFromDto } from "@/shared/lib/settings";
import type { SettingsDto } from "@/shared/types/api";

const dto: SettingsDto = {
  mcpUrl: "http://host:8080/mcp",
  mcpBearer: { set: true, hint: "…abcd" },
  llmProvider: "anthropic",
  llmModel: "claude-sonnet-4-6",
  llmApiKey: { set: true, hint: "…wxyz" },
  llmConfigured: true,
};

describe("formFromDto", () => {
  it("seeds non-secret fields and blanks secrets", () => {
    const form = formFromDto(dto);
    expect(form.mcpUrl).toBe("http://host:8080/mcp");
    expect(form.llmProvider).toBe("anthropic");
    expect(form.mcpBearer).toBe("");
    expect(form.llmApiKey).toBe("");
  });
});

describe("buildSettingsUpdate", () => {
  it("omits secret fields left blank", () => {
    const update = buildSettingsUpdate(formFromDto(dto));
    expect(update.mcpUrl).toBe("http://host:8080/mcp");
    expect(update.llmProvider).toBe("anthropic");
    expect(update).not.toHaveProperty("mcpBearer");
    expect(update).not.toHaveProperty("llmApiKey");
  });

  it("includes secret fields when the user typed one", () => {
    const form = { ...formFromDto(dto), llmApiKey: "sk-new", mcpBearer: "tok" };
    const update = buildSettingsUpdate(form);
    expect(update.llmApiKey).toBe("sk-new");
    expect(update.mcpBearer).toBe("tok");
  });
});
