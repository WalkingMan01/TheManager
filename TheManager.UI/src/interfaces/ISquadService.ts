import type { Player } from "./Player";

// services/ISquadService.ts
export interface ISquadService {
  getSquad(): Promise<Player[]>;
}