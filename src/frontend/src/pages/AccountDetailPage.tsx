import { FormEvent, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, errorMessage, isValidMoney } from '../api/client';
import HistoryTable from '../components/HistoryTable';

function AmountForm({ label, onSubmit }: { label: string; onSubmit: (amount: string) => Promise<unknown> }) {
  const [amount, setAmount] = useState('');
  const [error, setError] = useState('');
  const [done, setDone] = useState('');
  const [busy, setBusy] = useState(false);
  const id = `amount-${label}`;

  async function submit(e: FormEvent) {
    e.preventDefault();
    setDone('');
    if (!isValidMoney(amount.trim())) {
      setError('Enter a positive amount with at most 2 decimals.');
      return;
    }
    setError('');
    setBusy(true);
    try {
      await onSubmit(amount.trim());
      setAmount('');
      setDone(`${label} recorded.`);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form className="panel" onSubmit={submit} aria-label={label} noValidate>
      <h3>{label}</h3>
      <div className="field">
        <label htmlFor={id}>{label} amount (THB)</label>
        <input id={id} inputMode="decimal" value={amount} onChange={(e) => setAmount(e.target.value)} />
      </div>
      {error && <div className="alert error" role="alert">{error}</div>}
      {done && <div className="alert success" role="status">{done}</div>}
      <button className="btn" type="submit" disabled={busy}>{label}</button>
    </form>
  );
}

export default function AccountDetailPage() {
  const { accountId = '' } = useParams();
  const qc = useQueryClient();
  const account = useQuery({ queryKey: ['account', accountId], queryFn: () => api.getAccount(accountId) });

  const refresh = () =>
    Promise.all([
      qc.invalidateQueries({ queryKey: ['account', accountId] }),
      qc.invalidateQueries({ queryKey: ['transactions', accountId] }),
      qc.invalidateQueries({ queryKey: ['accounts'] }),
      qc.invalidateQueries({ queryKey: ['customer-accounts'] }),
    ]);

  const deposit = useMutation({ mutationFn: (a: string) => api.deposit(accountId, a), onSuccess: refresh });
  const withdraw = useMutation({ mutationFn: (a: string) => api.withdraw(accountId, a), onSuccess: refresh });

  if (account.isLoading) return <p>Loading…</p>;
  if (account.error) return <div className="alert error" role="alert">{errorMessage(account.error)}</div>;
  const a = account.data!;

  return (
    <section>
      <p><Link to={`/customers/${a.customerId}`}>← Customer</Link></p>
      <h1>Account</h1>
      <p className="mono">{a.id}</p>
      <div className="panel">
        <div>Balance</div>
        <div className="balance" aria-label="Balance">{a.balance} {a.currency}</div>
      </div>

      <div className="row-actions">
        <AmountForm label="Deposit" onSubmit={(amt) => deposit.mutateAsync(amt)} />
        <AmountForm label="Withdraw" onSubmit={(amt) => withdraw.mutateAsync(amt)} />
      </div>

      <h2>History</h2>
      <HistoryTable accountId={a.id} />
    </section>
  );
}
