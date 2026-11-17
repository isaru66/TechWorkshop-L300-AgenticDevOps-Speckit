import { act, cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, test, vi } from 'vitest';
import MoneyForms from '../components/MoneyForms.jsx';
import App from '../App.jsx';

afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

test('validates exact decimal amounts before invoking a money operation', () => {
  const submit = vi.fn();
  render(<MoneyForms pending={false} onSubmit={submit} />);
  const input = screen.getByLabelText('Deposit amount');
  for (const invalid of ['0', '-1', '1.001']) {
    fireEvent.change(input, { target: { value: invalid } });
    fireEvent.click(screen.getByRole('button', { name: 'Deposit' }));
    expect(screen.getByRole('alert')).toHaveTextContent(/positive/i);
    expect(submit).not.toHaveBeenCalled();
  }
  fireEvent.change(input, { target: { value: '100.01' } });
  fireEvent.click(screen.getByRole('button', { name: 'Deposit' }));
  expect(submit).toHaveBeenCalledOnce();
  expect(submit).toHaveBeenCalledWith('deposit', '100.01');
});

test('disables both money operations while a mutation is pending', () => {
  render(<MoneyForms pending onSubmit={vi.fn()} />);
  expect(screen.getByRole('button', { name: 'Deposit' })).toBeDisabled();
  expect(screen.getByRole('button', { name: 'Withdraw' })).toBeDisabled();
});

test('retains the completed result when a refresh fails and never submits twice while pending', async () => {
  let resolveMutation;
  const pendingRequest = new Promise((resolve) => { resolveMutation = resolve; });
  let refreshFails = false;
  const account = { id: 'account', customerId: 'customer', currency: 'THB', balance: '10.00' };
  const page = (items) => ({ items, page: 1, pageSize: 50, totalCount: items.length });
  const fetchMock = vi.fn(async (url, options) => {
    if (options?.method === 'POST') return pendingRequest;
    if (refreshFails && url === '/api/accounts/account') throw new Error('Refresh failed');
    return { ok: true, json: async () => url.startsWith('/api/customers?')
      ? page([{ id: 'customer', name: 'Customer' }]) : url.includes('/customers/customer/accounts') || url.startsWith('/api/accounts?')
        ? page([account]) : url.includes('/transactions') ? page([]) : account };
  });
  vi.stubGlobal('fetch', fetchMock);
  render(<App />);
  fireEvent.click(await screen.findByRole('button', { name: /Customer/ }));
  fireEvent.click(await screen.findByRole('button', { name: /account/ }));
  fireEvent.change(screen.getByLabelText('Deposit amount'), { target: { value: '1.00' } });
  fireEvent.click(screen.getByRole('button', { name: 'Deposit' }));
  expect(screen.getByRole('button', { name: 'Withdraw' })).toBeDisabled();
  fireEvent.click(screen.getByRole('button', { name: 'Deposit' }));
  expect(fetchMock.mock.calls.filter(([, options]) => options?.method === 'POST')).toHaveLength(1);
  refreshFails = true;
  await act(async () => resolveMutation({ ok: true, json: async () => ({ balance: '11.00', transaction: { id: 'tx-1' } }) }));
  expect(await screen.findByText(/Completed deposit: THB 11.00/)).toBeInTheDocument();
  expect(await screen.findByRole('alert')).toHaveTextContent('Refresh failed');
});

test('shows an unknown outcome without retrying a failed network submission', async () => {
  const account = { id: 'account', customerId: 'customer', currency: 'THB', balance: '10.00' };
  const page = (items) => ({ items, page: 1, pageSize: 50, totalCount: items.length });
  const fetchMock = vi.fn(async (url, options) => {
    if (options?.method === 'POST') throw new Error('Disconnected');
    return { ok: true, json: async () => url.startsWith('/api/customers?')
      ? page([{ id: 'customer', name: 'Customer' }]) : url.includes('/customers/customer/accounts') || url.startsWith('/api/accounts?')
        ? page([account]) : url.includes('/transactions') ? page([]) : account };
  });
  vi.stubGlobal('fetch', fetchMock);
  render(<App />);
  fireEvent.click(await screen.findByRole('button', { name: /Customer/ }));
  fireEvent.click(await screen.findByRole('button', { name: /account/ }));
  fireEvent.change(screen.getByLabelText('Withdrawal amount'), { target: { value: '1.00' } });
  fireEvent.click(screen.getByRole('button', { name: 'Withdraw' }));
  expect(await screen.findByText(/Completion is unknown/)).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Refresh account' }));
  await waitFor(() => expect(fetchMock.mock.calls.filter(([, options]) => options?.method === 'POST')).toHaveLength(1));
});

test('displays the rejection code and reloads the account after a declined withdrawal', async () => {
  const account = { id: 'account', customerId: 'customer', currency: 'THB', balance: '10.00' };
  const page = (items) => ({ items, page: 1, pageSize: 50, totalCount: items.length });
  const fetchMock = vi.fn(async (url, options) => {
    if (options?.method === 'POST') return { ok: false, status: 409, json: async () => ({ code: 'insufficient_funds', detail: 'Insufficient funds.' }) };
    return { ok: true, json: async () => url.startsWith('/api/customers?')
      ? page([{ id: 'customer', name: 'Customer' }]) : url.includes('/customers/customer/accounts') || url.startsWith('/api/accounts?')
        ? page([account]) : url.includes('/transactions') ? page([]) : account };
  });
  vi.stubGlobal('fetch', fetchMock);
  render(<App />);
  fireEvent.click(await screen.findByRole('button', { name: /Customer/ }));
  fireEvent.click(await screen.findByRole('button', { name: /account/ }));
  await screen.findByText('No transactions for this account.');
  fireEvent.change(screen.getByLabelText('Withdrawal amount'), { target: { value: '11.00' } });
  fireEvent.click(screen.getByRole('button', { name: 'Withdraw' }));
  expect(await screen.findByText(/insufficient_funds/)).toBeInTheDocument();
  await waitFor(() => expect(fetchMock.mock.calls.filter(([url]) => url === '/api/accounts/account')).toHaveLength(2));
  expect(screen.getByRole('heading', { name: 'THB 10.00' })).toBeInTheDocument();
});
