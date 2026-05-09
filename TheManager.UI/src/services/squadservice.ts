import type { ISquadService } from '@/interfaces/ISquadService'
import type { Player } from '@/interfaces/Player'

const BASE_URL = '/api'

export class SquadService implements ISquadService {
  async getSquad(): Promise<Player[]> {
    const response = await fetch(`${BASE_URL}/squad`)
    if (!response.ok) throw new Error(`Failed to fetch squad: ${response.status}`)
    return response.json() as Promise<Player[]>
  }
}
