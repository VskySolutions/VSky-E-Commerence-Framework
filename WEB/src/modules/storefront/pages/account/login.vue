<template>
  <div class="account-auth-page">
    <q-card flat bordered class="account-auth-card">
      <q-card-section>
        <div class="text-h6 text-weight-bold">Sign in</div>
        <div class="text-grey-7">Access your account, orders and saved addresses.</div>
      </q-card-section>

      <q-form @submit.prevent="onSubmit">
        <q-card-section class="q-gutter-md">
          <q-input
            v-model="email"
            type="email"
            label="Email"
            outlined
            dense
            :rules="[(v) => !!v || 'Email is required']"
            autocomplete="email"
          />
          <q-input
            v-model="password"
            :type="showPassword ? 'text' : 'password'"
            label="Password"
            outlined
            dense
            :rules="[(v) => !!v || 'Password is required']"
            autocomplete="current-password"
          >
            <template #append>
              <q-icon
                :name="showPassword ? 'o_visibility_off' : 'o_visibility'"
                class="cursor-pointer"
                @click="showPassword = !showPassword"
              />
            </template>
          </q-input>

          <div class="row justify-end">
            <router-link class="text-primary text-caption" :to="{ name: 'shop-forgot-password' }">
              Forgot password?
            </router-link>
          </div>
        </q-card-section>

        <q-card-actions class="q-px-md q-pb-md column q-gutter-sm">
          <q-btn type="submit" color="primary" no-caps unelevated label="Sign in" :loading="loading" class="full-width" />
          <div class="text-center text-grey-7 text-caption">
            New here?
            <router-link class="text-primary" :to="{ name: 'shop-register' }">Create an account</router-link>
          </div>
        </q-card-actions>
      </q-form>
    </q-card>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useQuasar } from 'quasar'
import { useCustomerAuthStore } from 'stores/customerAuth'
import { getApiErrorMessage } from 'services/api'

const router = useRouter()
const route = useRoute()
const $q = useQuasar()
const auth = useCustomerAuthStore()

const email = ref('')
const password = ref('')
const showPassword = ref(false)
const loading = ref(false)

async function onSubmit () {
  loading.value = true
  try {
    await auth.login({ email: email.value.trim(), password: password.value })
    const redirect = route.query.redirect
    router.push(redirect && typeof redirect === 'string' ? redirect : { name: 'shop-account-profile' })
  } catch (e) {
    $q.notify({ type: 'negative', message: getApiErrorMessage(e) })
  } finally {
    loading.value = false
  }
}
</script>

<style scoped lang="scss">
.account-auth-page {
  min-height: 70vh;
  display: flex;
  align-items: flex-start;
  justify-content: center;
  padding: 32px 16px;
}
.account-auth-card {
  width: 100%;
  max-width: 420px;
  border-radius: 12px;
}
</style>
