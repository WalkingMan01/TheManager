<script setup lang="ts">
import { ref, computed } from 'vue'

interface Player {
  id: number
  slot: number
  name: string
  position: 'GK' | 'DEF' | 'MID' | 'ATK'
  skill: number
  age: number
  status: string
  weeksUnavail: number
  wage: number
  contractWeeks: number
  goals: number
  apps: number
  temper: number
  transferListed: boolean
}

interface SubGroup {
  label: string | null
  players: Player[]
}

interface SquadSection {
  label: string
  headerClass: string
  textClass: string
  subGroups: SubGroup[]
}

const activeTab = ref('overview')
const selectedPlayer = ref<Player | null>(null)
const checkedIds = ref<number[]>([])

const tabs = [
  { id: 'overview',  label: 'Overview'  },
  { id: 'contracts', label: 'Contracts' },
]

const players = ref<Player[]>([
  { id:  1, slot:  1, name: 'Clemens',  position: 'GK',  skill: 8.4, age: 29, status: 'Normal',    weeksUnavail: 0, wage: 650,  contractWeeks: 52,  goals:  3, apps: 28, temper: 2, transferListed: false },
  { id:  2, slot:  2, name: 'Hansen',   position: 'DEF', skill: 7.8, age: 27, status: 'Normal',    weeksUnavail: 0, wage: 550,  contractWeeks: 78,  goals:  0, apps: 30, temper: 3, transferListed: false },
  { id:  3, slot:  3, name: 'Wright',   position: 'DEF', skill: 7.2, age: 24, status: 'Normal',    weeksUnavail: 0, wage: 450,  contractWeeks: 104, goals:  1, apps: 25, temper: 5, transferListed: false },
  { id:  4, slot:  4, name: 'Adams',    position: 'DEF', skill: 8.1, age: 31, status: 'Improving', weeksUnavail: 0, wage: 600,  contractWeeks: 26,  goals:  2, apps: 30, temper: 4, transferListed: false },
  { id:  5, slot:  5, name: 'Pearce',   position: 'DEF', skill: 6.9, age: 26, status: 'Normal',    weeksUnavail: 0, wage: 420,  contractWeeks: 52,  goals:  1, apps: 28, temper: 8, transferListed: false },
  { id:  6, slot:  6, name: 'Robson',   position: 'MID', skill: 9.8, age: 32, status: 'Star',      weeksUnavail: 0, wage: 1200, contractWeeks: 52,  goals:  8, apps: 30, temper: 3, transferListed: false },
  { id:  7, slot:  7, name: 'McAlstr',  position: 'MID', skill: 7.5, age: 25, status: 'Normal',    weeksUnavail: 0, wage: 480,  contractWeeks: 78,  goals:  4, apps: 28, temper: 2, transferListed: false },
  { id:  8, slot:  8, name: 'Ince',     position: 'MID', skill: 7.1, age: 22, status: 'Suspended', weeksUnavail: 1, wage: 380,  contractWeeks: 104, goals:  3, apps: 26, temper: 7, transferListed: false },
  { id:  9, slot:  9, name: 'Shearer',  position: 'ATK', skill: 9.1, age: 21, status: 'Normal',    weeksUnavail: 0, wage: 850,  contractWeeks: 104, goals: 18, apps: 30, temper: 2, transferListed: false },
  { id: 10, slot: 10, name: 'Fowler',   position: 'ATK', skill: 8.3, age: 19, status: 'Improving', weeksUnavail: 0, wage: 550,  contractWeeks: 78,  goals: 12, apps: 29, temper: 3, transferListed: false },
  { id: 11, slot: 11, name: 'Cole',     position: 'ATK', skill: 7.8, age: 23, status: 'Normal',    weeksUnavail: 0, wage: 500,  contractWeeks: 52,  goals:  9, apps: 30, temper: 4, transferListed: false },
  { id: 12, slot: 12, name: 'Jones',    position: 'MID', skill: 6.5, age: 24, status: 'Normal',    weeksUnavail: 0, wage: 320,  contractWeeks: 52,  goals:  2, apps: 12, temper: 3, transferListed: false },
  { id: 13, slot: 13, name: 'Seaman',   position: 'GK',  skill: 7.0, age: 26, status: 'Normal',    weeksUnavail: 0, wage: 400,  contractWeeks: 78,  goals:  0, apps:  8, temper: 1, transferListed: false },
  { id: 14, slot: 14, name: 'Southgte', position: 'DEF', skill: 6.8, age: 24, status: 'Normal',    weeksUnavail: 0, wage: 320,  contractWeeks: 52,  goals:  0, apps:  6, temper: 2, transferListed: false },
  { id: 15, slot: 15, name: 'Bould',    position: 'DEF', skill: 6.2, age: 28, status: 'Injured',   weeksUnavail: 4, wage: 280,  contractWeeks: 26,  goals:  0, apps: 10, temper: 3, transferListed: false },
  { id: 16, slot: 16, name: 'Thomas',   position: 'MID', skill: 5.8, age: 30, status: 'Normal',    weeksUnavail: 0, wage: 240,  contractWeeks:  8,  goals:  1, apps:  7, temper: 5, transferListed: false },
  { id: 17, slot: 17, name: 'Platt',    position: 'MID', skill: 6.5, age: 27, status: 'Normal',    weeksUnavail: 0, wage: 350,  contractWeeks: 20,  goals:  3, apps:  9, temper: 2, transferListed: true  },
  { id: 18, slot: 18, name: 'Harford',  position: 'ATK', skill: 5.9, age: 32, status: 'Normal',    weeksUnavail: 0, wage: 260,  contractWeeks: 30,  goals:  2, apps:  8, temper: 6, transferListed: false },
  { id: 19, slot: 19, name: 'Quinn',    position: 'ATK', skill: 6.1, age: 25, status: 'Injured',   weeksUnavail: 2, wage: 280,  contractWeeks: 52,  goals:  1, apps:  5, temper: 4, transferListed: false },
  { id: 20, slot: 20, name: 'Scales',   position: 'DEF', skill: 5.5, age: 22, status: 'Normal',    weeksUnavail: 0, wage: 220,  contractWeeks: 104, goals:  0, apps:  3, temper: 1, transferListed: false },
])

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

