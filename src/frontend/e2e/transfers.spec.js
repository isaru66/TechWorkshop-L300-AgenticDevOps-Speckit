import { expect, test } from '@playwright/test';

const source = '00209a31-c483-41bb-a9fd-35530f8f680b';
const destination = 'fc99d3e9-f19d-439e-87e4-c7a54b585c7d';

async function all(request, path) {
  const items = [];
  for (let page = 1; ; page++) {
    const data = await (await request.get(`${path}?page=${page}&pageSize=100`)).json();
    items.push(...data.items);
    if (items.length >= data.totalCount) return items;
  }
}

async function snapshot(request) {
  return {
    source: await (await request.get(`/api/accounts/${source}/balance`)).json(),
    destination: await (await request.get(`/api/accounts/${destination}/balance`)).json(),
    sourceHistory: await all(request, `/api/accounts/${source}/transactions`),
    destinationHistory: await all(request, `/api/accounts/${destination}/transactions`),
    transfers: await all(request, '/api/transfers'),
  };
}

const cents = (value) => {
  const [whole, fraction] = value.split('.');
  return BigInt(whole) * 100n + BigInt(fraction);
};
const amount = (value) => `${value / 100n}.${String(value % 100n).padStart(2, '0')}`;

test('transfers to another account and proves insufficient funds changes neither side', async ({ page, request }) => {
  const before = await snapshot(request);
  expect(cents(before.source.balance)).toBeGreaterThanOrEqual(10000n);
  const sourceAccount = await (await request.get(`/api/accounts/${source}`)).json();
  const customers = await all(request, '/api/customers');
  const customerIndex = customers.findIndex((item) => item.id === sourceAccount.customerId);
  const customer = customers[customerIndex];
  const owned = await all(request, `/api/customers/${customer.id}/accounts`);
  const accountIndex = owned.findIndex((item) => item.id === source);
  const accounts = await all(request, '/api/accounts');
  const destinationIndex = accounts.findIndex((item) => item.id === destination);
  expect(customerIndex).toBeGreaterThanOrEqual(0);
  expect(accountIndex).toBeGreaterThanOrEqual(0);
  expect(destinationIndex).toBeGreaterThanOrEqual(0);
  await page.goto('/');
  for (let index = 0; index < Math.floor(customerIndex / 50); index++)
    await page.getByRole('navigation', { name: 'Customer pages' }).getByRole('button', { name: 'Next' }).click();
  await page.getByRole('region', { name: 'Customers' }).getByRole('button', { name: customer.englishName || customer.name }).first().click();
  for (let index = 0; index < Math.floor(accountIndex / 50); index++)
    await page.getByRole('navigation', { name: 'Account pages' }).getByRole('button', { name: 'Next' }).click();
  await page.getByRole('button', { name: new RegExp(source) }).click();
  for (let index = 0; index < Math.floor(destinationIndex / 50); index++) {
    const nav = page.getByRole('navigation', { name: 'Destination account pages' });
    await expect(nav.getByText(`Page ${index + 1} of`)).toBeVisible();
    await nav.getByRole('button', { name: 'Next' }).click();
  }
  await expect(page.getByRole('option', { name: new RegExp(destination) })).toBeAttached();
  await page.getByLabel('Destination account', { exact: true }).selectOption(destination);
  await page.getByLabel('Transfer amount').fill('100.00');
  await page.getByRole('button', { name: 'Transfer', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Transfer completed' })).toBeVisible();
  const after = await snapshot(request);
  expect(cents(after.source.balance)).toBe(cents(before.source.balance) - 10000n);
  expect(cents(after.destination.balance)).toBe(cents(before.destination.balance) + 10000n);
  expect(after.sourceHistory.slice(0, before.sourceHistory.length)).toEqual(before.sourceHistory);
  expect(after.destinationHistory.slice(0, before.destinationHistory.length)).toEqual(before.destinationHistory);
  expect(after.sourceHistory).toHaveLength(before.sourceHistory.length + 1);
  expect(after.destinationHistory).toHaveLength(before.destinationHistory.length + 1);
  const outgoing = after.sourceHistory.at(-1);
  const incoming = after.destinationHistory.at(-1);
  expect(outgoing.transferId).toBe(incoming.transferId);
  expect(outgoing.counterpartyAccountId).toBe(destination);
  expect(incoming.counterpartyAccountId).toBe(source);
  expect(after.transfers).toHaveLength(before.transfers.length + 1);
  await expect(page.getByText(`Transfer ID: ${outgoing.transferId}`)).toBeVisible();
  await page.getByLabel('Transfer amount').fill(amount(cents(after.source.balance) + 1n));
  await page.getByRole('button', { name: 'Transfer', exact: true }).click();
  await expect(page.getByText(/insufficient_funds/)).toBeVisible();
  expect(await snapshot(request)).toEqual(after);
});
