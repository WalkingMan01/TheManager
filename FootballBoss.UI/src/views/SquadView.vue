<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import type { Player } from '../interfaces/Player'
import type { SquadSection } from '../interfaces/SquadSection'
//import { getSquad } from '../services/squadservice'
import type { ISquadService } from '@/interfaces/ISquadService'
import { MockSquadService } from '@/mocks/mocksquadservice'
import { SquadService } from '@/services/squadservice'

// const MOCK_PLAYERS: Player[] = [
//   { id:  1, slot:  1, name: 'Clemens',  position: 'GK',  skill: 8.4, age: 29, temper: 2 },
//   { id:  2, slot:  2, name: 'Hansen',   position: 'DEF', skill: 7.8, age: 27, temper: 3 },
//   { id:  3, slot:  3, name: 'Wright',   position: 'DEF', skill: 7.2, age: 24, temper: 5 },
//   { id:  4, slot:  4, name: 'Adams',    position: 'DEF', skill: 8.1, age: 31, temper: 4 },
//   { id:  5, slot:  5, name: 'Pearce',   position: 'DEF', skill: 6.9, age: 26, temper: 8 },
//   { id:  6, slot:  6, name: 'Robson',   position: 'MID', skill: 9.8, age: 32, temper: 3 },
//   { id:  7, slot:  7, name: 'McAlstr',  position: 'MID', skill: 7.5, age: 25, temper: 2 },
//   { id:  8, slot:  8, name: 'Ince',     position: 'MID', skill: 7.1, age: 22, temper: 7 },
//   { id:  9, slot:  9, name: 'Shearer',  position: 'ATK', skill: 9.1, age: 21, temper: 2 },
//   { id: 10, slot: 10, name: 'Fowler',   position: 'ATK', skill: 8.3, age: 19, temper: 3 },
//   { id: 11, slot: 11, name: 'Cole',     position: 'ATK', skill: 7.8, age: 23, temper: 4 },
//   { id: 12, slot: 12, name: 'Jones',    position: 'MID', skill: 6.5, age: 24, temper: 3 },
//   { id: 13, slot: 13, name: 'Seaman',   position: 'GK',  skill: 7.0, age: 26, temper: 1 },
//   { id: 14, slot: 14, name: 'Southgte', position: 'DEF', skill: 6.8, age: 24, temper: 2 },
//   { id: 15, slot: 15, name: 'Bould',    position: 'DEF', skill: 6.2, age: 28, temper: 3 },
//   { id: 16, slot: 16, name: 'Thomas',   position: 'MID', skill: 5.8, age: 30, temper: 5 },
//   { id: 17, slot: 17, name: 'Platt',    position: 'MID', skill: 6.5, age: 27, temper: 2 },
//   { id: 18, slot: 18, name: 'Harford',  position: 'ATK', skill: 5.9, age: 32, temper: 6 },
//   { id: 19, slot: 19, name: 'Quinn',    position: 'ATK', skill: 6.1, age: 25, temper: 4 },
//   { id: 20, slot: 20, name: 'Scales',   position: 'DEF', skill: 5.5, age: 22, temper: 1 },
// ]

const selectedPlayer = ref<Player | null>(null)
const checkedIds = ref<number[]>([])
const players = ref<Player[]>([])

onMounted(async () => {

  const useMock = 'true'; //import.meta.env.VITE_USE_MOCK === 'true';
  console.log("useMock: " + useMock);
  const squadService: ISquadService = useMock ? new MockSquadService() : new SquadService();

  players.value = await squadService.getSquad();

  // try {
  //   players.value = await squadService.getSquad()
  // } catch {
  //   players.value = MOCK_PLAYERS
  // }
})

const allChecked = computed(() =>
  players.value.length > 0 && players.value.every(p => checkedIds.value.includes(p.id))
)
const someChecked = computed(() => checkedIds.value.length > 0)

function toggleCheck(player: Player) {
  const idx = checkedIds.value.indexOf(player.id)
  if (idx >= 0) checkedIds.value.splice(idx, 1)
  else checkedIds.value.push(player.id)
}
function toggleAll() {
  checkedIds.value = allChecked.value ? [] : players.value.map(p => p.id)
}
function clearChecked() {
  checkedIds.value = []
}