const isAvailable = (p: Player) =>
  ['Normal', 'Improving', 'Recovering', 'Star'].includes(p.status)

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

const colCount = computed(() => activeTab.value === 'contracts' ? 10 : 11)

const positionMap: Record<string, string> = {
  GK: 'bg-yellow-100 text-yellow-800', DEF: 'bg-blue-100 text-blue-800',
  MID: 'bg-violet-100 text-violet-800', ATK: 'bg-red-100 text-red-800',
}
const positionBgMap: Record<string, string> = {
  GK: 'bg-yellow-500', DEF: 'bg-blue-600', MID: 'bg-violet-600', ATK: 'bg-red-600',
}
const statusMap: Record<string, string> = {
  Normal:    'bg-slate-100 text-slate-600',
  Improving: 'bg-emerald-100 text-emerald-700',
  Recovering:'bg-teal-100 text-teal-700',
  Star:      'bg-yellow-100 text-yellow-800',
  Injured:   'bg-red-100 text-red-700',
  Suspended: 'bg-orange-100 text-orange-700',
  OnLoan:    'bg-sky-100 text-sky-700',
  Retiring:  'bg-purple-100 text-purple-700',
}

const positionClass  = (pos: string)    => positionMap[pos]   ?? 'bg-slate-100 text-slate-700'
const positionBgClass = (pos: string)   => positionBgMap[pos] ?? 'bg-slate-500'
const statusClass    = (status: string) => statusMap[status]  ?? 'bg-slate-100 text-slate-600'

