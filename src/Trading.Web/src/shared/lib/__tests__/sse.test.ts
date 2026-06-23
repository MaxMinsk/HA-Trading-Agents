import { describe, it, expect } from "vitest";
import { parseSseEvent, splitSseChunks } from "@/shared/lib/sse";

describe("parseSseEvent", () => {
  it("parses an event name and data payload", () => {
    expect(parseSseEvent('event: decision\ndata: {"a":1}')).toEqual({ event: "decision", data: '{"a":1}' });
  });

  it("defaults the event name to message", () => {
    expect(parseSseEvent("data: hi")).toEqual({ event: "message", data: "hi" });
  });

  it("returns null when there is no data line", () => {
    expect(parseSseEvent("event: ping")).toBeNull();
  });
});

describe("splitSseChunks", () => {
  it("returns complete blocks and keeps the trailing remainder", () => {
    const { events, rest } = splitSseChunks("event: a\ndata: 1\n\nevent: b\ndata: 2\n\ndata: par");

    expect(events).toHaveLength(2);
    expect(rest).toBe("data: par");
  });
});
