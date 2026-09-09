import { useState, type FormEvent } from 'react'
import type { Rengjøringshandling, Renholdsplan } from '../tjenester/romtjeneste'
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
  onPlanleggRenhold?: (romId: string, plan: Renholdsplan, versjon: string) => Promise<void>
  rom: Rom
  onEndreRengjøringsstatus: (
    romId: string,
    handling: Rengjøringshandling,
    versjon: string,
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

export function Romkort({ rom, onEndreRengjøringsstatus, onPlanleggRenhold }: RomkortEgenskaper) {
  const [utførerHandling, setUtførerHandling] = useState(false)
  const [lagret, setLagret] = useState(false)
  const [feilmelding, setFeilmelding] = useState<string | null>(null)
  const nesteHandling = nesteRengjøringshandling[rom.rengjøringsstatus]

  async function utførRengjøringshandling() {
    setUtførerHandling(true)
    setFeilmelding(null)
    setLagret(false)

    try {
      await onEndreRengjøringsstatus(rom.id, nesteHandling.handling, rom.versjon)
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

  async function lagrePlan(hendelse: FormEvent<HTMLFormElement>) {
    hendelse.preventDefault()
    if (!onPlanleggRenhold || utførerHandling) return
    const data = new FormData(hendelse.currentTarget)
    setUtførerHandling(true)
    setFeilmelding(null)
    setLagret(false)
    try {
      await onPlanleggRenhold(rom.id, {
        ansvarligRenholder: String(data.get('ansvarligRenholder') ?? ''),
        prioritet: data.get('prioritet') === 'Haster' ? 'Haster' : 'Normal',
      }, rom.versjon)
      setLagret(true)
    } catch (feil) {
      setFeilmelding(feil instanceof Error ? feil.message : 'Renholdet kunne ikke planlegges.')
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

      {rom.rengjøringsstatus !== 'Ren' && (
        <p className="renholdsdetaljer">
          <strong>{rom.renholdsprioritet === 'Haster' ? 'Haster' : 'Normal prioritet'}</strong>
          {' · '}{rom.ansvarligRenholder || 'Ikke tildelt'}
        </p>
      )}

      {onPlanleggRenhold && rom.rengjøringsstatus !== 'Ren' && (
        <form className="renholdsplan" onSubmit={(hendelse) => void lagrePlan(hendelse)}
          key={rom.versjon}
          onChange={() => setLagret(false)}>
          <fieldset disabled={utførerHandling}>
            <legend>Planlegg renhold for rom {rom.nummer}</legend>
            <div className="skjemafelt">
              <label htmlFor={`ansvarlig-${rom.id}`}>Ansvarlig renholder</label>
              <input id={`ansvarlig-${rom.id}`} name="ansvarligRenholder" maxLength={100}
                defaultValue={rom.ansvarligRenholder ?? ''} placeholder="Ikke tildelt" />
            </div>
            <div className="skjemafelt">
              <label htmlFor={`prioritet-${rom.id}`}>Prioritet</label>
              <select id={`prioritet-${rom.id}`} name="prioritet" defaultValue={rom.renholdsprioritet}>
                <option value="Normal">Normal</option>
                <option value="Haster">Haster</option>
              </select>
            </div>
            <button className="lagreknapp" type="submit">Lagre plan</button>
          </fieldset>
          <p role="status">{lagret ? 'Renholdsplanen er lagret.' : ''}</p>
        </form>
      )}

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
