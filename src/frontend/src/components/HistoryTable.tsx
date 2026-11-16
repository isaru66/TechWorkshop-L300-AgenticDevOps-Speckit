import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { api, errorMessage } from '../api/client';
import Pager from './Pager';

const LABELS = {
  DEPOSIT: 'Deposit',
  WITHDRAWAL: 'Withdrawal',
  TRANSFER_IN: 'Transfer in',
  TRANSFER_OUT: 'Transfer out',
} as const;

export default function HistoryTable({ accountId, size = 20 }: { accountId: string; size?: number }) {
  const [page, setPage] = useState(0);
  const { data, error, isLoading } = useQuery({
    queryKey: ['transactions', accountId, page, size],
    queryFn: () => api.listTransactions(accountId, page, size),
  });

  if (isLoading) return <p>Loading…</p>;
  if (error) return <div className="alert error" role="alert">{errorMessage(error)}</div>;
  if (!data || data.items.length === 0) return <p className="empty">No transactions yet.</p>;

  return (
    <>
      <table aria-label="Transaction history">
        <thead>
          <tr>
            <th>Date</th><th>Type</th><th className="num">Amount</th><th className="num">Balance after</th>
            <th>Counterparty</th>
          </tr>
        </thead>
        <tbody>
          {data.items.map((t) => (
            <tr key={t.id}>
              <td>{new Date(t.completedAt).toLocaleString()}</td>
              <td>{LABELS[t.type] ?? t.type}</td>
              <td className="num">{t.amount}</td>
              <td className="num">{t.resultingBalance}</td>
              <td className="mono">{t.counterpartyAccountId ?? ''}</td>
            </tr>
          ))}
        </tbody>
      </table>
      <Pager page={data.page} totalPages={data.totalPages} onPage={setPage} />
    </>
  );
}
