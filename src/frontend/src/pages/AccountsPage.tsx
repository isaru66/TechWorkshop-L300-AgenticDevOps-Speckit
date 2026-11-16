import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { api, errorMessage } from '../api/client';
import Pager from '../components/Pager';

export default function AccountsPage() {
  const [page, setPage] = useState(0);
  const { data, error, isLoading } = useQuery({
    queryKey: ['accounts', page],
    queryFn: () => api.listAccounts(page),
  });

  return (
    <section>
      <h1>Accounts</h1>
      {isLoading && <p>Loading…</p>}
      {error && <div className="alert error" role="alert">{errorMessage(error)}</div>}
      {data && data.items.length === 0 && <p className="empty">No accounts yet.</p>}
      {data && data.items.length > 0 && (
        <>
          <table>
            <thead>
              <tr><th>Account</th><th>Customer</th><th>Currency</th><th className="num">Balance</th></tr>
            </thead>
            <tbody>
              {data.items.map((a) => (
                <tr key={a.id}>
                  <td><Link className="mono" to={`/accounts/${a.id}`}>{a.id}</Link></td>
                  <td><Link className="mono" to={`/customers/${a.customerId}`}>{a.customerId}</Link></td>
                  <td>{a.currency}</td>
                  <td className="num">{a.balance}</td>
                </tr>
              ))}
            </tbody>
          </table>
          <Pager page={data.page} totalPages={data.totalPages} onPage={setPage} />
        </>
      )}
    </section>
  );
}
