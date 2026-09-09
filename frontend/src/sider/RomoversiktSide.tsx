import { useEffect, useState } from 'react'
import { Romkort } from '../komponenter/Romkort'
import { RegistrerRomskjema } from '../komponenter/RegistrerRomskjema'
import { Renholdstavle } from '../komponenter/Renholdstavle'
import {
  endreRengjøringsstatus,
  hentRom,
  planleggRenhold,
  type Renholdsplan,
  type Rengjøringshandling,
} from '../tjenester/romtjeneste'
import type { Rom } from '../typer/rom'

export function RomoversiktSide() {
  const [rom, setRom] = useState<Rom[]>([])
  const [laster, setLaster] = useState(true)
  const [feilmelding, setFeilmelding] = useState<string | null>(null)
  const [forsøk, setForsøk] = useState(0)
  const [viserRegistrering, setViserRegistrering] = useState(false)
  const [søk, setSøk] = useState('')
  const [etasje, setEtasje] = useState('alle')
  const [statusfilter, setStatusfilter] = useState('alle')
  const [visning, setVisning] = useState<'rom' | 'renhold'>('rom')

  useEffect(() => {
    const avbryter = new AbortController()

    async function lastRom() {
      setLaster(true)
      setFeilmelding(null)

      try {
        setRom(await hentRom(avbryter.signal))
      } catch (feil) {
        if (feil instanceof DOMException && feil.name === 'AbortError') {
          return
        }

        setFeilmelding('Romoversikten kunne ikke lastes. Prøv igjen om litt.')
      } finally {
        if (!avbryter.signal.aborted) {
          setLaster(false)
        }
      }
    }

    void lastRom()
    return () => avbryter.abort()
  }, [forsøk])

  const antallKlare = rom.filter(
    (hotellrom) => hotellrom.erKlartForInnsjekking,
  ).length
  const måRengjøres = rom.filter(
    (hotellrom) => hotellrom.rengjøringsstatus !== 'Ren',
  ).length
  const driftsavvik = rom.filter(
    (hotellrom) => hotellrom.driftsstatus !== 'Operativ',
  ).length

  const etasjer = [...new Set(rom.map((hotellrom) => hotellrom.etasje))].sort(
    (a, b) => a - b,
  )
  const synligeRom = rom.filter((hotellrom) => {
    const matcherSøk = hotellrom.nummer.toLocaleLowerCase('nb').includes(
      søk.trim().toLocaleLowerCase('nb'),
    )
    const matcherEtasje = etasje === 'alle' || String(hotellrom.etasje) === etasje
    const matcherStatus = statusfilter === 'alle'
      || (statusfilter === 'klare' && hotellrom.erKlartForInnsjekking)
      || (statusfilter === 'renhold' && hotellrom.rengjøringsstatus !== 'Ren')
      || (statusfilter === 'drift' && hotellrom.driftsstatus !== 'Operativ')
    return matcherSøk && matcherEtasje && matcherStatus
  })

  function nullstillFiltre() {
    setSøk('')
    setEtasje('alle')
    setStatusfilter('alle')
  }

  function leggTilRom(nyttRom: Rom) {
    setFeilmelding(null)
    setLaster(false)
    setRom((registrerteRom) =>
      [...registrerteRom, nyttRom].sort((første, andre) =>
        første.nummer.localeCompare(andre.nummer, 'nb'),
      ),
    )
  }

  async function oppdaterRengjøringsstatus(
    romId: string,
    handling: Rengjøringshandling,
    versjon: string,
  ) {
    const oppdatertRom = await endreRengjøringsstatus(romId, handling, versjon)
    setRom((registrerteRom) =>
      registrerteRom.map((hotellrom) =>
        hotellrom.id === oppdatertRom.id ? oppdatertRom : hotellrom,
      ),
    )
  }

  async function oppdaterRenholdsplan(romId: string, plan: Renholdsplan, versjon: string) {
    const oppdatertRom = await planleggRenhold(romId, plan, versjon)
    setRom((registrerteRom) => registrerteRom.map((hotellrom) =>
      hotellrom.id === oppdatertRom.id ? oppdatertRom : hotellrom))
  }

  return (
    <div className="appskall">
      <header className="toppfelt">
        <a className="merkenavn" href="/" aria-label="HotelOps startside">
          <span className="merkeikon" aria-hidden="true">H</span>
          HotelOps
        </a>
        {import.meta.env.DEV && <span className="miljømerke">Lokal utvikling</span>}
      </header>

      <main>
        <section className="sidehode" aria-labelledby="romoverskrift">
          <div>
            <p className="overlinje">Driftsoversikt</p>
            <h1 id="romoverskrift">Hotellrom</h1>
            <p className="introduksjon">
              Se hvilke rom som er klare, og hvor renhold eller vedlikehold må
              følges opp.
            </p>
          </div>
          <div className="sidehandlinger">
            <button className="nullstillknapp" type="button" disabled={laster}
              onClick={() => setForsøk((verdi) => verdi + 1)}>
              {laster ? 'Oppdaterer …' : 'Oppdater oversikten'}
            </button>
            <div className="oppdatering" aria-live="polite">
              {laster ? 'Oppdaterer oversikten …' : `${rom.length} rom registrert`}
            </div>
            <button
              className="hovedknapp"
              type="button"
              aria-expanded={viserRegistrering}
              aria-controls="registrer-rom"
              onClick={() => setViserRegistrering((vises) => !vises)}
            >
              {viserRegistrering ? 'Lukk skjema' : 'Registrer rom'}
            </button>
          </div>
        </section>

        {viserRegistrering && (
          <RegistrerRomskjema onRomRegistrert={leggTilRom} />
        )}

        {!laster && !feilmelding && rom.length > 0 && (
          <section className="nøkkeltall" aria-label="Nøkkeltall for rom">
            <div className="nøkkeltallkort">
              <span>Alle rom</span>
              <strong>{rom.length}</strong>
            </div>
            <div className="nøkkeltallkort nøkkeltallkort--positiv">
              <span>Klare for innsjekking</span>
              <strong>{antallKlare}</strong>
            </div>
            <div className="nøkkeltallkort nøkkeltallkort--advarsel">
              <span>Krever renhold</span>
              <strong>{måRengjøres}</strong>
            </div>
            <div className="nøkkeltallkort nøkkeltallkort--kritisk">
              <span>Driftsavvik</span>
              <strong>{driftsavvik}</strong>
            </div>
          </section>
        )}

        <div className="visningsvalg" role="group" aria-label="Velg visning">
          <button type="button" aria-pressed={visning === 'rom'}
            onClick={() => setVisning('rom')}>Alle rom</button>
          <button type="button" aria-pressed={visning === 'renhold'}
            onClick={() => setVisning('renhold')}>Renholdstavle</button>
        </div>

        {laster && (
          <section className="beskjed" aria-live="polite">
            <span className="lasteindikator" aria-hidden="true" />
            <h2>Henter hotellrom</h2>
            <p>Vent litt mens oversikten oppdateres.</p>
          </section>
        )}

        {!laster && feilmelding && (
          <section className="beskjed beskjed--feil" role="alert">
            <span className="beskjedikon" aria-hidden="true">!</span>
            <h2>Noe gikk galt</h2>
            <p>{feilmelding}</p>
            <button type="button" onClick={() => setForsøk((verdi) => verdi + 1)}>
              Prøv på nytt
            </button>
          </section>
        )}

        {!laster && !feilmelding && rom.length === 0 && (
          <section className="beskjed">
            <span className="beskjedikon" aria-hidden="true">0</span>
            <h2>Ingen rom er registrert</h2>
            <p>Rom vises her når de er lagt inn i databasen.</p>
          </section>
        )}

        {!laster && !feilmelding && rom.length > 0 && visning === 'renhold' && (
          <Renholdstavle rom={rom} onEndreRengjøringsstatus={oppdaterRengjøringsstatus}
            onPlanleggRenhold={oppdaterRenholdsplan} />
        )}

        {!laster && !feilmelding && rom.length > 0 && visning === 'rom' && (
          <section className="romfiltre" aria-label="Søk og filtrer rom">
            <div className="skjemafelt">
              <label htmlFor="romsøk">Søk etter romnummer</label>
              <input id="romsøk" type="search" placeholder="For eksempel 101"
                value={søk} onChange={(hendelse) => setSøk(hendelse.target.value)} />
            </div>
            <div className="skjemafelt">
              <label htmlFor="etasjefilter">Etasje</label>
              <select id="etasjefilter" value={etasje}
                onChange={(hendelse) => setEtasje(hendelse.target.value)}>
                <option value="alle">Alle etasjer</option>
                {etasjer.map((nummer) => (
                  <option key={nummer} value={nummer}>Etasje {nummer}</option>
                ))}
              </select>
            </div>
            <div className="skjemafelt">
              <label htmlFor="statusfilter">Vis rom</label>
              <select id="statusfilter" value={statusfilter}
                onChange={(hendelse) => setStatusfilter(hendelse.target.value)}>
                <option value="alle">Alle statuser</option>
                <option value="klare">Klare for innsjekking</option>
                <option value="renhold">Krever renhold</option>
                <option value="drift">Driftsavvik</option>
              </select>
            </div>
            <button className="nullstillknapp" type="button" onClick={nullstillFiltre}
              disabled={!søk && etasje === 'alle' && statusfilter === 'alle'}>
              Nullstill filtre
            </button>
            <p className="filterresultat" role="status">
              Viser {synligeRom.length} av {rom.length} rom. Nøkkeltallene gjelder alle rom.
            </p>
          </section>
        )}

        {!laster && !feilmelding && rom.length > 0 && synligeRom.length === 0 && visning === 'rom' && (
          <section className="beskjed">
            <h2>Ingen rom passer søket</h2>
            <p>Prøv et annet romnummer eller endre filtrene.</p>
            <button type="button" onClick={nullstillFiltre}>Vis alle rom</button>
          </section>
        )}

        {!laster && !feilmelding && synligeRom.length > 0 && visning === 'rom' && (
          <section className="romrutenett" aria-label="Romoversikt">
            {synligeRom.map((hotellrom) => (
              <Romkort
                key={hotellrom.id}
                rom={hotellrom}
                onEndreRengjøringsstatus={oppdaterRengjøringsstatus}
              />
            ))}
          </section>
        )}
      </main>
    </div>
  )
}
