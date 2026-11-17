import { afterEach, expect, test, vi } from 'vitest';
import { act, cleanup, fireEvent, render, screen, within } from '@testing-library/react';
import App from '../App.jsx';

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

test('selects an account and shows exact balances and recorded history', async () => {
  const account = { id: '00209a31-c483-41bb-a9fd-35530f8f680b', customerId: 'customer-1', currency: 'THB', balance: '52608.86' };
  vi.stubGlobal('fetch', vi.fn(async (url) => {
    let data;
    if (url.startsWith('/api/customers?')) data = { items: [{ id: 'customer-1', name: 'กานต์', englishName: 'Kan' }], page: 1, pageSize: 50, totalCount: 1 };
    else if (url.startsWith('/api/customers/customer-1/accounts')) data = { items: [account], page: 1, pageSize: 50, totalCount: 1 };
    else if (url.startsWith('/api/accounts?')) data = { items: [account], page: 1, pageSize: 50, totalCount: 1 };
    else if (url.includes('/transactions')) data = { items: [{ id: 'entry-1', sequence: '1002', type: 'DEPOSIT', amount: '100.00', completedAt: '2026-07-30T00:00:00Z', resultingBalance: '52608.86' }], page: 1, pageSize: 50, totalCount: 1 };
    else data = account;
    return { ok: true, json: async () => data };
  }));
  render(<App />);
  fireEvent.click(await screen.findByRole('button', { name: /Kan/ }));
  fireEvent.click(await screen.findByRole('button', { name: /52,608.86/ }));
  expect(await screen.findByRole('heading', { name: 'THB 52,608.86' })).toBeInTheDocument();
  expect(await screen.findByText('1002')).toBeInTheDocument();
  expect(screen.getByText('DEPOSIT')).toBeInTheDocument();
  expect(screen.getByText('THB 100.00')).toBeInTheDocument();
});

test('shows an explicit service error instead of a fabricated customer list', async () => {
  vi.stubGlobal('fetch', vi.fn(async () => { throw new Error('Connection unavailable'); }));
  render(<App />);
  expect(await screen.findByRole('alert')).toHaveTextContent('Connection unavailable');
});

test('ignores a stale account response after a different customer is selected', async () => {
  let resolveFirst;
  const firstResponse = new Promise((resolve) => { resolveFirst = resolve; });
  const page = (items) => ({ items, page: 1, pageSize: 50, totalCount: items.length });
  const reply = (data) => ({ ok: true, json: async () => data });
  vi.stubGlobal('fetch', vi.fn(async (url) => {
    if (url.startsWith('/api/customers?')) return reply(page([
      { id: 'first', name: 'First customer' }, { id: 'second', name: 'Second customer' },
    ]));
    if (url.startsWith('/api/customers/first/accounts')) return firstResponse;
    if (url.startsWith('/api/customers/second/accounts')) return reply(page([
      { id: 'current-account', customerId: 'second', currency: 'THB', balance: '10.00' },
    ]));
    return reply(page([]));
  }));
  render(<App />);
  fireEvent.click(await screen.findByRole('button', { name: /First customer/ }));
  fireEvent.click(screen.getByRole('button', { name: /Second customer/ }));
  expect(await screen.findByText('current-account')).toBeInTheDocument();
  await act(async () => resolveFirst(reply(page([
    { id: 'stale-account', customerId: 'first', currency: 'THB', balance: '20.00' },
  ]))));
  expect(screen.queryByText('stale-account')).not.toBeInTheDocument();
});

test('shows empty account history without an error', async () => {
  const account = { id: 'empty-account', customerId: 'customer', currency: 'THB', balance: '0.00' };
  const page = (items) => ({ items, page: 1, pageSize: 50, totalCount: items.length });
  vi.stubGlobal('fetch', vi.fn(async (url) => ({
    ok: true,
    json: async () => url.startsWith('/api/customers?') ? page([{ id: 'customer', name: 'Fictional customer' }])
      : url.includes('/customers/customer/accounts') || url.startsWith('/api/accounts?') ? page([account])
        : url.includes('/transactions') ? page([]) : account,
  })));
  render(<App />);
  const customerButton = await screen.findByRole('button', { name: /Fictional customer/ });
  fireEvent.click(customerButton);
  fireEvent.click(await screen.findByRole('button', { name: /THB 0.00/ }));
  expect(await screen.findByText('No transactions for this account.')).toBeInTheDocument();
  fireEvent.click(customerButton);
  expect(screen.queryByText('Loading accounts...')).not.toBeInTheDocument();
});

