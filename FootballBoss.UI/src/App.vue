<script setup lang="ts">
import { ref, computed } from 'vue'
import SquadView from './views/SquadView.vue'
import LandingView from './views/LandingView.vue'

type GamePhase = 'landing' | 'in-game'
const gamePhase = ref<GamePhase>('landing')

function startNewGame() {
  localStorage.removeItem('footballboss-save')
  gamePhase.value = 'in-game'
}

function continueGame() {
  gamePhase.value = 'in-game'
}

const activeNav = ref('squad')

const gameNavItems = [
  { id: 'dashboard', label: 'Dashboard' },
  { id: 'squad',     label: 'Squad'     },
  { id: 'transfers', label: 'Transfers' },
  { id: 'finance',   label: 'Finance'   },
  { id: 'fixtures',  label: 'Fixtures'  },
  { id: 'training',  label: 'Training'  },
]

const systemNavItems = [
  { id: 'new-game', label: 'New Game' },
  { id: 'quit',     label: 'Quit'     },
]

const isInGame = computed(() => !['new-game', 'quit'].includes(activeNav.value))
</script>

<template>
  <LandingView v-if="gamePhase === 'landing'" @new-game="startNewGame" @continue-game="continueGame" />
  <div v-else class="app-layout">
    <nav class="sidebar">

      <!-- Header -->
      <div class="sidebar-header">
        <div class="brand">
          <div class="brand-icon">
            <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" stroke-width="2">
              <circle cx="12" cy="12" r="10"/>
              <path stroke-linecap="round" stroke-linejoin="round" d="M12 8v4l3 3"/>
            </svg>
          </div>
          <span class="brand-name">FootballBoss</span>
        </div>
        <div v-if="isInGame" class="club-info">
          <p class="club-name">Anytown FC</p>
          <p class="club-meta">Div 1 · Week 23 of 46</p>
        </div>
      </div>

      <div class="sidebar-divider"></div>

      <!-- In-game navigation -->
      <div v-if="isInGame" class="nav-section">
        <p class="nav-section-label">Manage</p>
        <ul class="nav-list">
          <li
            v-for="item in gameNavItems"
            :key="item.id"
            :class="['nav-item', { active: activeNav === item.id }]"
            @click="activeNav = item.id"
          >
            {{ item.label }}
          </li>
        </ul>
      </div>

      <div class="sidebar-divider"></div>

      <!-- System navigation -->
      <div class="nav-section">
        <p class="nav-section-label">Game</p>
        <ul class="nav-list">
          <li
            v-for="item in systemNavItems"
            :key="item.id"
            :class="['nav-item', { active: activeNav === item.id }]"
            @click="activeNav = item.id"
          >
            {{ item.label }}
          </li>
        </ul>
      </div>

      <!-- Footer -->
      <div class="sidebar-footer">
        <p class="version">Football Boss v0.1</p>
      </div>
    </nav>

    <main class="main-content">
      <SquadView v-if="activeNav === 'squad'" />
      <div v-else class="placeholder">
        <p class="placeholder-title">
          {{ gameNavItems.find(i => i.id === activeNav)?.label ?? systemNavItems.find(i => i.id === activeNav)?.label }}
        </p>
        <p class="placeholder-sub">Coming soon</p>
      </div>
    </main>
  </div>
</template>

<style scoped>
.app-layout {
  display: flex;
  min-height: 100svh;
  width: 100%;
}

/* ── Sidebar ─────────────────────────────────────────────────────────────── */

.sidebar {
  width: 200px;
  flex-shrink: 0;
  background: #fff;
  border-right: 1px solid #e2e8f0;
  display: flex;
  flex-direction: column;
}

.sidebar-header {
  padding: 16px 16px 14px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.brand {
  display: flex;
  align-items: center;
  gap: 8px;
}

.brand-icon {
  width: 28px;
  height: 28px;
  border-radius: 6px;
  background: #15803d;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
  color: #fff;
}

.brand-icon svg {
  width: 15px;
  height: 15px;
}

.brand-name {
  font-size: 13px;
  font-weight: 700;
  color: #0f172a;
  letter-spacing: -0.1px;
}

.club-info {
  padding: 8px 10px;
  background: #f8fafc;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
}

.club-name {
  font-size: 13px;
  font-weight: 600;
  color: #0f172a;
  margin: 0;
  line-height: 1.3;
}

.club-meta {
  font-size: 11px;
  color: #64748b;
  margin: 2px 0 0;
}

.sidebar-divider {
  height: 1px;
  background: #e2e8f0;
  margin: 0;
}

/* ── Nav sections ────────────────────────────────────────────────────────── */

.nav-section {
  padding: 10px 8px 4px;
}

.nav-section-label {
  font-size: 10px;
  font-weight: 600;
  letter-spacing: 0.08em;
  text-transform: uppercase;
  color: #94a3b8;
  margin: 0 0 4px;
  padding: 0 8px;
}

.nav-list {
  list-style: none;
  margin: 0;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 1px;
}

.nav-item {
  padding: 7px 10px;
  border-radius: 6px;
  cursor: pointer;
  font-size: 13px;
  font-weight: 500;
  color: #64748b;
  transition: background 0.1s, color 0.1s;
}

.nav-item:hover {
  background: #f8fafc;
  color: #0f172a;
}

.nav-item.active {
  background: #f1f5f9;
  color: #0f172a;
  font-weight: 600;
}

/* ── Sidebar footer ──────────────────────────────────────────────────────── */

.sidebar-footer {
  margin-top: auto;
  padding: 12px 16px;
  border-top: 1px solid #e2e8f0;
}

.version {
  font-size: 10px;
  color: #cbd5e1;
  margin: 0;
}

/* ── Main content ────────────────────────────────────────────────────────── */

.main-content {
  flex: 1;
  overflow-y: auto;
  background: #f8fafc;
  min-width: 0;
}

.placeholder {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  height: 100%;
  gap: 8px;
}

.placeholder-title {
  font-size: 20px;
  font-weight: 600;
  color: #94a3b8;
  margin: 0;
}

.placeholder-sub {
  font-size: 13px;
  color: #cbd5e1;
  margin: 0;
}
</style>
