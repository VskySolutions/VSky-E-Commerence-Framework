<template>
  <q-card flat bordered class="login-card q-pa-sm">
    <q-card-section class="text-center q-pb-none">
      <q-icon name="o_storefront" size="42px" color="primary" />
      <div class="text-h5 q-mt-sm">{{ tenant.brandName }}</div>
      <div class="text-subtitle2 text-grey-7">Sign in to continue</div>
    </q-card-section>

    <q-form @submit.prevent="onSubmit">
      <q-card-section class="q-gutter-md">
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

        <q-input
          v-model="password"
          :type="showPassword ? 'text' : 'password'"
          label="Password"
          outlined
          autocomplete="current-password"
          :disable="loading"
          :rules="[(v) => !!v || 'Password is required']"
        >
          <template #prepend><q-icon name="o_lock" /></template>
          <template #append>
            <q-icon
              :name="showPassword ? 'o_visibility_off' : 'o_visibility'"
              class="cursor-pointer"
              @click="showPassword = !showPassword"
            />
          </template>
        </q-input>

        <div class="row justify-end">
          <router-link class="text-primary text-caption" :to="{ name: 'forgot-password' }">
            Forgot password?
          </router-link>
        </div>
      </q-card-section>

      <q-card-actions class="q-px-md q-pb-md column q-gutter-sm">
        <q-btn
          type="submit"
          color="primary"
          class="full-width"
          label="Login"
          unelevated
          size="md"
          :loading="loading"
        />
      </q-card-actions>
    </q-form>
  </q-card>
</template>

<script setup>
/*
 * Login page (WO-94 Step 12): actually calls authStore.login and routes to
 * /dashboard (or the ?redirect target) on success.
 */
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from 'stores/auth'
import { useTenantStore } from 'stores/tenant'
import { useNotify } from 'composables/useNotify'
import { getApiErrorMessage } from 'services/api'

const email = ref('')
const password = ref('')
const showPassword = ref(false)
const loading = ref(false)

const auth = useAuthStore()
const tenant = useTenantStore()
const router = useRouter()
const route = useRoute()
const notify = useNotify()

async function onSubmit () {
  loading.value = true
  try {
    await auth.login({ email: email.value, password: password.value })
    notify.success('Signed in successfully')
    // Unified login (WO-112): honour an explicit ?redirect, else send each role to its
    // home — staff (any role) to the admin dashboard, customers (no role) to the storefront.
    if (typeof route.query.redirect === 'string' && route.query.redirect) {
      router.push(route.query.redirect)
    } else {
      router.push(auth.roles.length ? '/dashboard' : '/shop')
    }
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-card {
  width: 380px;
  max-width: 92vw;
}
</style>
