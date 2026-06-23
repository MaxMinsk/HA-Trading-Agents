import type {
  AccountSnapshot,
  ApiConfig,
  CrewMessage,
  Decision,
  ExecuteRequest,
  ExecutionOutcome,
  RunRequest,
} from "@/shared/types/api";
import { parseSseEvent, splitSseChunks } from "@/shared/lib/sse";

/** Error carrying the HTTP status of a failed API call. */
export class ApiError extends Error {
  constructor(
    message: string,
    readonly status: number,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

export interface RunHandlers {
  onMessage?: (message: CrewMessage) => void;
  onDecision?: (decision: Decision) => void;
  onError?: (message: string) => void;
}

export interface TradingApiClient {
  getConfig(): Promise<ApiConfig>;
  getStatus(): Promise<unknown>;
  getBalances(): Promise<AccountSnapshot>;
  execute(request: ExecuteRequest): Promise<ExecutionOutcome>;
  runCrew(request: RunRequest, handlers: RunHandlers, signal?: AbortSignal): Promise<void>;
}

export interface CreateClientOptions {
  baseUrl?: string;
  fetchImpl?: typeof fetch;
}

const jsonHeaders = { "content-type": "application/json" };

export function createTradingApiClient(options: CreateClientOptions = {}): TradingApiClient {
  const baseUrl = options.baseUrl ?? "";
  const fetchImpl: typeof fetch = options.fetchImpl ?? ((...args) => fetch(...args));

  async function request<T>(path: string, init?: RequestInit): Promise<T> {
    const response = await fetchImpl(`${baseUrl}${path}`, init);
    if (!response.ok) {
      throw new ApiError(`${init?.method ?? "GET"} ${path} failed (${response.status})`, response.status);
    }

    return (await response.json()) as T;
  }

  return {
    getConfig: () => request<ApiConfig>("/api/config"),
    getStatus: () => request<unknown>("/api/status"),
    getBalances: () => request<AccountSnapshot>("/api/balances"),
    execute: (req) =>
      request<ExecutionOutcome>("/api/execute", {
        method: "POST",
        headers: jsonHeaders,
        body: JSON.stringify(req),
      }),
    runCrew: async (req, handlers, signal) => {
      const init: RequestInit = { method: "POST", headers: jsonHeaders, body: JSON.stringify(req) };
      if (signal) {
        init.signal = signal;
      }

      const response = await fetchImpl(`${baseUrl}/api/run`, init);
      if (!response.ok || !response.body) {
        throw new ApiError(`run failed (${response.status})`, response.status);
      }

      const reader = response.body.getReader();
      const decoder = new TextDecoder();
      let buffer = "";
      for (;;) {
        const { done, value } = await reader.read();
        if (done) {
          break;
        }

        buffer += decoder.decode(value, { stream: true });
        const { events, rest } = splitSseChunks(buffer);
        buffer = rest;
        for (const block of events) {
          dispatch(block, handlers);
        }
      }

      if (buffer.trim().length > 0) {
        dispatch(buffer, handlers);
      }
    },
  };
}

function dispatch(block: string, handlers: RunHandlers): void {
  const evt = parseSseEvent(block);
  if (!evt) {
    return;
  }

  try {
    if (evt.event === "message") {
      handlers.onMessage?.(JSON.parse(evt.data) as CrewMessage);
    } else if (evt.event === "decision") {
      handlers.onDecision?.(JSON.parse(evt.data) as Decision);
    } else if (evt.event === "error") {
      const parsed = JSON.parse(evt.data) as { message?: string };
      handlers.onError?.(parsed.message ?? "run failed");
    }
  } catch {
    handlers.onError?.("malformed event from server");
  }
}
