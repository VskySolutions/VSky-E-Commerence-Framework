<template>
  <q-page class="app-page">
    <AppListHeader
      title="Promotions"
      :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Promotions' }]"
      :show-add="false"
    />
    <q-tabs v-model="tab" dense no-caps align="left" active-color="primary" indicator-color="primary" class="text-grey-7 q-mb-md" @update:model-value="onTab">
      <q-tab name="discounts" label="Discounts" icon="o_local_offer" />
      <q-tab name="coupons" label="Coupon Codes" icon="o_confirmation_number" />
    </q-tabs>
    <q-tab-panels v-model="tab" animated class="bg-transparent">
      <q-tab-panel name="discounts" class="q-pa-none"><DiscountsPanel /></q-tab-panel>
      <q-tab-panel name="coupons" class="q-pa-none"><CouponsPanel /></q-tab-panel>
    </q-tab-panels>
  </q-page>
</template>

<script setup>
/* Promotions admin (WO-115): Discounts + Coupon Codes sub-tabs. */
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppListHeader from 'components/common/AppListHeader.vue'
import DiscountsPanel from 'modules/pricing/components/DiscountsPanel.vue'
import CouponsPanel from 'modules/pricing/components/CouponsPanel.vue'

const route = useRoute()
const router = useRouter()
const tab = ref(route.query.tab === 'coupons' ? 'coupons' : 'discounts')
function onTab (value) { router.replace({ query: { ...route.query, tab: value } }) }
</script>
