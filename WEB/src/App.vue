<template>
  <router-view />
  <SessionExpiryWarning v-if="auth.isAuthenticated" />
</template>

<script setup>
/*
 * Root component (WO-94 Step 9). Layouts resolve per-route via the router.
 * On mount it initializes the auth store (re-hydrates permissions from any
 * persisted token) and mounts the session-expiry warning for signed-in users.
 */
import { onMounted } from 'vue'
import { useAuthStore } from 'stores/auth'
import SessionExpiryWarning from 'shared/session_expiry_warning.vue'

const auth = useAuthStore()

onMounted(() => {
  auth.initialize()
})
</script>
