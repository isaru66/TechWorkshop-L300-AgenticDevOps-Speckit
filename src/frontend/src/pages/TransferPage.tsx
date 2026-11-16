import { FormEvent, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, errorMessage, isValidMoney } from '../api/client';

export default function TransferPage() {
  const qc = useQueryClient();
  const [source, setSource] = useState('');
  const [dest, setDest] = useState('');
  const [amount, setAmount] = useState('');
  const [localError, setLocalError] = useState('');
  const accounts = useQuery({ queryKey: ['accounts', 'picker'], queryFn: () => api.listAccounts(0, 100) });

  const transfer = useMutation({
    mutationFn: async () => {
      const t = await api.transfer({
        sourceAccountId: source.trim(),
        destinationAccountId: dest.trim(),
        amount: amount.trim(),
      });
      const [s, d] = await Promise.all([api.getBalance(t.sourceAccountId), api.getBalance(t.destinationAccountId)]);
      return { transfer: t, s, d };
    },
    onSuccess: () =>
      Promise.all(
        ['accounts', 'account', 'transactions', 'customer-accounts'].map((k) =>
          qc.invalidateQueries({ queryKey: [k] }),
        ),
      ),
  });

  function submit(e: FormEvent) {
    e.preventDefault();
    if (!source.trim() || !dest.trim()) {
      setLocalError('Choose or enter both a source and a destination account.');
      return;
    }
    if (!isValidMoney(amount.trim())) {
      setLocalError('Enter a positive amount with at most 2 decimals.');
      return;
    }
    setLocalError('');
    transfer.mutate();
  }

  const result = transfer.data;
  const options = accounts.data?.items ?? [];

  return (
    <section>
      <h1>Transfer</h1>
      <form className="panel" onSubmit={submit} noValidate>
        <div className="form-grid">
          <div className="field">
            <label htmlFor="source">Source account</label>
            <input id="source" list="account-options" value={source} onChange={(e) => setSource(e.target.value)} />
          </div>
          <div className="field">
            <label htmlFor="dest">Destination account</label>
            <input id="dest" list="account-options" value={dest} onChange={(e) => setDest(e.target.value)} />
          </div>
          <div className="field">
            <label htmlFor="amount">Amount (THB)</label>
            <input id="amount" inputMode="decimal" value={amount} onChange={(e) => setAmount(e.target.value)} />
          </div>
        </div>
        <datalist id="account-options">
          {options.map((a) => (
            <option key={a.id} value={a.id}>{`${a.id} (${a.balance} ${a.currency})`}</option>
          ))}
        </datalist>
        {localError && <div className="alert error" role="alert">{localError}</div>}
        {transfer.error && <div className="alert error" role="alert">{errorMessage(transfer.error)}</div>}
        <button className="btn" type="submit" disabled={transfer.isPending}>Transfer</button>
      </form>

      {transfer.isSuccess && (
        <div className="alert success" role="status">
          Transfer completed: {result?.transfer.amount} THB moved.
          {result && (
            <ul>
              <li>Source balance: {result.s.balance}</li>
              <li>Destination balance: {result.d.balance}</li>
            </ul>
          )}
        </div>
      )}
    </section>
  );
}