function statusLabel(player: Player) {
  if (player.status === 'Injured')   return `Injured (${player.weeksUnavail}w)`
  if (player.status === 'Suspended') return `Suspended (${player.weeksUnavail}w)`
  return player.status
}
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
function contractColor(w: number) {
  if (w <= 12) return 'text-red-600'
  if (w <= 26) return 'text-amber-500'
  return 'text-slate-600'
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

      <!-- Tabs -->
      <div class="flex border-b border-slate-200 px-4">
        <button
          v-for="tab in tabs"
          :key="tab.id"
          @click="activeTab = tab.id"
          :class="['px-4 py-3 text-sm font-medium border-b-2 -mb-px transition-colors',
            activeTab === tab.id
              ? 'border-slate-900 text-slate-900'
              : 'border-transparent text-slate-500 hover:text-slate-700 hover:border-slate-300']"
        >
          {{ tab.label }}
        </button>
      </div>

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
              <th class="px-3 py-2 text-left text-xs font-semibold text-slate-400 uppercase tracking-wider">Status</th>
              <template v-if="activeTab === 'overview'">
                <th class="px-3 py-2 text-right text-xs font-semibold text-slate-400 uppercase tracking-wider">Goals</th>
                <th class="px-3 py-2 text-right text-xs font-semibold text-slate-400 uppercase tracking-wider">Apps</th>
                <th class="px-3 py-2 text-right text-xs font-semibold text-slate-400 uppercase tracking-wider">Tmp</th>
              </template>
              <template v-if="activeTab === 'contracts'">
                <th class="px-3 py-2 text-right text-xs font-semibold text-slate-400 uppercase tracking-wider">Wage/wk</th>
                <th class="px-3 py-2 text-right text-xs font-semibold text-slate-400 uppercase tracking-wider">Contract</th>
              </template>
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
                    checkedIds.includes(player.id)       ? 'bg-blue-50' :
                    selectedPlayer?.id === player.id     ? 'bg-blue-50/50' :
                    player.transferListed                ? 'bg-amber-50/60 hover:bg-amber-50' :
                                                          'hover:bg-slate-50',
                    !isAvailable(player) ? 'opacity-70' : '',
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
                    <div class="flex items-center gap-1.5">
                      <span :class="['font-medium', player.slot <= 11 ? 'text-slate-900' : 'text-slate-700']">
                        {{ player.name }}
                      </span>
                      <span v-if="player.status === 'Star'" class="text-yellow-500 text-sm leading-none" title="Star Player">★</span>
                      <span v-if="player.transferListed" class="inline-flex px-1.5 py-0.5 rounded text-xs font-medium bg-amber-100 text-amber-700">
                        Listed
                      </span>
                    </div>
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
                  <td class="px-3 py-[5px]">
                    <span :class="['inline-flex px-2 py-0.5 rounded-full text-xs font-medium', statusClass(player.status)]">
                      {{ statusLabel(player) }}
                    </span>
                  </td>
                  <template v-if="activeTab === 'overview'">
                    <td class="px-3 py-[5px] text-right tabular-nums text-slate-600">{{ player.goals }}</td>
                    <td class="px-3 py-[5px] text-right tabular-nums text-slate-600">{{ player.apps }}</td>
                    <td class="px-3 py-3 text-right">
                      <span :class="['font-semibold text-xs tabular-nums', temperColor(player.temper)]">{{ player.temper }}</span>
                    </td>
                  </template>
                  <template v-if="activeTab === 'contracts'">
                    <td class="px-3 py-[5px] text-right tabular-nums text-slate-600">£{{ player.wage }}</td>
                    <td class="px-3 py-3 text-right">
                      <span :class="['font-semibold text-xs tabular-nums', contractColor(player.contractWeeks)]">
                        {{ player.contractWeeks }}w
                      </span>
                    </td>
                  </template>
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
              <div class="flex items-center gap-2">
                <h2 class="text-lg font-bold text-slate-900">{{ selectedPlayer.name }}</h2>
                <span v-if="selectedPlayer.status === 'Star'" class="text-yellow-500 text-xl">★</span>
                <span v-if="selectedPlayer.transferListed" class="inline-flex px-2 py-0.5 rounded text-xs font-medium bg-amber-100 text-amber-700">
                  Transfer Listed
                </span>
              </div>
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

        <div class="grid grid-cols-8 gap-3">
          <div class="col-span-1 bg-slate-50 rounded-lg p-3">
            <p class="text-xs text-slate-500 mb-1">Skill</p>
            <p class="text-xl font-bold text-slate-900 tabular-nums">{{ selectedPlayer.skill.toFixed(1) }}</p>
          </div>
          <div class="col-span-1 bg-slate-50 rounded-lg p-3">
            <p class="text-xs text-slate-500 mb-1">Age</p>
            <p class="text-xl font-bold text-slate-900 tabular-nums">{{ Math.abs(selectedPlayer.age) }}</p>
          </div>
          <div class="col-span-1 bg-slate-50 rounded-lg p-3">
            <p class="text-xs text-slate-500 mb-1">Goals</p>
            <p class="text-xl font-bold text-slate-900 tabular-nums">{{ selectedPlayer.goals }}</p>
          </div>
          <div class="col-span-1 bg-slate-50 rounded-lg p-3">
            <p class="text-xs text-slate-500 mb-1">Apps</p>
            <p class="text-xl font-bold text-slate-900 tabular-nums">{{ selectedPlayer.apps }}</p>
          </div>
          <div class="col-span-1 bg-slate-50 rounded-lg p-3">
            <p class="text-xs text-slate-500 mb-1">Temper</p>
            <p :class="['text-xl font-bold tabular-nums', temperColor(selectedPlayer.temper)]">
              {{ selectedPlayer.temper }}<span class="text-sm text-slate-400">/9</span>
            </p>
          </div>
          <div class="col-span-1 bg-slate-50 rounded-lg p-3">
            <p class="text-xs text-slate-500 mb-1">Wage/wk</p>
            <p class="text-xl font-bold text-slate-900 tabular-nums">£{{ selectedPlayer.wage }}</p>
          </div>
          <div class="col-span-1 bg-slate-50 rounded-lg p-3">
            <p class="text-xs text-slate-500 mb-1">Contract</p>
            <p :class="['text-xl font-bold tabular-nums', contractColor(selectedPlayer.contractWeeks)]">
              {{ selectedPlayer.contractWeeks }}<span class="text-sm text-slate-400">w</span>
            </p>
          </div>
          <div class="col-span-1 bg-slate-50 rounded-lg p-3">
            <p class="text-xs text-slate-500 mb-1">Status</p>
            <span :class="['inline-flex mt-1 px-2 py-0.5 rounded-full text-xs font-medium', statusClass(selectedPlayer.status)]">
              {{ statusLabel(selectedPlayer) }}
            </span>
          </div>
        </div>

        <div class="flex gap-2 mt-4">
          <button class="px-3.5 py-2 text-xs font-semibold text-white bg-slate-800 rounded-lg hover:bg-slate-700 transition-colors">
            Re-negotiate Contract
          </button>
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