const firstTeam      = computed(() => players.value.filter(p => p.slot >= 1  && p.slot <= 11).sort((a, b) => a.slot - b.slot))
const subPlayer      = computed(() => players.value.filter(p => p.slot === 12))
const reservePlayers = computed(() => players.value.filter(p => p.slot >= 13).sort((a, b) => a.slot - b.slot))

const goalkeepers = computed(() => firstTeam.value.filter(p => p.position === 'GK'))
const defenders   = computed(() => firstTeam.value.filter(p => p.position === 'DEF'))
const midfielders = computed(() => firstTeam.value.filter(p => p.position === 'MID'))
const attackers   = computed(() => firstTeam.value.filter(p => p.position === 'ATK'))

const squadSections = computed<SquadSection[]>(() => [
  {
    label: '⚽  First Team',
    headerClass: 'bg-emerald-50 border-emerald-100',
    textClass: 'text-emerald-700',
    subGroups: [
      { label: 'Goalkeeper',  players: goalkeepers.value },
      { label: 'Defenders',   players: defenders.value   },
      { label: 'Midfielders', players: midfielders.value },
      { label: 'Attackers',   players: attackers.value   },
    ].filter(g => g.players.length > 0),
  },
  {
    label: 'Substitute',
    headerClass: 'bg-sky-50 border-sky-100',
    textClass: 'text-sky-700',
    subGroups: [{ label: null, players: subPlayer.value }],
  },
  {
    label: 'Reserves',
    headerClass: 'bg-slate-100 border-slate-200',
    textClass: 'text-slate-500',
    subGroups: [{ label: null, players: reservePlayers.value }],
  },
])

const colCount = 8

const positionMap: Record<string, string> = {
  GK: 'bg-yellow-100 text-yellow-800', DEF: 'bg-blue-100 text-blue-800',
  MID: 'bg-violet-100 text-violet-800', ATK: 'bg-red-100 text-red-800',
}
const positionBgMap: Record<string, string> = {
  GK: 'bg-yellow-500', DEF: 'bg-blue-600', MID: 'bg-violet-600', ATK: 'bg-red-600',
}

const positionClass   = (pos: string) => positionMap[pos]   ?? 'bg-slate-100 text-slate-700'
const positionBgClass = (pos: string) => positionBgMap[pos] ?? 'bg-slate-500'

function skillBarColor(skill: number) {
  if (skill >= 9)   return 'bg-yellow-400'
  if (skill >= 7.5) return 'bg-emerald-500'
  if (skill >= 6)   return 'bg-blue-400'
  return 'bg-slate-400'
}
function temperColor(t: number) {
  if (t >= 7) return 'text-red-600'
  if (t >= 5) return 'text-amber-500'
  return 'text-slate-400'
}

function toggleSelect(player: Player) {
  selectedPlayer.value = selectedPlayer.value?.id === player.id ? null : player
}
function promoteToSub(player: Player) {
  const sub = players.value.find(p => p.slot === 12)
  if (sub) {
    const old = player.slot
    player.slot = 12
    sub.slot = old
  }
  selectedPlayer.value = null
}
function benchPlayer(player: Player) {
  const slots = players.value.filter(p => p.slot >= 13).map(p => p.slot)
  const maxSlot = slots.length ? Math.max(...slots) : 12
  if (maxSlot < 20) player.slot = maxSlot + 1
  selectedPlayer.value = null
}
</script>

