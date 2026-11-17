export function formatMoney(value) {
  const match = /^([0-9]+)\.([0-9]{2})$/.exec(value ?? '');
  if (!match) return 'Unavailable';
  return `THB ${match[1].replace(/\B(?=(\d{3})+(?!\d))/g, ',')}.${match[2]}`;
}

export function validAmount(value) {
  if (!/^[0-9]{1,16}(\.[0-9]{1,2})?$/.test(value)) return false;
  const [whole, fraction = ''] = value.split('.');
  const satang = BigInt(whole) * 100n + BigInt(fraction.padEnd(2, '0'));
  return satang > 0n && satang <= 999999999999999999n;
}
