import type { ReactNode } from "react";
import { SettingsPanel } from "@/shared/components/SettingsPanel";
import { RunConfigForm } from "@/features/run/components/RunConfigForm";
import { DebateTranscript } from "@/features/run/components/DebateTranscript";
import { DecisionPanel } from "@/features/run/components/DecisionPanel";
import { BalancesPanel } from "@/features/account/components/BalancesPanel";

function Card({ title, children }: { title?: string; children: ReactNode }) {
  return (
    <div className="rounded border border-slate-200 bg-white p-4">
      {title && <h2 className="mb-3 text-sm font-semibold text-slate-700">{title}</h2>}
      {children}
    </div>
  );
}

export function App() {
  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <header className="border-b border-slate-200 bg-white px-6 py-3">
        <h1 className="text-lg font-semibold">Trading Agent</h1>
        <p className="text-xs text-slate-500">
          MAF crew over MCP — every decision passes the deterministic risk gate before execution.
        </p>
      </header>
      <main className="mx-auto grid max-w-6xl gap-6 p-6 lg:grid-cols-2">
        <section className="flex flex-col gap-6">
          <Card title="Settings">
            <SettingsPanel />
          </Card>
          <Card>
            <RunConfigForm />
          </Card>
          <Card title="Decision">
            <DecisionPanel />
          </Card>
          <Card title="Balances">
            <BalancesPanel />
          </Card>
        </section>
        <Card title="Debate">
          <DebateTranscript />
        </Card>
      </main>
    </div>
  );
}
