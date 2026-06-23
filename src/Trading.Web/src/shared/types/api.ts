// Hand-written TypeScript mirrors of the Trading.Api wire shapes (camelCase, matching the backend's
// default System.Text.Json serialization). No codegen.

export interface ApiConfig {
  llmConfigured: boolean;
  provider: string | null;
  model: string | null;
  mcpUrl: string;
  mcpBearerSet: boolean;
}

export interface CrewMessage {
  role: string;
  content: string;
}

export interface Decision {
  action: string;
  sizeFraction: number;
  confidence: number;
  rationale: string;
  keyRisks: string[];
}

export interface Balance {
  asset: string;
  free: number;
  locked: number;
}

export interface AccountSnapshot {
  balances: Balance[];
}

export interface ExecutionResult {
  clientOrderId: string;
  status: string;
  filledQuantity: number;
  averagePrice: number;
  fee: number;
  note: string | null;
}

export interface ExecutionOutcome {
  symbol: string;
  action: string;
  verdict: string;
  approvedFraction: number;
  reason: string;
  placed: boolean;
  result: ExecutionResult | null;
}

export interface RunRequest {
  symbol: string;
  interval: string;
  market: string;
  provider?: string;
  model?: string;
}

export interface ExecuteRequest {
  symbol: string;
  action: string;
  sizeFraction: number;
  interval: string;
  market: string;
}
