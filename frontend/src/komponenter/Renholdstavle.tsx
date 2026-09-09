import { useState } from 'react'
import type { Rom } from '../typer/rom'
import type { Rengjøringshandling, Renholdsplan } from '../tjenester/romtjeneste'
import { Romkort } from './Romkort'
import { velgRenholdsrom } from '../tjenester/renholdsutvalg'

interface RenholdstavleEgenskaper {
  onPlanleggRenhold: (romId: string, plan: Renholdsplan, versjon: string) => Promise<void>
  rom: Rom[]
  onEndreRengjøringsstatus: (romId: string, handling: Rengjøringshandling, versjon: string) => Promise<void>
}

export function Renholdstavle({ rom, onEndreRengjøringsstatus, onPlanleggRenhold }: RenholdstavleEgenskaper) {
  const [etasje, setEtasje] = useState('alle')
  const [ansvarlig, setAnsvarlig] = useState('alle')
  const [prioritet, setPrioritet] = useState('alle')
  const etasjer = [...new Set(rom.map((hotellrom) => hotellrom.etasje))].sort((a, b) => a - b)
  const renholdere = [...new Set([
    ...rom.map((hotellrom) => hotellrom.ansvarligRenholder).filter((navn): navn is string => !!navn),
    // Behold valgt navn når siste oppgave fullføres eller tildeles på nytt.
    ...(ansvarlig.startsWith('navn:') ? [ansvarlig.slice(5)] : []),
  ])].sort((a, b) => a.localeCompare(b, 'nb'))
  const renholdsrom = velgRenholdsrom(rom, { etasje, ansvarlig, prioritet })
  const totaltRenholdsbehov = rom.filter((hotellrom) => hotellrom.rengjøringsstatus !== 'Ren').length
  const harFiltre = etasje !== 'alle' || ansvarlig !== 'alle' || prioritet !== 'alle'

  function nullstillFiltre() {
    setEtasje('alle')
    setAnsvarlig('alle')
    setPrioritet('alle')
  }
  const kolonner = [
    { status: 'Skitten', tittel: 'Venter på renhold', tomtekst: 'Ingen rom venter på renhold.' },
    { status: 'UnderRengjøring', tittel: 'Under rengjøring', tomtekst: 'Ingen rengjøring pågår.' },
  ] as const

  return (
    <section aria-labelledby="renholdsoverskrift">
      <div className="renholdshode">
        <div>
          <h2 id="renholdsoverskrift">Renholdstavle</h2>
          <p>Hasteoppgaver vises først. Tildel renhold, start og fullfør. Ferdige rom fjernes fra tavlen.</p>
        </div>
      </div>
      <div className="renholdsfiltre" role="group" aria-label="Filtrer renholdstavlen">
        <div className="skjemafelt">
          <label htmlFor="renholdsetasje">Etasje</label>
          <select id="renholdsetasje" value={etasje}
            onChange={(hendelse) => setEtasje(hendelse.target.value)}>
            <option value="alle">Alle etasjer</option>
            {etasjer.map((nummer) => <option key={nummer} value={nummer}>Etasje {nummer}</option>)}
          </select>
        </div>
        <div className="skjemafelt">
          <label htmlFor="renholdsansvarlig">Ansvarlig renholder</label>
          <select id="renholdsansvarlig" value={ansvarlig}
            onChange={(hendelse) => setAnsvarlig(hendelse.target.value)}>
            <option value="alle">Alle renholdere</option>
            <option value="utildelt">Ikke tildelt</option>
            {renholdere.map((navn) => <option key={navn} value={`navn:${navn}`}>{navn}</option>)}
          </select>
        </div>
        <div className="skjemafelt">
          <label htmlFor="renholdsprioritet">Prioritet</label>
          <select id="renholdsprioritet" value={prioritet}
            onChange={(hendelse) => setPrioritet(hendelse.target.value)}>
            <option value="alle">Alle prioriteter</option>
            <option value="Haster">Haster</option>
            <option value="Normal">Normal</option>
          </select>
        </div>
        <button className="nullstillknapp" type="button" disabled={!harFiltre} onClick={nullstillFiltre}>
          Nullstill filtre
        </button>
      </div>
      <p className="renholdsoppsummering" role="status">
        Viser {renholdsrom.length} av {totaltRenholdsbehov} rom med renholdsbehov.
        {' '}
        {renholdsrom.filter((hotellrom) => hotellrom.rengjøringsstatus === 'Skitten').length} venter på renhold,
        {' '}{renholdsrom.filter((hotellrom) => hotellrom.rengjøringsstatus === 'UnderRengjøring').length} under rengjøring
        {' i utvalget. Nøkkeltallene øverst gjelder hele hotellet.'}
      </p>
      {harFiltre && renholdsrom.length === 0 && (
        <div className="beskjed">
          <h3>Ingen renholdsrom passer filtrene</h3>
          <p>Endre utvalget for å se andre rom med renholdsbehov.</p>
          <button type="button" onClick={nullstillFiltre}>Vis alt renhold</button>
        </div>
      )}
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
                ? <p className="renholdstom">{harFiltre ? 'Ingen rom i dette utvalget.' : kolonne.tomtekst}</p>
                : kolonnerom.map((hotellrom) => (
                  <Romkort key={hotellrom.id} rom={hotellrom}
                    onPlanleggRenhold={onPlanleggRenhold}
                    onEndreRengjøringsstatus={onEndreRengjøringsstatus} />
                ))}
            </section>
          )
        })}
      </div>
    </section>
  )
}
