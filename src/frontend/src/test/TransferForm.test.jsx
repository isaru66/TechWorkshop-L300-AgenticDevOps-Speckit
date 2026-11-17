import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, test, vi } from 'vitest';
import TransferForm from '../components/TransferForm.jsx';
import App from '../App.jsx';

afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

test('requires a distinct destination and an exact positive amount', () => {
  const submit = vi.fn();
  const accounts = { items: [
    { id: 'source', currency: 'THB', balance: '10.00' },
    { id: 'destination', currency: 'THB', balance: '20.00' },
  ], page: 1, pageSize: 50, totalCount: 2 };
  render(<TransferForm sourceId="source" accounts={accounts} pending={false} onSubmit={submit} onPageChange={vi.fn()} />);
  expect(screen.queryByRole('option', { name: /source/ })).not.toBeInTheDocument();
  fireEvent.change(screen.getByLabelText('Destination account'), { target: { value: 'destination' } });
  fireEvent.change(screen.getByLabelText('Transfer amount'), { target: { value: '0.001' } });
  fireEvent.click(screen.getByRole('button', { name: 'Transfer', exact: true }));
  expect(screen.getByRole('alert')).toBeInTheDocument();
  expect(submit).not.toHaveBeenCalled();
  fireEvent.change(screen.getByLabelText('Transfer amount'), { target: { value: '0.01' } });
  fireEvent.click(screen.getByRole('button', { name: 'Transfer', exact: true }));
  expect(submit).toHaveBeenCalledWith('destination', '0.01');
});

test('disables submission while any mutation is pending', () => {
  render(<TransferForm sourceId="source" accounts={{ items: [], page: 1, pageSize: 50, totalCount: 0 }}
    pending onSubmit={vi.fn()} onPageChange={vi.fn()} />);
  expect(screen.getByRole('button', { name: 'Transfer', exact: true })).toBeDisabled();
});

test('shows a completed transfer and can open the other customer account history', async () => {
  let resolveTransfer;
  const pendingRequest = new Promise((resolve) => { resolveTransfer = resolve; });
  const source = { id: 'source', customerId: 'owner1', currency: 'THB', balance: '10.00' };
  const destination = { id: 'destination', customerId: 'owner2', currency: 'THB', balance: '20.00' };
  const page = (items) => ({ items, page: 1, pageSize: 50, totalCount: items.length });
  const fetchMock = vi.fn(async (url, options) => {
    if (options?.method === 'POST') return pendingRequest;
    let data;
    if (url.startsWith('/api/customers?')) data = page([{ id: 'owner1', name: 'Owner one' }]);
    else if (url === '/api/customers/owner2') data = { id: 'owner2', name: 'Owner two' };
    else if (url.startsWith('/api/customers/owner1/accounts')) data = page([source]);
    else if (url.startsWith('/api/customers/owner2/accounts')) data = page([destination]);
    else if (url.startsWith('/api/accounts?')) data = page([source, destination]);
    else if (url.includes('/transactions')) data = page([]);
    else data = url.endsWith('/destination') ? destination : source;
    return { ok: true, json: async () => data };
  });

  vi.stubGlobal('fetch', fetchMock);
  render(<App />);
  fireEvent.click(await screen.findByRole('button', { name: /Owner one/ }));
  fireEvent.click(await screen.findByRole('button', { name: /source/ }));
  await screen.findByRole('option', { name: /destination/ });
  fireEvent.change(screen.getByLabelText('Destination account'), { target: { value: 'destination' } });
  fireEvent.change(screen.getByLabelText('Transfer amount'), { target: { value: '1.00' } });
  fireEvent.click(screen.getByRole('button', { name: 'Transfer', exact: true }));
  expect(screen.getByRole('button', { name: 'Deposit' })).toBeDisabled();
  fireEvent.click(screen.getByRole('button', { name: 'Transfer', exact: true }));
  expect(fetchMock.mock.calls.filter(([, options]) => options?.method === 'POST')).toHaveLength(1);
  await act(async () => resolveTransfer({ ok: true, json: async () => ({
    transfer: { id: 'transfer-id' }, sourceBalance: { accountId: 'source', balance: '9.00' },
    destinationBalance: { accountId: 'destination', balance: '21.00' },
  }) }));
  expect(await screen.findByText('Transfer ID: transfer-id')).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'View destination history' }));
  expect(await screen.findByRole('heading', { name: 'THB 20.00' })).toBeInTheDocument();
});

