<template>
  <q-card flat bordered class="fp-card q-pa-sm">
    <q-card-section class="text-center q-pb-none">
      <q-icon name="o_lock_reset" size="42px" color="primary" />
      <div class="text-h5 q-mt-sm">Forgot password</div>
      <div class="text-subtitle2 text-grey-7">We'll email you a link to choose a new password.</div>
    </q-card-section>

    <template v-if="!done">
      <q-form @submit.prevent="onSubmit">
        <q-card-section>
          <q-input
            v-model="email"
            type="email"
            label="Email"
            outlined
            autocomplete="username"
            :disable="loading"
            :rules="[(v) => !!v || 'Email is required']"
          >
            <template #prepend><q-icon name="o_mail" /></template>
          </q-input>
        </q-card-section>

        <q-card-actions class="q-px-md q-pb-md column q-gutter-sm">
          <q-btn type="submit" color="primary" class="full-width" label="Send reset link" unelevated :loading="loading" />
          <q-btn flat no-caps color="grey-8" class="full-width" label="Back to sign in" :to="{ name: 'login' }" />
        </q-card-actions>
      </q-form>
    </template>

    <q-card-section v-else class="text-center q-gutter-sm">
      <q-icon name="o_outgoing_mail" color="positive" size="48px" />
      <div class="text-subtitle1 text-weight-medium">Check your inbox</div>
      <div class="text-grey-7">
        If an account exists for <strong>{{ email }}</strong>, a password-reset link is on its way. The link expires in 1 hour.
      </div>
      <q-btn color="primary" flat no-caps label="Back to sign in" :to="{ name: 'login' }" />
    </q-card-section>
  </q-card>
</template>

<script setup>
/*
 * Admin forgot-password: requests a reset email via authApi.requestPasswordReset. Always shows the
 * same "check your inbox" confirmation, even for unknown emails, so accounts can't be probed.
 */
import { ref } from 'vue'
import { authApi, getApiErrorMessage } from 'services/api'
import { useNotify } from 'composables/useNotify'

const email = ref('')
const loading = ref(false)
const done = ref(false)
const notify = useNotify()

async function onSubmit () {
  loading.value = true
  try {
    await authApi.requestPasswordReset(email.value.trim())
    done.value = true
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.fp-card {
  width: 400px;
  max-width: 92vw;
}
</style>
