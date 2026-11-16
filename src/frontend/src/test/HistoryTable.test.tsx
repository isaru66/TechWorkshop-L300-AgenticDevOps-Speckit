import { screen, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api } from '../api/client';
import HistoryTable from '../components/HistoryTable';
import { renderRoute } from './utils';

vi.mock('../api/client', async (orig) => {
  const actual = await orig<typeof import('../api/client')>();
  return { ...actual, api: { listTransactions: vi.fn() } };
});

const e = (seq: number, amount: string) => ({
  id: `e${seq}`, sequence: seq, accountId: 'acc-1', type: 'DEPOSIT' as const, amount, currency: 'THB' as const,
  completedAt: '2025-01-01T00:00:00Z', resultingBalance: amount,
});

const show = () => renderRoute(<HistoryTable accountId="acc-1" />, '/', '/');

beforeEach(() => vi.resetAllMocks());

describe('HistoryTable', () => {
  it('renders rows in the order returned (newest first)', async () => {
    vi.mocked(api.listTransactions).mockResolvedValue({
      page: 0, size: 20, totalElements: 2, totalPages: 1, items: [e(2, '20.00'), e(1, '10.00')],
    });
    show();
    await screen.findByRole('table');
    const rows = screen.getAllByRole('row').slice(1);
    expect(within(rows[0]).getAllByText('20.00').length).toBeGreaterThan(0);
    expect(within(rows[1]).getAllByText('10.00').length).toBeGreaterThan(0);
  });

  it('pages forward and back', async () => {
    vi.mocked(api.listTransactions).mockImplementation(async (_id, page = 0) => ({
      page, size: 1, totalElements: 2, totalPages: 2, items: [e(2 - page, page ? '10.00' : '20.00')],
    }));
    show();
    await screen.findByText('Page 1 of 2');
    expect(screen.getByRole('button', { name: 'Previous' })).toBeDisabled();
    await userEvent.click(screen.getByRole('button', { name: 'Next' }));
    await screen.findByText('Page 2 of 2');
    expect(api.listTransactions).toHaveBeenLastCalledWith('acc-1', 1, 20);
    expect(screen.getByRole('button', { name: 'Next' })).toBeDisabled();
    await userEvent.click(screen.getByRole('button', { name: 'Previous' }));
    await screen.findByText('Page 1 of 2');
  });

  it('shows the empty state', async () => {
    vi.mocked(api.listTransactions).mockResolvedValue({ page: 0, size: 20, totalElements: 0, totalPages: 0, items: [] });
    show();
    expect(await screen.findByText('No transactions yet.')).toBeInTheDocument();
  });
});
