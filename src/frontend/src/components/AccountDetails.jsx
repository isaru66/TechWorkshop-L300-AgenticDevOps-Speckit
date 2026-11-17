import { formatMoney } from '../api/money.js';

export default function AccountDetails({ customer, account, loading, error }) {
  if (loading) return <section className="panel"><h2>Account details</h2><p role="status">Loading balance...</p></section>;
  if (error) return <section className="panel"><h2>Account details</h2><p className="error" role="alert">Balance refresh failed: {error}</p></section>;
  if (!account) return <section className="panel"><h2>Account details</h2><p>Select an account to see its balance and history.</p></section>;
  return (
    <section className="panel balance-panel" aria-labelledby="balance-heading">
      <p className="caption">Available balance</p>
      <h2 id="balance-heading">{formatMoney(account.balance)}</h2>
      <p>{customer?.englishName || customer?.name}</p>
      <dl><dt>Account UUID</dt><dd className="identifier">{account.id}</dd><dt>Currency</dt><dd>{account.currency}</dd></dl>
    </section>
  );
}
