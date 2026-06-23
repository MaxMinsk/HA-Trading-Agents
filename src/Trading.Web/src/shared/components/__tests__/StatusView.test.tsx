import { describe, it, expect } from "vitest";
import { render, screen } from "@testing-library/react";
import { StatusView } from "@/shared/components/StatusView";

describe("StatusView", () => {
  it("shows the model when configured", () => {
    render(
      <StatusView
        config={{ llmConfigured: true, provider: "Anthropic", model: "claude-sonnet-4-6", mcpUrl: "http://x/mcp", mcpBearerSet: true }}
        error={null}
      />,
    );

    expect(screen.getByText(/Anthropic/)).toBeInTheDocument();
    expect(screen.getByText("bearer set")).toBeInTheDocument();
  });

  it("warns when the model is not configured", () => {
    render(
      <StatusView
        config={{ llmConfigured: false, provider: null, model: null, mcpUrl: "http://x/mcp", mcpBearerSet: false }}
        error={null}
      />,
    );

    expect(screen.getByText("not configured")).toBeInTheDocument();
    expect(screen.getByText("no bearer")).toBeInTheDocument();
  });

  it("shows an error", () => {
    render(<StatusView config={null} error="boom" />);

    expect(screen.getByText(/boom/)).toBeInTheDocument();
  });
});
