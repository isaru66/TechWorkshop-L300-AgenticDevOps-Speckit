import { Link, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api, errorMessage } from '../api/client';

const FIELDS: [string, keyof import('../api/types').Customer][] = [
  ['Name', 'name'],
  ['English name', 'englishName'],
  ['Email', 'email'],
  ['Phone', 'phone'],
  ['Address', 'addressLine'],
  ['Subdistrict', 'subdistrict'],
  ['District', 'district'],
  ['Province', 'province'],
  ['Postcode', 'postcode'],
  ['Date of birth', 'dateOfBirth'],
  ['Occupation', 'occupation'],
  ['National ID', 'nationalId'],
  ['Created', 'createdAt'],
];

export default function CustomerDetailPage() {
  const { customerId = '' } = useParams();
  const qc = useQueryClient();
  const customer = useQuery({ queryKey: ['customer', customerId], queryFn: () => api.getCustomer(customerId) });
  const accounts = useQuery({
    queryKey: ['customer-accounts', customerId],
    queryFn: () => api.listCustomerAccounts(customerId),
  });
  const open = useMutation({
    mutationFn: () => api.openAccount(customerId),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['customer-accounts', customerId] }),
  });

  if (customer.isLoading) return <p>Loading…</p>;
  if (customer.error) return <div className="alert error" role="alert">{errorMessage(customer.error)}</div>;
  const c = customer.data!;

  return (
    <section>
      <p><Link to="/customers">← Customers</Link></p>
      <h1>{c.name}</h1>
      <div className="panel">
        <dl className="form-grid">
          {FIELDS.map(([label, key]) => (
            <div key={key} className="field">
              <dt><strong>{label}</strong></dt>
              <dd style={{ margin: 0 }}>{c[key] ?? '—'}</dd>
            </div>
          ))}
        </dl>
      </div>

      <div className="toolbar">
        <h2>Accounts</h2>
        <button className="btn" type="button" disabled={open.isPending} onClick={() => open.mutate()}>
          Open account
        </button>
      </div>
      {open.error && <div className="alert error" role="alert">{errorMessage(open.error)}</div>}
      {accounts.error && <div className="alert error" role="alert">{errorMessage(accounts.error)}</div>}
      {accounts.data && accounts.data.length === 0 && <p className="empty">No accounts yet.</p>}
      {accounts.data && accounts.data.length > 0 && (
        <table>
          <thead>
            <tr><th>Account</th><th>Currency</th><th className="num">Balance</th></tr>
          </thead>
          <tbody>
            {accounts.data.map((a) => (
              <tr key={a.id}>
                <td><Link className="mono" to={`/accounts/${a.id}`}>{a.id}</Link></td>
                <td>{a.currency}</td>
                <td className="num">{a.balance}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </section>
  );
}
