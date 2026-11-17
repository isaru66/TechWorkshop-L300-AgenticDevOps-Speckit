import { useEffect, useRef, useState } from 'react';
import { bankingClient } from './api/bankingClient.js';
import CustomerBrowser from './components/CustomerBrowser.jsx';
import AccountBrowser from './components/AccountBrowser.jsx';
import AccountDetails from './components/AccountDetails.jsx';
import HistoryTable from './components/HistoryTable.jsx';
import MoneyForms from './components/MoneyForms.jsx';
import TransferForm from './components/TransferForm.jsx';
import TransferResult from './components/TransferResult.jsx';
import { formatMoney } from './api/money.js';

const emptyPage = () => ({ items: [], page: 1, pageSize: 50, totalCount: 0 });

export default function App() {
  const [customerPage, setCustomerPage] = useState(1);
  const [customers, setCustomers] = useState(emptyPage);
  const [customerLoading, setCustomerLoading] = useState(true);
  const [customerError, setCustomerError] = useState('');
  const [customer, setCustomer] = useState(null);
  const [accountPage, setAccountPage] = useState(1);
  const [accounts, setAccounts] = useState(emptyPage);
  const [accountLoading, setAccountLoading] = useState(false);
  const [accountError, setAccountError] = useState('');
  const [accountId, setAccountId] = useState(null);
  const [account, setAccount] = useState(null);
  const [historyPage, setHistoryPage] = useState(1);
  const [history, setHistory] = useState(emptyPage);
  const [historyLoading, setHistoryLoading] = useState(false);
  const [historyError, setHistoryError] = useState('');
  const [refreshVersion, setRefreshVersion] = useState(0);
  const [pending, setPending] = useState(false);
  const [outcome, setOutcome] = useState(null);
  const [transferResult, setTransferResult] = useState(null);
  const [destinationPage, setDestinationPage] = useState(1);
  const [destinations, setDestinations] = useState(emptyPage);
  const [destinationError, setDestinationError] = useState('');
  const mutationInFlight = useRef(false);
  const selection = useRef({ customer: null, accountId: null });
  const navigationVersion = useRef(0);

  useEffect(() => {
    const controller = new AbortController();
    bankingClient.customers(customerPage, controller.signal).then((page) => {
      if (controller.signal.aborted) return;
      setCustomers(page); setCustomerLoading(false); setCustomerError('');
    }).catch((error) => {
      if (!controller.signal.aborted && error.name !== 'AbortError') { setCustomerLoading(false); setCustomerError(error.message); }
    });
    return () => controller.abort();
  }, [customerPage]);

  useEffect(() => {
    if (!customer) return undefined;
    const controller = new AbortController();
    bankingClient.customerAccounts(customer.id, accountPage, controller.signal).then((page) => {
      if (controller.signal.aborted) return;
      setAccounts(page); setAccountLoading(false); setAccountError('');
    }).catch((error) => {
      if (!controller.signal.aborted && error.name !== 'AbortError') { setAccountLoading(false); setAccountError(error.message); }
    });
    return () => controller.abort();
  }, [customer, accountPage, refreshVersion]);

  useEffect(() => {
    if (!accountId) return undefined;
    const controller = new AbortController();
    Promise.all([bankingClient.account(accountId, controller.signal), bankingClient.history(accountId, historyPage, controller.signal)])
      .then(([details, entries]) => {
        if (!controller.signal.aborted) { setAccount(details); setHistory(entries); setHistoryLoading(false); setHistoryError(''); }
      })
      .catch((error) => {
        if (!controller.signal.aborted && error.name !== 'AbortError') { setHistoryLoading(false); setHistoryError(error.message); }
      });
    return () => controller.abort();
  }, [accountId, historyPage, refreshVersion]);

  useEffect(() => {
    if (!accountId) return undefined;
    const controller = new AbortController();
    bankingClient.accounts(destinationPage, controller.signal).then((page) => {
      if (!controller.signal.aborted) { setDestinations(page); setDestinationError(''); }
    }).catch((error) => {
      if (!controller.signal.aborted && error.name !== 'AbortError') setDestinationError(error.message);
    });
    return () => controller.abort();
  }, [accountId, destinationPage, refreshVersion]);

  function refresh() {
    if (selection.current.accountId) setHistoryLoading(true);
    if (selection.current.customer) setAccountLoading(true);
    setRefreshVersion((value) => value + 1);
  }

  async function submitMoney(type, amount) {
    if (mutationInFlight.current || !accountId) return;
    mutationInFlight.current = true;
    setPending(true);
    setOutcome(null);
    try {
      const result = await bankingClient.money(accountId, type, amount);
      setOutcome({ message: `Completed ${type}: ${formatMoney(result.balance)}. Transaction ${result.transaction.id}. Account ${result.accountId ?? accountId}.` });
    } catch (error) {
      setOutcome({ error: true, message: error.status
        ? `${error.code}: ${error.message}`
        : 'Completion is unknown. Refresh and reconcile before submitting again.' });
    } finally {
      mutationInFlight.current = false;
      setPending(false);
      refresh();
    }
  }

  async function submitTransfer(destinationId, amount) {
    if (mutationInFlight.current || !accountId) return;
    mutationInFlight.current = true;
    setPending(true);
    setOutcome(null);
    setTransferResult(null);
    try {
      const result = await bankingClient.transfer(accountId, destinationId, amount);
      setTransferResult(result);
    } catch (error) {
      setOutcome({ error: true, message: error.status
        ? `${error.code}: ${error.message}`
        : 'Completion is unknown. Refresh and reconcile before submitting again.' });
    } finally {
      mutationInFlight.current = false;
      setPending(false);
      refresh();
    }
  }

  async function viewAccount(id) {
    if (selection.current.accountId === id) { refresh(); return; }
    const navigation = ++navigationVersion.current;
    try {
      const selected = await bankingClient.account(id);
      const owner = await bankingClient.customer(selected.customerId);
      if (navigation !== navigationVersion.current) return;
      selection.current = { customer: owner, accountId: id };
      setCustomer(owner); setAccountPage(1); setAccounts(emptyPage()); setAccountId(id);
      setAccount(null); setHistory(emptyPage()); setHistoryPage(1); setAccountLoading(true); setHistoryLoading(true);
    } catch {
      if (navigation === navigationVersion.current)
        setOutcome({ error: true, message: 'Unable to refresh the selected account history.' });
    }
  }

  function chooseCustomer(value) {
    if (customer?.id === value.id) return;
    navigationVersion.current++;
    selection.current = { customer: value, accountId: null };
    setCustomer(value); setAccountPage(1); setAccounts(emptyPage()); setAccountId(null); setAccount(null);
    setHistory(emptyPage()); setAccountLoading(true); setAccountError('');
  }
  function chooseAccount(value) {
    if (accountId === value.id) return;
    navigationVersion.current++;
    selection.current = { customer, accountId: value.id };
    setAccountId(value.id); setAccount(null); setHistory(emptyPage()); setHistoryPage(1);
    setHistoryLoading(true); setHistoryError('');
  }

  return (
    <>
      <header className="topbar"><div><p className="brand-label">WORKSHOP</p><h1>Fictional Banking Workshop</h1></div><p>Fictional data only</p></header>
      <main className="workspace">
        <div className="page-heading"><h2>Accounts and activity</h2><p>Explore customers, balances, and transaction history.</p></div>
        <div className="dashboard">
          <aside className="sidebar">
            <CustomerBrowser data={customers} loading={customerLoading} error={customerError} selectedId={customer?.id}
              onSelect={chooseCustomer} onPageChange={(value) => { setCustomerLoading(true); setCustomerPage(value); }} />
            <AccountBrowser customer={customer} data={accounts} loading={accountLoading} error={accountError} selectedId={accountId}
              onSelect={chooseAccount} onPageChange={(value) => { setAccountLoading(true); setAccountPage(value); }} />
          </aside>
          <div className="content">
            <AccountDetails customer={customer} account={account} />
            {accountId && <MoneyForms pending={pending} onSubmit={submitMoney} />}
            {accountId && <TransferForm key={accountId} sourceId={accountId} accounts={destinations}
              pending={pending} onSubmit={submitTransfer} onPageChange={setDestinationPage} />}
            {destinationError && <p className="error" role="alert">Destination accounts: {destinationError}</p>}
            {transferResult && <TransferResult result={transferResult} onViewAccount={viewAccount} />}
            {outcome && <section className="panel" aria-live="polite">
              <p className={outcome.error ? 'error' : ''}>{outcome.message}</p>
              <button type="button" className="secondary" onClick={refresh}>Refresh account</button>
            </section>}
            {accountId && <HistoryTable data={history} loading={historyLoading} error={historyError}
              onPageChange={(value) => { setHistoryLoading(true); setHistoryPage(value); }} />}
          </div>
        </div>
      </main>
    </>
  );
}
