import type { Rom } from '../typer/rom'

export async function hentRom(signal?: AbortSignal): Promise<Rom[]> {
  const svar = await fetch('/api/rom', {
    headers: { Accept: 'application/json' },
    signal,
  })

  if (!svar.ok) {
    throw new Error('Kunne ikke hente romoversikten.')
  }

  return (await svar.json()) as Rom[]
}
