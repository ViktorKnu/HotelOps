import type { Rom } from '../typer/rom'

export interface OpprettRomForespørsel {
  nummer: string
  etasje: number
}

export type Rengjøringshandling = 'marker-skitten' | 'start' | 'fullfor'

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

async function lesRengjøringsfeil(svar: Response): Promise<string> {
  try {
    const problem = (await svar.json()) as ProblemDetaljer
    return problem.detail ?? problem.title ?? 'Rengjøringsstatusen kunne ikke endres.'
  } catch {
    return 'Rengjøringsstatusen kunne ikke endres.'
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

export async function endreRengjøringsstatus(
  romId: string,
  handling: Rengjøringshandling,
): Promise<Rom> {
  try {
    const svar = await fetch(`/api/rom/${romId}/rengjoring/${handling}`, {
      method: 'PATCH',
      headers: { Accept: 'application/json' },
    })

    if (!svar.ok) {
      throw new Romstatusfeil(await lesRengjøringsfeil(svar))
    }

    return (await svar.json()) as Rom
  } catch (feil) {
    if (feil instanceof Romstatusfeil) {
      throw feil
    }

    throw new Romstatusfeil(
      'Rengjøringsstatusen kunne ikke endres. Kontroller tilkoblingen og prøv igjen.',
    )
  }
}

class Romstatusfeil extends Error {}
