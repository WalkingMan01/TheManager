export interface Player {
  id: number
  slot: number
  name: string
  position: 'GK' | 'DEF' | 'MID' | 'ATK'
  skill: number
  age: number
  temper: number
}
