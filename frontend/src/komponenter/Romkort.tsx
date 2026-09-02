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

export function Romkort({ rom }: RomkortEgenskaper) {
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
    </article>
  )
}
