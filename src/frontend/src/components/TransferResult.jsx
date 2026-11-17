import { useEffect, useState } from 'react';
import { bankingClient } from '../api/bankingClient.js';
import { formatMoney } from '../api/money.js';

export default function TransferResult({ result, refreshVersion, onViewAccount }) {
  const [balances, setBalances] = useState({});
  useEffect(() => {
    const controller = new AbortController();
    for (const [side, id] of [['source', result.sourceId], ['destination', result.destinationId]]) {
      bankingClient.account(id, controller.signal).then((account) => {
        if (!controller.signal.aborted) setBalances((previous) => ({ ...previous, [side]: { value: account.balance } }));
      }).catch(() => {
        if (!controller.signal.aborted) setBalances((previous) => ({ ...previous, [side]: { error: true } }));
      });
    }
    return () => controller.abort();
  }, [result, refreshVersion]);

  return (
    <section className="panel" aria-live="polite">
      <h2>{result.transfer ? 'Transfer completed' : 'Transfer accounts'}</h2>
      {result.transfer && <p className="identifier">Transfer ID: {result.transfer.id}</p>}
      <p>Source balance: {balances.source?.error ? 'Refresh unavailable' : balances.source?.value ? formatMoney(balances.source.value) : 'Loading...'}</p>
      <button type="button" className="secondary" onClick={() => onViewAccount(result.sourceId)}>View source history</button>
      <p>Destination balance: {balances.destination?.error ? 'Refresh unavailable' : balances.destination?.value ? formatMoney(balances.destination.value) : 'Loading...'}</p>
      <button type="button" className="secondary" onClick={() => onViewAccount(result.destinationId)}>View destination history</button>
    </section>
  );
}
