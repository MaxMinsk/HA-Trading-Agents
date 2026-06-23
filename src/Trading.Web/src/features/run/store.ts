import { create } from "zustand";
import type { CrewMessage, Decision } from "@/shared/types/api";

export interface RunConfig {
  symbol: string;
  interval: string;
  market: string;
  provider: string;
  model: string;
}

export type RunStatus = "idle" | "running" | "done" | "error";

interface RunState {
  config: RunConfig;
  status: RunStatus;
  messages: CrewMessage[];
  decision: Decision | null;
  error: string | null;
  setConfig: (patch: Partial<RunConfig>) => void;
  startRun: () => void;
  appendMessage: (message: CrewMessage) => void;
  setDecision: (decision: Decision) => void;
  setError: (error: string) => void;
  reset: () => void;
}

const defaultConfig: RunConfig = {
  symbol: "BTCUSDT",
  interval: "1h",
  market: "spot",
  provider: "",
  model: "",
};

export const useRunStore = create<RunState>((set) => ({
  config: defaultConfig,
  status: "idle",
  messages: [],
  decision: null,
  error: null,
  setConfig: (patch) => set((state) => ({ config: { ...state.config, ...patch } })),
  startRun: () => set({ status: "running", messages: [], decision: null, error: null }),
  appendMessage: (message) => set((state) => ({ messages: [...state.messages, message] })),
  setDecision: (decision) => set({ decision, status: "done" }),
  setError: (error) => set({ error, status: "error" }),
  reset: () => set({ status: "idle", messages: [], decision: null, error: null }),
}));
