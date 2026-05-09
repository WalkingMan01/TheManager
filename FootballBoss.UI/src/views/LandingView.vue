<script setup lang="ts">
const hasSavedGame = !!localStorage.getItem('footballboss-save')

const emit = defineEmits<{
  newGame: []
  continueGame: []
}>()
</script>

<template>
  <div class="landing">
    <div class="brand">
      <div class="brand-icon">
        <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" stroke-width="2">
          <circle cx="12" cy="12" r="10"/>
          <path stroke-linecap="round" stroke-linejoin="round" d="M12 8v4l3 3"/>
        </svg>
      </div>
      <h1 class="brand-name">FootballBoss</h1>
    </div>

    <div class="cards">
      <button class="card" @click="emit('newGame')">
        <div class="card-icon card-icon--green">
          <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" stroke-width="2">
            <circle cx="12" cy="12" r="10"/>
            <path stroke-linecap="round" stroke-linejoin="round" d="M12 8v8M8 12h8"/>
          </svg>
        </div>
        <p class="card-title">New Game</p>
        <p class="card-desc">Start a fresh career as a football manager</p>
      </button>

      <button
        class="card"
        :class="{ 'card--disabled': !hasSavedGame }"
        :disabled="!hasSavedGame"
        :title="hasSavedGame ? '' : 'No saved game found'"
        @click="emit('continueGame')"
      >
        <div class="card-icon card-icon--slate">
          <svg fill="none" stroke="currentColor" viewBox="0 0 24 24" stroke-width="2">
            <path stroke-linecap="round" stroke-linejoin="round" d="M5 12h14M13 6l6 6-6 6"/>
          </svg>
        </div>
        <p class="card-title">Continue Game</p>
        <p class="card-desc">{{ hasSavedGame ? 'Resume your saved career' : 'No saved game found' }}</p>
      </button>
    </div>

    <p class="version">Football Boss v0.1</p>
  </div>
</template>

<style scoped>
.landing {
  width: 100%;
  min-height: 100svh;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  background: #f8fafc;
  gap: 32px;
}

/* ── Brand ───────────────────────────────────────────────────────────────── */

.brand {
  display: flex;
  align-items: center;
  gap: 10px;
}

.brand-icon {
  width: 36px;
  height: 36px;
  border-radius: 8px;
  background: #15803d;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #fff;
  flex-shrink: 0;
}

.brand-icon svg {
  width: 18px;
  height: 18px;
}

.brand-name {
  font-size: 22px;
  font-weight: 700;
  color: #0f172a;
  margin: 0;
  letter-spacing: -0.3px;
}

/* ── Cards ───────────────────────────────────────────────────────────────── */

.cards {
  display: flex;
  gap: 16px;
}

.card {
  width: 200px;
  padding: 28px 20px;
  background: #fff;
  border: 1px solid #e2e8f0;
  border-radius: 16px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  cursor: pointer;
  transition: border-color 0.15s, box-shadow 0.15s;
  text-align: center;
}

.card:hover:not(:disabled) {
  border-color: #94a3b8;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);
}

.card--disabled {
  opacity: 0.45;
  cursor: not-allowed;
}

.card-icon {
  width: 48px;
  height: 48px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.card-icon svg {
  width: 24px;
  height: 24px;
}

.card-icon--green {
  background: #dcfce7;
  color: #15803d;
}

.card-icon--slate {
  background: #f1f5f9;
  color: #475569;
}

.card-title {
  font-size: 15px;
  font-weight: 700;
  color: #0f172a;
  margin: 0;
}

.card-desc {
  font-size: 12px;
  color: #64748b;
  margin: 0;
  line-height: 1.4;
}

/* ── Footer ──────────────────────────────────────────────────────────────── */

.version {
  position: absolute;
  bottom: 16px;
  font-size: 11px;
  color: #cbd5e1;
  margin: 0;
}
</style>
