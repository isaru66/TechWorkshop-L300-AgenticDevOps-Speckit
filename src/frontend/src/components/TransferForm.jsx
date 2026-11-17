import { useState } from 'react';
import { formatMoney, validAmount } from '../api/money.js';
import Pagination from './Pagination.jsx';

export default function TransferForm({ sourceId, accounts, pending, onSubmit, onPageChange }) {
  const [destination, setDestination] = useState(null);
  const [amount, setAmount] = useState('');
  const [error, setError] = useState('');
  const options = accounts.items.filter((account) => account.id !== sourceId);
  if (destination && !options.some((account) => account.id === destination.id) && destination.id !== sourceId)
    options.unshift(destination);

  function submit(event) {
    event.preventDefault();
    if (!destination || destination.id === sourceId || !validAmount(amount)) {
      setError('Select a different destination and enter a positive amount with at most two decimal places.');
      return;
    }
    setError('');
    onSubmit(destination.id, amount);
  }

  return (
    <section className="panel" aria-labelledby="transfer-heading">
      <h2 id="transfer-heading">Transfer fictional funds</h2>
      <p className="identifier">Source account: {sourceId}</p>
      {error && <p className="error" role="alert">{error}</p>}
      <form onSubmit={submit}>
        <label htmlFor="destination-account">Destination account</label>
        <select id="destination-account" value={destination?.id ?? ''} onChange={(event) =>
          setDestination(options.find((account) => account.id === event.target.value) ?? null)}>
          <option value="">Select a destination</option>
          {options.map((account) => <option key={account.id} value={account.id}>{account.id} - {formatMoney(account.balance)}</option>)}
        </select>
        <label htmlFor="transfer-amount">Transfer amount</label>
        <input id="transfer-amount" inputMode="decimal" value={amount} onChange={(event) => setAmount(event.target.value)} />
        <button type="submit" disabled={pending}>Transfer</button>
      </form>
      <Pagination {...accounts} onPageChange={onPageChange} label="Destination account" />
    </section>
  );
}
