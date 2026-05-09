import type { ISquadService } from "@/interfaces/ISquadService";
import type { Player } from "@/interfaces/Player";

const MOCK_PLAYERS: Player[] = [
  { id:  1, slot:  1, name: 'Clemens',  position: 'GK',  skill: 8.4, age: 29, temper: 2 },
  { id:  2, slot:  2, name: 'Hansen',   position: 'DEF', skill: 7.8, age: 27, temper: 3 },
  { id:  3, slot:  3, name: 'Wright',   position: 'DEF', skill: 7.2, age: 24, temper: 5 },
  { id:  4, slot:  4, name: 'Adams',    position: 'DEF', skill: 8.1, age: 31, temper: 4 },
  { id:  5, slot:  5, name: 'Pearce',   position: 'DEF', skill: 6.9, age: 26, temper: 8 },
  { id:  6, slot:  6, name: 'Robson',   position: 'MID', skill: 9.8, age: 32, temper: 3 },
  { id:  7, slot:  7, name: 'McAlstr',  position: 'MID', skill: 7.5, age: 25, temper: 2 },
  { id:  8, slot:  8, name: 'Ince',     position: 'MID', skill: 7.1, age: 22, temper: 7 },
  { id:  9, slot:  9, name: 'Shearer',  position: 'ATK', skill: 9.1, age: 21, temper: 2 },
  { id: 10, slot: 10, name: 'Fowler',   position: 'ATK', skill: 8.3, age: 19, temper: 3 },
  { id: 11, slot: 11, name: 'Cole',     position: 'ATK', skill: 7.8, age: 23, temper: 4 },
  { id: 12, slot: 12, name: 'Jones',    position: 'MID', skill: 6.5, age: 24, temper: 3 },
  { id: 13, slot: 13, name: 'Seaman',   position: 'GK',  skill: 7.0, age: 26, temper: 1 },
  { id: 14, slot: 14, name: 'Southgte', position: 'DEF', skill: 6.8, age: 24, temper: 2 },
  { id: 15, slot: 15, name: 'Bould',    position: 'DEF', skill: 6.2, age: 28, temper: 3 },
  { id: 16, slot: 16, name: 'Thomas',   position: 'MID', skill: 5.8, age: 30, temper: 5 },
  { id: 17, slot: 17, name: 'Platt',    position: 'MID', skill: 6.5, age: 27, temper: 2 },
  { id: 18, slot: 18, name: 'Harford',  position: 'ATK', skill: 5.9, age: 32, temper: 6 },
  { id: 19, slot: 19, name: 'Quinn',    position: 'ATK', skill: 6.1, age: 25, temper: 4 },
  { id: 20, slot: 20, name: 'Scales',   position: 'DEF', skill: 5.5, age: 22, temper: 1 },
]

export class MockSquadService implements ISquadService {
  async getSquad(): Promise<Player[]> {    
    return MOCK_PLAYERS
  }
}