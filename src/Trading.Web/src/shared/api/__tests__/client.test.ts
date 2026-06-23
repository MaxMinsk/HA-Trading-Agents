import { describe, it, expect, vi } from "vitest";
import { ApiError, createTradingApiClient } from "@/shared/api/client";

function streamResponse(body: string): Response {
  const stream = new ReadableStream<Uint8Array>({
    start(controller) {
      controller.enqueue(new TextEncoder().encode(body));
      controller.close();
    },
  });
  return new Response(stream, { status: 200, headers: { "content-type": "text/event-stream" } });
}

describe("createTradingApiClient.runCrew", () => {
  it("dispatches message and decision events from the SSE stream", async () => {
    const body =
      'event: message\ndata: {"role":"analyst","content":"up"}\n\n' +
      'event: decision\ndata: {"action":"Buy","sizeFraction":0.2,"confidence":0.7,"rationale":"x","keyRisks":[]}\n\n';
    const fetchImpl = vi.fn().mockResolvedValue(streamResponse(body));
    const client = createTradingApiClient({ fetchImpl: fetchImpl as unknown as typeof fetch });

    const roles: string[] = [];
    let action = "";
    await client.runCrew(
      { symbol: "BTCUSDT", interval: "1h", market: "spot" },
      { onMessage: (m) => roles.push(m.role), onDecision: (d) => (action = d.action) },
    );

    expect(roles).toEqual(["analyst"]);
    expect(action).toBe("Buy");
  });
});

describe("createTradingApiClient request errors", () => {
  it("throws ApiError on a non-ok response", async () => {
    const fetchImpl = vi.fn().mockResolvedValue(new Response("nope", { status: 500 }));
    const client = createTradingApiClient({ fetchImpl: fetchImpl as unknown as typeof fetch });

    await expect(client.getBalances()).rejects.toBeInstanceOf(ApiError);
  });
});
