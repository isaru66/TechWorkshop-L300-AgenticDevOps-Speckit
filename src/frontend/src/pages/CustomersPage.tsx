import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { api, errorMessage } from '../api/client';
import Pager from '../components/Pager';

export default function CustomersPage() {
  const [page, setPage] = useState(0);
  const { data, error, isLoading } = useQuery({
    queryKey: ['customers', page],
    queryFn: () => api.listCustomers(page),
  });

  return (
    <section>
      <div className="toolbar">
        <h1>Customers</h1>
        <Link className="btn" to="/customers/new">New customer</Link>
      </div>
      {isLoading && <p>Loading…</p>}
      {error && <div className="alert error" role="alert">{errorMessage(error)}</div>}
      {data && data.items.length === 0 && <p className="empty">No customers yet.</p>}
      {data && data.items.length > 0 && (
        <>
          <table>
            <thead>
              <tr><th>Name</th><th>Email</th><th>Phone</th><th>Province</th></tr>
            </thead>
            <tbody>
              {data.items.map((c) => (
                <tr key={c.id}>
                  <td><Link to={`/customers/${c.id}`}>{c.name}</Link></td>
                  <td>{c.email}</td>
                  <td>{c.phone}</td>
                  <td>{c.province}</td>
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
