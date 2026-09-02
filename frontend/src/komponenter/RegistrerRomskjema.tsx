import { useState, type FormEvent } from 'react'
import { opprettRom } from '../tjenester/romtjeneste'
import type { Rom } from '../typer/rom'

interface RegistrerRomskjemaEgenskaper {
  onRomRegistrert: (rom: Rom) => void
}

export function RegistrerRomskjema({
  onRomRegistrert,
}: RegistrerRomskjemaEgenskaper) {
  const [nummer, setNummer] = useState('')
  const [etasje, setEtasje] = useState('1')
  const [sender, setSender] = useState(false)
  const [feilmelding, setFeilmelding] = useState<string | null>(null)
  const [bekreftelse, setBekreftelse] = useState<string | null>(null)

  async function håndterInnsending(hendelse: FormEvent<HTMLFormElement>) {
    hendelse.preventDefault()
    const etasjenummer = Number(etasje)

    if (!Number.isInteger(etasjenummer)) {
      setFeilmelding('Etasje må være et heltall.')
      return
    }

    setSender(true)
    setFeilmelding(null)
    setBekreftelse(null)

    try {
      const rom = await opprettRom({ nummer, etasje: etasjenummer })
      setNummer('')
      setBekreftelse(`Rom ${rom.nummer} er registrert.`)
      onRomRegistrert(rom)
    } catch (feil) {
      setFeilmelding(
        feil instanceof Error
          ? feil.message
          : 'Rommet kunne ikke registreres. Prøv igjen.',
      )
    } finally {
      setSender(false)
    }
  }

  return (
    <section className="registreringspanel" id="registrer-rom" aria-labelledby="registrer-rom-tittel">
      <div>
        <p className="overlinje">Nytt hotellrom</p>
        <h2 id="registrer-rom-tittel">Registrer rom</h2>
        <p className="registreringspanel__forklaring">
          Nye rom registreres som ledige, rene og operative.
        </p>
      </div>

      <form className="romskjema" onSubmit={håndterInnsending}>
        <div className="skjemafelt">
          <label htmlFor="romnummer">Romnummer</label>
          <input
            id="romnummer"
            name="nummer"
            value={nummer}
            onChange={(hendelse) => setNummer(hendelse.target.value)}
            placeholder="For eksempel 101"
            autoComplete="off"
            required
          />
        </div>

        <div className="skjemafelt skjemafelt--kort">
          <label htmlFor="etasje">Etasje</label>
          <input
            id="etasje"
            name="etasje"
            type="number"
            step="1"
            value={etasje}
            onChange={(hendelse) => setEtasje(hendelse.target.value)}
            required
          />
        </div>

        <button className="lagreknapp" type="submit" disabled={sender}>
          {sender ? 'Registrerer …' : 'Registrer rom'}
        </button>

        <div className="skjemamelding" aria-live="polite">
          {feilmelding && <p className="skjemamelding--feil">{feilmelding}</p>}
          {bekreftelse && <p className="skjemamelding--suksess">{bekreftelse}</p>}
        </div>
      </form>
    </section>
  )
}
