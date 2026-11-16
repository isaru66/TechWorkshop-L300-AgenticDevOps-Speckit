import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { api, ApiError } from '../api/client';
import TransferPage from '../pages/TransferPage';
import { renderRoute } from './utils';

vi.mock('../api/client', async (orig) => {
  const actual = await orig<typeof import('../api/client')>();
  return { ...actual, api: { listAccounts: vi.fn(), transfer: vi.fn(), getBalance: vi.fn() } };
});

const A = '11111111-1111-1111-1111-111111111111';
const B = '22222222-2222-2222-2222-222222222222';

async function fill(amount: string) {
  const user = userEvent.setup();
  await user.type(screen.getByLabelText('Source account'), A);
  await user.type(screen.getByLabelText('Destination account'), B);
  await user.type(screen.getByLabelText('Amount (THB)'), amount);
  await user.click(screen.getByRole('button', { name: 'Transfer' }));
}

beforeEach(() => {
  vi.resetAllMocks();
  vi.mocked(api.listAccounts).mockResolvedValue({ page: 0, size: 100, totalElements: 0, totalPages: 0, items: [] });
});

describe('TransferPage', () => {
  it('shows a confirmation on success', async () => {
    vi.mocked(api.transfer).mockResolvedValue({
      id: 't1', sourceAccountId: A, destinationAccountId: B, amount: '100.00', currency: 'THB', completedAt: '2025-01-01T00:00:00Z',
    });
    vi.mocked(api.getBalance)
      .mockResolvedValueOnce({ accountId: A, currency: 'THB', balance: '900.00' })
      .mockResolvedValueOnce({ accountId: B, currency: 'THB', balance: '1100.00' });
    renderRoute(<TransferPage />, '/transfer', '/transfer');
    await fill('100.00');
    expect(await screen.findByText(/Transfer completed/)).toBeInTheDocument();
    expect(await screen.findByText(/Source balance: 900.00/)).toBeInTheDocument();
    expect(api.transfer).toHaveBeenCalledWith({ sourceAccountId: A, destinationAccountId: B, amount: '100.00' });
  });

  it('shows a clear error on INSUFFICIENT_FUNDS and fetches no balances', async () => {
    vi.mocked(api.transfer).mockRejectedValue(
      new ApiError(422, { code: 'INSUFFICIENT_FUNDS', message: 'Insufficient funds' }),
    );
    renderRoute(<TransferPage />, '/transfer', '/transfer');
    await fill('999999.00');
    expect(await screen.findByText(/Insufficient funds/)).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByText(/Transfer completed/)).not.toBeInTheDocument());
    expect(api.getBalance).not.toHaveBeenCalled();
  });
});
