<template>
  <q-dialog v-model="visible" persistent>
    <q-card style="min-width: 340px; max-width: 90vw">
      <q-card-section class="row items-center no-wrap">
        <q-icon name="o_schedule" color="warning" size="28px" class="q-mr-sm" />
        <div class="text-h6">Session expiring</div>
      </q-card-section>

      <q-card-section class="q-pt-none text-body2">
        Your session will expire in
        <strong>{{ countdownLabel }}</strong>. Do you want to stay signed in?
      </q-card-section>

      <q-card-actions align="right">
        <q-btn flat label="Sign out" color="grey-8" :disable="busy" @click="onSignOut" />
        <q-btn
          unelevated
          label="Stay signed in"
          color="primary"
          :loading="busy"
          @click="onStay"
        />
      </q-card-actions>
    </q-card>
  </q-dialog>
</template>

<script setup>
import { ref, computed, onMounted, onBeforeUnmount } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from 'stores/auth'

// Show the warning when this many milliseconds (or fewer) remain.
const WARN_BEFORE_MS = 2 * 60 * 1000

const auth = useAuthStore()
const router = useRouter()

const visible = ref(false)
const busy = ref(false)
const remainingMs = ref(0)
let timer = null

const countdownLabel = computed(() => {
  const totalSec = Math.max(0, Math.round(remainingMs.value / 1000))
  const m = Math.floor(totalSec / 60)
  const s = totalSec % 60
  return `${m}:${String(s).padStart(2, '0')}`
})

function expiryTime () {
  if (!auth.expiresAtUtc) return null
  const t = new Date(auth.expiresAtUtc).getTime()
  return Number.isNaN(t) ? null : t
}

function tick () {
  const exp = expiryTime()
  if (!auth.isAuthenticated || !exp) {
    remainingMs.value = 0
    visible.value = false
    return
  }
  remainingMs.value = exp - Date.now()
  if (remainingMs.value <= 0) {
    // Expired: let the next 401 drive the redirect, just hide the dialog.
    visible.value = false
  } else {
    visible.value = remainingMs.value <= WARN_BEFORE_MS
  }
}

async function onStay () {
  busy.value = true
  try {
    if (auth.refreshToken) await auth.refresh()
    visible.value = false
  } catch (e) {
    await onSignOut()
  } finally {
    busy.value = false
  }
}

async function onSignOut () {
  visible.value = false
  await auth.logout()
  router.push('/auth/login')
}

onMounted(() => {
  tick()
  timer = setInterval(tick, 1000)
})

onBeforeUnmount(() => {
  if (timer) clearInterval(timer)
})
</script>
