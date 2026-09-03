import { useState } from 'react'
import type { Rengjøringshandling } from '../tjenester/romtjeneste'
import type {
  Beleggsstatus,
  Driftsstatus,
  Rengjøringsstatus,
  Rom,
} from '../typer/rom'

type Romstatus = Beleggsstatus | Rengjøringsstatus | Driftsstatus

const statusnavn: Record<Romstatus, string> = {
  Ledig: 'Ledig',
  Opptatt: 'Opptatt',
  Ren: 'Ren',
  Skitten: 'Skitten',
  UnderRengjøring: 'Under rengjøring',
  Operativ: 'Operativ',
  UnderVedlikehold: 'Under vedlikehold',
  UteAvDrift: 'Ute av drift',
}

const statusstil: Record<Romstatus, string> = {
  Ledig: 'positiv',
  Opptatt: 'nøytral',
  Ren: 'positiv',
  Skitten: 'advarsel',
  UnderRengjøring: 'nøytral',
  Operativ: 'positiv',
  UnderVedlikehold: 'advarsel',
  UteAvDrift: 'kritisk',
}

interface RomkortEgenskaper {
  rom: Rom
  onEndreRengjøringsstatus: (
    romId: string,
    handling: Rengjøringshandling,
  ) => Promise<void>
}

const nesteRengjøringshandling: Record<
  Rengjøringsstatus,
  { handling: Rengjøringshandling; tekst: string }
> = {
  Ren: { handling: 'marker-skitten', tekst: 'Marker som skittent' },
  Skitten: { handling: 'start', tekst: 'Start rengjøring' },
  UnderRengjøring: { handling: 'fullfor', tekst: 'Fullfør rengjøring' },
}

function Statusmerke({ navn, status }: { navn: string; status: Romstatus }) {
  return (
    <div className="statusrad">
      <dt>{navn}</dt>
      <dd className={`statusmerke statusmerke--${statusstil[status]}`}>
        {statusnavn[status]}
      </dd>
    </div>
  )
}

export function Romkort({ rom, onEndreRengjøringsstatus }: RomkortEgenskaper) {
  const [utførerHandling, setUtførerHandling] = useState(false)
  const [feilmelding, setFeilmelding] = useState<string | null>(null)
  const nesteHandling = nesteRengjøringshandling[rom.rengjøringsstatus]

  async function utførRengjøringshandling() {
    setUtførerHandling(true)
    setFeilmelding(null)

    try {
      await onEndreRengjøringsstatus(rom.id, nesteHandling.handling)
    } catch (feil) {
      setFeilmelding(
        feil instanceof Error
          ? feil.message
          : 'Rengjøringsstatusen kunne ikke endres.',
      )
    } finally {
      setUtførerHandling(false)
    }
  }

  return (
    <article className="romkort">
      <div className="romkort__topp">
        <div>
          <p className="romkort__etikett">Rom</p>
          <h2>{rom.nummer}</h2>
        </div>
        <span
          className={`klarstatus ${rom.erKlartForInnsjekking ? 'klarstatus--klar' : ''}`}
        >
          {rom.erKlartForInnsjekking ? 'Klar for innsjekking' : 'Ikke klar'}
        </span>
      </div>

      <p className="romkort__etasje">Etasje {rom.etasje}</p>

      <dl className="statusliste">
        <Statusmerke navn="Belegg" status={rom.beleggsstatus} />
        <Statusmerke navn="Renhold" status={rom.rengjøringsstatus} />
        <Statusmerke navn="Drift" status={rom.driftsstatus} />
      </dl>

      <div className="romkort__handling">
        <button
          type="button"
          disabled={utførerHandling}
          onClick={() => void utførRengjøringshandling()}
        >
          {utførerHandling ? 'Oppdaterer …' : nesteHandling.tekst}
        </button>
        <p className="romkort__handlingsfeil" role="alert">
          {feilmelding}
        </p>
      </div>
    </article>
  )
}
