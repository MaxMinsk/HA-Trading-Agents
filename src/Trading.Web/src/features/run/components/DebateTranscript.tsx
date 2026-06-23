import { useRunStore } from "@/features/run/store";

const roleLabels: Record<string, string> = {
  analyst: "Analyst",
  bull: "Bull",
  bear: "Bear",
  trader: "Trader",
  "risk-reviewer": "Risk reviewer",
};

export function DebateTranscript() {
  const messages = useRunStore((state) => state.messages);
  const status = useRunStore((state) => state.status);
  const error = useRunStore((state) => state.error);

  if (status === "idle") {
    return <p className="text-sm text-slate-500">Configure a run and start the crew to see the debate.</p>;
  }

  return (
    <div className="flex flex-col gap-3">
      {messages.map((message, index) => (
        <article key={`${message.role}-${index}`} className="rounded border border-slate-200 bg-white p-3">
          <h3 className="mb-1 text-xs font-semibold uppercase tracking-wide text-slate-500">
            {roleLabels[message.role] ?? message.role}
          </h3>
          <p className="whitespace-pre-wrap text-sm text-slate-800">{message.content}</p>
        </article>
      ))}
      {status === "running" && <p className="text-sm text-slate-500">Thinking…</p>}
      {error && <p className="text-sm text-red-600">Error: {error}</p>}
    </div>
  );
}