<template>
  <div class="px-6 pt-1 pb-7">

    <!-- Page Header -->
    <div class="mb-3">
      <h2 class="text-xs font-medium text-slate-500">Squad Management</h2>
    </div>

    <!-- Squad Table Card -->
    <div class="bg-white rounded-xl border border-slate-200 shadow-sm overflow-hidden">

      <!-- Bulk Action Bar -->
      <transition name="fade">
        <div v-if="someChecked" class="flex items-center justify-between px-4 py-2.5 bg-blue-50 border-b border-blue-100">
          <span class="text-sm font-medium text-blue-800">
            {{ checkedIds.length }} player{{ checkedIds.length !== 1 ? 's' : '' }} selected
          </span>
          <div class="flex items-center gap-2">
            <button class="px-3 py-1.5 text-xs font-semibold text-red-600 border border-red-200 bg-white rounded-lg hover:bg-red-50 transition-colors">
              Transfer List
            </button>
            <button class="px-3 py-1.5 text-xs font-semibold text-slate-700 border border-slate-200 bg-white rounded-lg hover:bg-slate-50 transition-colors">
              Offer for Sale
            </button>
            <button @click="clearChecked" class="px-3 py-1.5 text-xs font-semibold text-slate-500 hover:text-slate-700 transition-colors">
              Clear
            </button>
          </div>
        </div>
      </transition>

      <!-- Table -->
      <div class="overflow-x-auto">
        <table class="w-full text-sm">
          <thead>
            <tr class="bg-slate-50 border-b border-slate-200">
              <th class="pl-4 pr-2 py-2 w-10">
                <input
                  type="checkbox"
                  :checked="allChecked"
                  @change="toggleAll"
                  class="h-4 w-4 rounded border-slate-300 cursor-pointer accent-slate-800"
                >
              </th>
              <th class="pl-2 pr-2 py-2 text-left text-xs font-semibold text-slate-400 uppercase tracking-wider w-8">#</th>
              <th class="px-3 py-2 text-left text-xs font-semibold text-slate-400 uppercase tracking-wider">Name</th>
              <th class="px-3 py-2 text-left text-xs font-semibold text-slate-400 uppercase tracking-wider">Pos</th>
              <th class="px-3 py-2 text-left text-xs font-semibold text-slate-400 uppercase tracking-wider">Skill</th>
              <th class="px-3 py-2 text-left text-xs font-semibold text-slate-400 uppercase tracking-wider">Age</th>
              <th class="px-3 py-2 text-right text-xs font-semibold text-slate-400 uppercase tracking-wider">Tmp</th>
              <th class="px-3 pr-4 py-2 w-24"></th>
            </tr>
          </thead>
          <tbody>
            <template v-for="section in squadSections" :key="section.label">
              <tr>
                <td :colspan="colCount" :class="['px-4 py-1 border-y', section.headerClass]">
                  <span :class="['text-xs font-bold uppercase tracking-widest', section.textClass]">
                    {{ section.label }}
                  </span>
                </td>
              </tr>
              <template v-for="group in section.subGroups" :key="group.label ?? 'default'">
                <tr v-if="group.label">
                  <td :colspan="colCount" class="px-4 py-1 bg-slate-50 border-b border-slate-100">
                    <span class="text-xs font-semibold text-slate-400 uppercase tracking-widest">{{ group.label }}</span>
                  </td>
                </tr>
                <tr
                  v-for="player in group.players"
                  :key="player.id"
                  @click="toggleSelect(player)"
                  :class="[
                    'border-b border-slate-100 cursor-pointer transition-colors',
                    checkedIds.includes(player.id)   ? 'bg-blue-50' :
                    selectedPlayer?.id === player.id ? 'bg-blue-50/50' :
                                                       'hover:bg-slate-50',
                  ]"
                >
                  <td class="pl-4 pr-2 py-[5px]" @click.stop>
                    <input
                      type="checkbox"
                      :checked="checkedIds.includes(player.id)"
                      @change="toggleCheck(player)"
                      class="h-4 w-4 rounded border-slate-300 cursor-pointer accent-slate-800"
                    >
                  </td>
                  <td class="pl-2 pr-2 py-[5px] text-slate-400 text-xs tabular-nums">{{ player.slot }}</td>
                  <td class="px-3 py-[5px]">
                    <span :class="['font-medium', player.slot <= 11 ? 'text-slate-900' : 'text-slate-700']">
                      {{ player.name }}
                    </span>
                  </td>
                  <td class="px-3 py-[5px]">
                    <span :class="['inline-flex px-2 py-0.5 rounded-full text-xs font-semibold', positionClass(player.position)]">
                      {{ player.position }}
                    </span>
                  </td>
                  <td class="px-3 py-[5px]">
                    <div class="flex items-center gap-2">
                      <span class="font-mono font-semibold text-slate-800 tabular-nums w-8">{{ player.skill.toFixed(1) }}</span>
                      <div class="flex gap-0.5">
                        <div
                          v-for="i in 10"
                          :key="i"
                          :class="['w-1.5 h-2 rounded-sm', i <= Math.floor(player.skill) ? skillBarColor(player.skill) : 'bg-slate-200']"
                        ></div>
                      </div>
                    </div>
                  </td>
                  <td class="px-3 py-3 text-slate-600 tabular-nums">{{ Math.abs(player.age) }}</td>
                  <td class="px-3 py-3 text-right">
                    <span :class="['font-semibold text-xs tabular-nums', temperColor(player.temper)]">{{ player.temper }}</span>
                  </td>
                  <td class="px-3 pr-4 py-[5px] text-right">
                    <button
                      v-if="player.slot >= 13"
                      @click.stop="promoteToSub(player)"
                      class="text-xs font-medium text-emerald-600 hover:text-emerald-800 px-2 py-1 rounded hover:bg-emerald-50 transition-colors"
                    >
                      Promote
                    </button>
                    <button
                      v-else-if="player.slot <= 11"
                      @click.stop="benchPlayer(player)"
                      class="text-xs font-medium text-slate-400 hover:text-slate-700 px-2 py-1 rounded hover:bg-slate-100 transition-colors"
                    >
                      Bench
                    </button>
                    <button
                      v-else
                      class="text-xs font-medium text-slate-400 hover:text-slate-700 px-2 py-1 rounded hover:bg-slate-100 transition-colors"
                    >
                      ···
                    </button>
                  </td>
                </tr>
              </template>
            </template>
          </tbody>
        </table>
      </div>
    </div>

    <!-- Player Detail Panel -->
    <transition name="fade">
      <div v-if="selectedPlayer" class="mt-4 bg-white rounded-xl border border-slate-200 shadow-sm p-5">
        <div class="flex items-start justify-between mb-5">
          <div class="flex items-center gap-3">
            <div :class="['w-12 h-12 rounded-xl flex items-center justify-center text-white font-bold', positionBgClass(selectedPlayer.position)]">
              {{ selectedPlayer.position }}
            </div>
            <div>
              <h2 class="text-lg font-bold text-slate-900">{{ selectedPlayer.name }}</h2>
              <p class="text-sm text-slate-500">{{ selectedPlayer.position }} · Squad slot {{ selectedPlayer.slot }}</p>
            </div>
          </div>
          <button
            @click="selectedPlayer = null"
            class="text-slate-400 hover:text-slate-600 p-1 rounded-lg hover:bg-slate-100 transition-colors"
          >
            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24" stroke-width="2">
              <path stroke-linecap="round" stroke-linejoin="round" d="M6 18L18 6M6 6l12 12"/>
            </svg>
          </button>
        </div>

        <div class="grid grid-cols-3 gap-3">
          <div class="bg-slate-50 rounded-lg p-3">
            <p class="text-xs text-slate-500 mb-1">Skill</p>
            <p class="text-xl font-bold text-slate-900 tabular-nums">{{ selectedPlayer.skill.toFixed(1) }}</p>
          </div>
          <div class="bg-slate-50 rounded-lg p-3">
            <p class="text-xs text-slate-500 mb-1">Age</p>
            <p class="text-xl font-bold text-slate-900 tabular-nums">{{ Math.abs(selectedPlayer.age) }}</p>
          </div>
          <div class="bg-slate-50 rounded-lg p-3">
            <p class="text-xs text-slate-500 mb-1">Temper</p>
            <p :class="['text-xl font-bold tabular-nums', temperColor(selectedPlayer.temper)]">
              {{ selectedPlayer.temper }}<span class="text-sm text-slate-400">/9</span>
            </p>
          </div>
        </div>

        <div class="flex gap-2 mt-4">
          <button class="px-3.5 py-2 text-xs font-semibold text-red-600 border border-red-200 rounded-lg hover:bg-red-50 transition-colors">
            Transfer List
          </button>
          <button class="px-3.5 py-2 text-xs font-semibold text-slate-600 border border-slate-200 rounded-lg hover:bg-slate-50 transition-colors">
            Scout Report
          </button>
        </div>
      </div>
    </transition>

  </div>
</template>

<style scoped>
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.15s, transform 0.15s;
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}
</style>
