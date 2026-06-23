// Minimal Server-Sent Events parsing. /api/run is a POST stream (EventSource only supports GET), so
// the client reads the body itself and uses these pure helpers, which keeps them unit-testable.

export interface SseEvent {
  event: string;
  data: string;
}

/** Parses one `\n\n`-delimited SSE block into an event (or null if it carries no data). */
export function parseSseEvent(block: string): SseEvent | null {
  let event = "message";
  const dataLines: string[] = [];
  for (const line of block.split("\n")) {
    if (line.startsWith("event:")) {
      event = line.slice("event:".length).trim();
    } else if (line.startsWith("data:")) {
      dataLines.push(line.slice("data:".length).trim());
    }
  }

  if (dataLines.length === 0) {
    return null;
  }

  return { event, data: dataLines.join("\n") };
}

/** Splits a buffer into complete event blocks plus the trailing (incomplete) remainder. */
export function splitSseChunks(buffer: string): { events: string[]; rest: string } {
  const parts = buffer.split("\n\n");
  const rest = parts.pop() ?? "";
  return { events: parts.filter((p) => p.trim().length > 0), rest };
}