test.each(['rejected', 'network', 'server'])('refreshes without retry after a %s transfer', async (failure) => {
  const source = { id: 'source', customerId: 'owner1', currency: 'THB', balance: '10.00' };
  const destination = { id: 'destination', customerId: 'owner2', currency: 'THB', balance: '20.00' };
  const page = (items) => ({ items, page: 1, pageSize: 50, totalCount: items.length });
  const fetchMock = vi.fn(async (url, options) => {
    if (options?.method === 'POST') {
      if (failure === 'network') throw new Error('Disconnected');
      if (failure === 'server') return { ok: false, status: 503, json: async () => ({ code: 'completion_unknown' }) };
      return { ok: false, status: 409, json: async () => ({ code: 'insufficient_funds', detail: 'Insufficient funds.' }) };
    }
    return { ok: true, json: async () => url.startsWith('/api/customers?')
      ? page([{ id: 'owner1', name: 'Owner one' }]) : url.startsWith('/api/customers/owner1/accounts')
        ? page([source]) : url.startsWith('/api/customers/owner2/accounts') ? page([destination])
          : url.startsWith('/api/accounts?') ? page([source, destination]) : url.includes('/transactions') ? page([])
            : url === '/api/customers/owner2' ? { id: 'owner2', name: 'Owner two' }
              : url === '/api/customers/owner1' ? { id: 'owner1', name: 'Owner one' }
                : url === '/api/accounts/destination' ? destination : source };
  });
  vi.stubGlobal('fetch', fetchMock);
  render(<App />);
  fireEvent.click(await screen.findByRole('button', { name: /Owner one/ }));
  fireEvent.click(await screen.findByRole('button', { name: /source/ }));
  await screen.findByRole('option', { name: /destination/ });
  fireEvent.change(screen.getByLabelText('Destination account'), { target: { value: 'destination' } });
  fireEvent.change(screen.getByLabelText('Transfer amount'), { target: { value: '11.00' } });
  fireEvent.click(screen.getByRole('button', { name: 'Transfer', exact: true }));
  expect(await screen.findByText(failure === 'rejected' ? /insufficient_funds/ : /Completion is unknown/)).toBeInTheDocument();
  await waitFor(() => expect(fetchMock.mock.calls.filter(([url]) => url.startsWith('/api/accounts?'))).toHaveLength(2));
  expect(fetchMock.mock.calls.filter(([, options]) => options?.method === 'POST')).toHaveLength(1);
  expect(await screen.findByText('Destination balance: THB 20.00')).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'View destination history' }));
  expect(await screen.findByRole('heading', { name: 'THB 20.00' })).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'View source history' }));
  expect(await screen.findByRole('heading', { name: 'THB 10.00' })).toBeInTheDocument();
});

test('does not present a stale balance for a destination selected on another page', () => {
  const firstPage = { items: [{ id: 'destination', balance: '20.00' }], page: 1, pageSize: 1, totalCount: 2 };
  const { rerender } = render(<TransferForm sourceId="source" accounts={firstPage} pending={false} onSubmit={vi.fn()} onPageChange={vi.fn()} />);
  fireEvent.change(screen.getByLabelText('Destination account'), { target: { value: 'destination' } });
  rerender(<TransferForm sourceId="source" accounts={{ ...firstPage, items: [{ id: 'other', balance: '5.00' }], page: 2 }}
    pending={false} onSubmit={vi.fn()} onPageChange={vi.fn()} />);
  expect(screen.getByRole('option', { name: /^destination/ })).not.toHaveTextContent('20.00');
});

test('hides destination balances when their refresh is unavailable', () => {
  const accounts = { items: [{ id: 'destination', balance: '20.00' }], page: 1, pageSize: 50, totalCount: 1 };
  render(<TransferForm sourceId="source" accounts={accounts} pending={false} balancesAvailable={false}
    onSubmit={vi.fn()} onPageChange={vi.fn()} />);
  expect(screen.getByRole('option', { name: /^destination/ })).toHaveTextContent('balance unavailable');
  expect(screen.getByRole('option', { name: /^destination/ })).not.toHaveTextContent('20.00');
});
