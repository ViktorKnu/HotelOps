import type { Rom } from '../typer/rom'

export interface OpprettRomForespørsel {
  nummer: string
  etasje: number
}

interface ProblemDetaljer {
  title?: string
  detail?: string
  errors?: Record<string, string[]>
}

class Romregistreringsfeil extends Error {}

async function lesFeilmelding(svar: Response): Promise<string> {
  try {
    const problem = (await svar.json()) as ProblemDetaljer
    const valideringsfeil = problem.errors?.nummer?.[0]

    return (
      valideringsfeil ??
      problem.detail ??
      problem.title ??
      'Rommet kunne ikke registreres.'
    )
  } catch {
    return 'Rommet kunne ikke registreres.'
  }
}

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

export async function opprettRom(forespørsel: OpprettRomForespørsel): Promise<Rom> {
  try {
    const svar = await fetch('/api/rom', {
      method: 'POST',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(forespørsel),
    })

    if (!svar.ok) {
      throw new Romregistreringsfeil(await lesFeilmelding(svar))
    }

    return (await svar.json()) as Rom
  } catch (feil) {
    if (feil instanceof Romregistreringsfeil) {
      throw feil
    }

    throw new Romregistreringsfeil(
      'Rommet kunne ikke registreres. Kontroller tilkoblingen og prøv igjen.',
    )
  }
}
