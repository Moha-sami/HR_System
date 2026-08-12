// Shared JWT helpers for tests.

export function fakeJwt(payload: Record<string, unknown>): string {
  const header = btoa(JSON.stringify({ alg: 'none' }));
  const body = btoa(JSON.stringify(payload));
  return `${header}.${body}.fake-sig`;
}

export function futureExp(minutes = 30): number {
  return Math.floor(Date.now() / 1000) + minutes * 60;
}

export function pastExp(minutes = 30): number {
  return Math.floor(Date.now() / 1000) - minutes * 60;
}
