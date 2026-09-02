export type Beleggsstatus = 'Ledig' | 'Opptatt'

export type Rengjøringsstatus = 'Ren' | 'Skitten' | 'UnderRengjøring'

export type Driftsstatus = 'Operativ' | 'UnderVedlikehold' | 'UteAvDrift'

export interface Rom {
  id: string
  nummer: string
  etasje: number
  beleggsstatus: Beleggsstatus
  rengjøringsstatus: Rengjøringsstatus
  driftsstatus: Driftsstatus
  erKlartForInnsjekking: boolean
}
