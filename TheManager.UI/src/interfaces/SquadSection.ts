import type { SubGroup } from './SubGroup'

export interface SquadSection {
  label: string
  headerClass: string
  textClass: string
  subGroups: SubGroup[]
}
