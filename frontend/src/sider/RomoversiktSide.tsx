import { useEffect, useState } from 'react'
import { Romkort } from '../komponenter/Romkort'
import { hentRom } from '../tjenester/romtjeneste'
import type { Rom } from '../typer/rom'

export function RomoversiktSide() {
  const [rom, setRom] = useState<Rom[]>([])
  const [laster, setLaster] = useState(true)
  const [feilmelding, setFeilmelding] = useState<string | null>(null)
  const [forsøk, setForsøk] = useState(0)

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
          <div className="oppdatering" aria-live="polite">
            {laster ? 'Oppdaterer oversikten …' : `${rom.length} rom registrert`}
          </div>
        </section>

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

        {!laster && !feilmelding && rom.length > 0 && (
          <section className="romrutenett" aria-label="Romoversikt">
            {rom.map((hotellrom) => (
              <Romkort key={hotellrom.id} rom={hotellrom} />
            ))}
          </section>
        )}
      </main>
    </div>
  )
}
