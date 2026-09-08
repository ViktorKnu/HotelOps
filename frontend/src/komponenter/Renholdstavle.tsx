import { useState } from 'react'
import type { Rom } from '../typer/rom'
import type { Rengjøringshandling } from '../tjenester/romtjeneste'
import { Romkort } from './Romkort'

interface RenholdstavleEgenskaper {
  rom: Rom[]
  onEndreRengjøringsstatus: (romId: string, handling: Rengjøringshandling) => Promise<void>
}

export function Renholdstavle({ rom, onEndreRengjøringsstatus }: RenholdstavleEgenskaper) {
  const [etasje, setEtasje] = useState('alle')
  const etasjer = [...new Set(rom.map((hotellrom) => hotellrom.etasje))].sort((a, b) => a - b)
  const renholdsrom = rom
    .filter((hotellrom) => hotellrom.rengjøringsstatus !== 'Ren')
    .filter((hotellrom) => etasje === 'alle' || String(hotellrom.etasje) === etasje)
    .sort((a, b) => a.etasje - b.etasje || a.nummer.localeCompare(b.nummer, 'nb', { numeric: true }))
  const kolonner = [
    { status: 'Skitten', tittel: 'Venter på renhold', tomtekst: 'Ingen rom venter på renhold.' },
    { status: 'UnderRengjøring', tittel: 'Under rengjøring', tomtekst: 'Ingen rengjøring pågår.' },
  ] as const

  return (
    <section aria-labelledby="renholdsoverskrift">
      <div className="renholdshode">
        <div>
          <h2 id="renholdsoverskrift">Renholdstavle</h2>
          <p>Start og fullfør renhold. Ferdige rom fjernes fra tavlen.</p>
        </div>
        <div className="skjemafelt">
          <label htmlFor="renholdsetasje">Etasje</label>
          <select id="renholdsetasje" value={etasje}
            onChange={(hendelse) => setEtasje(hendelse.target.value)}>
            <option value="alle">Alle etasjer</option>
            {etasjer.map((nummer) => <option key={nummer} value={nummer}>Etasje {nummer}</option>)}
          </select>
        </div>
      </div>
      <p className="renholdsoppsummering" role="status">
        {renholdsrom.filter((hotellrom) => hotellrom.rengjøringsstatus === 'Skitten').length} venter på renhold,
        {' '}{renholdsrom.filter((hotellrom) => hotellrom.rengjøringsstatus === 'UnderRengjøring').length} under rengjøring
        {etasje === 'alle' ? ' på hotellet.' : ` i etasje ${etasje}.`}
      </p>
      <div className="renholdstavle">
        {kolonner.map((kolonne) => {
          const kolonnerom = renholdsrom.filter((hotellrom) => hotellrom.rengjøringsstatus === kolonne.status)
          return (
            <section className="renholdskolonne" key={kolonne.status}
              aria-labelledby={`renhold-${kolonne.status}`}>
              <h3 id={`renhold-${kolonne.status}`}>
                {kolonne.tittel} <span className="renholdsantall">{kolonnerom.length}</span>
              </h3>
              {kolonnerom.length === 0
                ? <p className="renholdstom">{kolonne.tomtekst}</p>
                : kolonnerom.map((hotellrom) => (
                  <Romkort key={hotellrom.id} rom={hotellrom}
                    onEndreRengjøringsstatus={onEndreRengjøringsstatus} />
                ))}
            </section>
          )
        })}
      </div>
    </section>
  )
}
