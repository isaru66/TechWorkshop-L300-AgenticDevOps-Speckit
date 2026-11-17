import { expect, test } from '@playwright/test';

const sourceId = '00209a31-c483-41bb-a9fd-35530f8f680b';

async function history(request, id) {
  const entries = [];
  for (let page = 1; ; page++) {
    const data = await (await request.get(`/api/accounts/${id}/transactions?page=${page}&pageSize=100`)).json();
    entries.push(...data.items);
    if (entries.length >= data.totalCount) return entries;
  }
}

test('deposits and withdraws with preserved history and rejects excessive withdrawal', async ({ page, request }) => {
  const account = await (await request.get(`/api/accounts/${sourceId}`)).json();
  const customers = await (await request.get('/api/customers?page=1&pageSize=100')).json();
  const first = customers.items.findIndex((item) => item.id === account.customerId);
  let customer;
  let customerPage;
  if (first >= 0) { customer = customers.items[first]; customerPage = Math.floor(first / 50) + 1; }
  else {
    const rest = await (await request.get('/api/customers?page=2&pageSize=100')).json();
    customer = rest.items.find((item) => item.id === account.customerId);
    customerPage = 3;
  }
  expect(customer).toBeTruthy();
  const customerAccounts = await (await request.get(`/api/customers/${customer.id}/accounts?page=1&pageSize=100`)).json();
  const accountPage = Math.floor(customerAccounts.items.findIndex((item) => item.id === sourceId) / 50) + 1;
  const before = await history(request, sourceId);
  await page.goto('/');
  for (let i = 1; i < customerPage; i++) await page.getByRole('navigation', { name: 'Customer pages' }).getByRole('button', { name: 'Next' }).click();
  await page.getByRole('region', { name: 'Customers' }).getByRole('button', { name: customer.englishName || customer.name }).first().click();
  for (let i = 1; i < accountPage; i++) await page.getByRole('navigation', { name: 'Account pages' }).getByRole('button', { name: 'Next' }).click();
  await page.getByRole('button', { name: new RegExp(sourceId) }).click();
  await page.getByLabel('Deposit amount').fill('1.00');
  await page.getByRole('button', { name: 'Deposit', exact: true }).click();
  await expect(page.getByText(/Completed deposit/)).toBeVisible();
  await page.getByLabel('Withdrawal amount').fill('1.00');
  await page.getByRole('button', { name: 'Withdraw', exact: true }).click();
  await expect(page.getByText(/Completed withdrawal/)).toBeVisible();
  const after = await (await request.get(`/api/accounts/${sourceId}`)).json();
  const updated = await history(request, sourceId);
  expect(after.balance).toBe(account.balance);
  expect(updated.slice(0, before.length)).toEqual(before);
  expect(updated.slice(before.length).map((entry) => entry.type)).toEqual(['DEPOSIT', 'WITHDRAWAL']);
  const [whole, fraction] = after.balance.split('.');
  const excessive = (BigInt(whole) * 100n + BigInt(fraction) + 1n);
  await page.getByLabel('Withdrawal amount').fill(`${excessive / 100n}.${String(excessive % 100n).padStart(2, '0')}`);
  await page.getByRole('button', { name: 'Withdraw', exact: true }).click();
  await expect(page.getByText(/insufficient_funds/)).toBeVisible();
  expect((await (await request.get(`/api/accounts/${sourceId}`)).json()).balance).toBe(after.balance);
  expect(await history(request, sourceId)).toEqual(updated);
});
