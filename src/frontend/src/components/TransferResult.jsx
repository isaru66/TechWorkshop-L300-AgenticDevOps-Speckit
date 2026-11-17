import { formatMoney } from '../api/money.js';

export default function TransferResult({ result, onViewAccount }) {
  return (
    <section className="panel" aria-live="polite">
      <h2>Transfer completed</h2>
      <p className="identifier">Transfer ID: {result.transfer.id}</p>
      <p>Source balance: {formatMoney(result.sourceBalance.balance)}</p>
      <button type="button" className="secondary" onClick={() => onViewAccount(result.sourceBalance.accountId)}>View source history</button>
      <p>Destination balance: {formatMoney(result.destinationBalance.balance)}</p>
      <button type="button" className="secondary" onClick={() => onViewAccount(result.destinationBalance.accountId)}>View destination history</button>
    </section>
  );
}
