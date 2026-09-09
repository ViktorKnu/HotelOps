import type { Rom } from '../typer/rom'

export interface Renholdsfiltre {
  etasje: string
  ansvarlig: string
  prioritet: string
}

export function velgRenholdsrom(rom: Rom[], filtre: Renholdsfiltre): Rom[] {
  return rom
    .filter((rom) => rom.rengjøringsstatus !== 'Ren')
    .filter((rom) => filtre.etasje === 'alle' || String(rom.etasje) === filtre.etasje)
    .filter((rom) => filtre.ansvarlig === 'alle'
      || (filtre.ansvarlig === 'utildelt' && !rom.ansvarligRenholder)
      || (!!rom.ansvarligRenholder && filtre.ansvarlig === `navn:${rom.ansvarligRenholder}`))
    .filter((rom) => filtre.prioritet === 'alle' || rom.renholdsprioritet === filtre.prioritet)
    .sort((a, b) => Number(b.renholdsprioritet === 'Haster') - Number(a.renholdsprioritet === 'Haster')
      || a.etasje - b.etasje || a.nummer.localeCompare(b.nummer, 'nb', { numeric: true }))
}
