<template>
  <div class="account-shell">
    <div class="row items-center justify-between q-mb-md">
      <div>
        <div class="text-h6 text-weight-bold">My account</div>
        <div class="text-grey-7">{{ greeting }}</div>
      </div>
      <q-btn flat no-caps color="grey-8" icon="o_logout" label="Sign out" @click="onLogout" />
    </div>

    <q-tabs
      v-model="tab"
      dense
      no-caps
      align="left"
      active-color="primary"
      indicator-color="primary"
      class="text-grey-7 q-mb-md account-tabs"
    >
      <q-route-tab name="profile" label="Profile" icon="o_person" :to="{ name: 'shop-account-profile' }" />
      <q-route-tab name="addresses" label="Addresses" icon="o_home_pin" :to="{ name: 'shop-account-addresses' }" />
      <q-route-tab name="orders" label="Orders" icon="o_receipt_long" :to="{ name: 'shop-account-orders' }" />
    </q-tabs>

    <router-view />
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useCustomerAuthStore } from 'stores/customerAuth'

const router = useRouter()
const auth = useCustomerAuthStore()

const tab = ref('profile')
const greeting = auth.displayName ? `Welcome back, ${auth.displayName}` : 'Manage your profile, addresses and orders.'

function onLogout () {
  auth.logout()
  router.push({ name: 'shop-home' })
}
</script>

<style scoped lang="scss">
.account-shell {
  max-width: 900px;
  margin: 0 auto;
  padding: 24px 16px 48px;
}
.account-tabs {
  border-bottom: 1px solid rgba(0, 0, 0, 0.08);
}
</style>
