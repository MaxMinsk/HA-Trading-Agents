import { useBalances } from "@/features/account/hooks/useBalances";

export function BalancesPanel() {
  const { data, error, refresh } = useBalances();

  return (
    <div className="flex flex-col gap-2 text-sm">
      <button
        type="button"
        onClick={() => void refresh()}
        className="self-start rounded border border-slate-300 px-2 py-1 text-xs hover:bg-slate-100"
      >
        Refresh
      </button>
      {error && <p className="text-xs text-slate-500">Balances unavailable ({error}). Execution may be disabled.</p>}
      {data && data.balances.length === 0 && <p className="text-xs text-slate-500">No balances.</p>}
      {data && data.balances.length > 0 && (
        <table className="text-xs">
          <thead>
            <tr className="text-left text-slate-500">
              <th className="pr-4">Asset</th>
              <th className="pr-4">Free</th>
              <th>Locked</th>
            </tr>
          </thead>
          <tbody>
            {data.balances.map((balance) => (
              <tr key={balance.asset}>
                <td className="pr-4 font-medium">{balance.asset}</td>
                <td className="pr-4">{balance.free}</td>
                <td>{balance.locked}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
