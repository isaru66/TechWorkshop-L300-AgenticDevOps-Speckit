import { useState } from 'react';
import { validAmount } from '../api/money.js';

export default function MoneyForms({ pending, onSubmit }) {
  const [deposit, setDeposit] = useState('');
  const [withdrawal, setWithdrawal] = useState('');
  const [error, setError] = useState('');

  function submit(event, type, value) {
    event.preventDefault();
    if (!validAmount(value)) {
      setError('Enter a positive amount with at most two decimal places.');
      return;
    }
    setError('');
    onSubmit(type, value);
  }

  return (
    <section className="panel" aria-labelledby="money-heading">
      <h2 id="money-heading">Fictional funds</h2>
      {error && <p className="error" role="alert">{error}</p>}
      <form onSubmit={(event) => submit(event, 'deposit', deposit)}>
        <label htmlFor="deposit-amount">Deposit amount</label>
        <input id="deposit-amount" inputMode="decimal" value={deposit} onChange={(event) => setDeposit(event.target.value)} />
        <button type="submit" disabled={pending}>Deposit</button>
      </form>
      <form onSubmit={(event) => submit(event, 'withdrawal', withdrawal)}>
        <label htmlFor="withdrawal-amount">Withdrawal amount</label>
        <input id="withdrawal-amount" inputMode="decimal" value={withdrawal} onChange={(event) => setWithdrawal(event.target.value)} />
        <button type="submit" disabled={pending}>Withdraw</button>
      </form>
    </section>
  );
}