test('announces loading and a history read failure', async () => {
  let rejectHistory;
  const historyRequest = new Promise((_, reject) => { rejectHistory = reject; });
  const account = { id: 'account', customerId: 'customer', currency: 'THB', balance: '10.00' };
  const page = (items) => ({ items, page: 1, pageSize: 50, totalCount: items.length });
  vi.stubGlobal('fetch', vi.fn(async (url) => {
    if (url.includes('/transactions')) return historyRequest;
    return { ok: true, json: async () => url.startsWith('/api/customers?')
      ? page([{ id: 'customer', name: 'Customer' }]) : url.includes('/customers/customer/accounts') || url.startsWith('/api/accounts?')
        ? page([account]) : account };
  }));
  render(<App />);
  fireEvent.click(await screen.findByRole('button', { name: /Customer/ }));
  fireEvent.click(await screen.findByRole('button', { name: /account/ }));
  expect(screen.getByRole('status')).toHaveTextContent('Loading history');
  await act(async () => rejectHistory(new Error('Read failed')));
  expect(await screen.findByRole('alert')).toHaveTextContent('Read failed');
});

test('allows browsing additional customer pages', async () => {
  vi.stubGlobal('fetch', vi.fn(async (url) => {
    const second = url.includes('page=2');
    return { ok: true, json: async () => ({
      items: [{ id: second ? 'customer-2' : 'customer-1', name: second ? 'Second page customer' : 'First page customer' }],
      page: second ? 2 : 1, pageSize: 1, totalCount: 2,
    }) };
  }));
  render(<App />);
  expect(await screen.findByText('First page customer')).toBeInTheDocument();
  fireEvent.click(screen.getByRole('button', { name: 'Next' }));
  expect(await screen.findByText('Second page customer')).toBeInTheDocument();
});

test('allows browsing additional account and history pages', async () => {
  const account = (id) => ({ id, customerId: 'customer', currency: 'THB', balance: '10.00' });
  vi.stubGlobal('fetch', vi.fn(async (url) => {
    const second = url.includes('page=2');
    let data;
    if (url.startsWith('/api/customers?')) data = { items: [{ id: 'customer', name: 'Customer' }], page: 1, pageSize: 50, totalCount: 1 };
    else if (url.includes('/customers/customer/accounts')) data = { items: [account(second ? 'account-2' : 'account-1')], page: second ? 2 : 1, pageSize: 1, totalCount: 2 };
    else if (url.startsWith('/api/accounts?')) data = { items: [account('account-1'), account('account-2')], page: 1, pageSize: 50, totalCount: 2 };
    else if (url.includes('/transactions')) data = {
      items: [{ id: second ? 'entry-2' : 'entry-1', sequence: second ? '9007199254740993' : '1',
        type: 'DEPOSIT', amount: '1.00', completedAt: '2026-01-01T00:00:00Z', resultingBalance: '10.00' }],
      page: second ? 2 : 1, pageSize: 1, totalCount: 2,
    };
    else data = account('account-2');
    return { ok: true, json: async () => data };
  }));
  render(<App />);
  fireEvent.click(await screen.findByRole('button', { name: /Customer/ }));
  expect(await screen.findByText('account-1')).toBeInTheDocument();
  fireEvent.click(within(screen.getByRole('navigation', { name: 'Account pages' })).getByRole('button', { name: 'Next' }));
  fireEvent.click(await screen.findByRole('button', { name: /account-2/ }));
  expect(await screen.findByText('DEPOSIT')).toBeInTheDocument();
  fireEvent.click(within(screen.getByRole('navigation', { name: 'History pages' })).getByRole('button', { name: 'Next' }));
  expect(await screen.findByText('9007199254740993')).toBeInTheDocument();
});

test('shows fictional customers from the API without login or account creation', async () => {
  vi.stubGlobal('fetch', vi.fn(async () => ({
    ok: true,
    json: async () => ({
      items: [{ id: 'ae9d2161-c812-4a5c-9758-121aacd45e23', name: 'กานต์ แสงสาธิต', englishName: 'Kan Saengsathit' }],
      page: 1, pageSize: 50, totalCount: 1,
    }),
  })));
  render(<App />);
  expect(await screen.findByText('Kan Saengsathit')).toBeInTheDocument();
  expect(screen.getByText('กานต์ แสงสาธิต')).toBeInTheDocument();
  expect(screen.queryByRole('button', { name: /sign in|create account/i })).not.toBeInTheDocument();
});
