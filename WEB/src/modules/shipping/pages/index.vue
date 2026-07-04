<template>
  <q-page class="app-page">
    <AppListHeader title="Shipping" :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Shipping' }]" :show-add="false" />
    <q-tabs v-model="tab" dense no-caps align="left" active-color="primary" indicator-color="primary" class="text-grey-7 q-mb-md" @update:model-value="onTab">
      <q-tab name="methods" label="Methods" icon="o_local_shipping" />
      <q-tab name="zones" label="Zones" icon="o_map" />
    </q-tabs>
    <q-tab-panels v-model="tab" animated class="bg-transparent">
      <q-tab-panel name="methods" class="q-pa-none"><ShippingMethodsPanel /></q-tab-panel>
      <q-tab-panel name="zones" class="q-pa-none"><ShippingZonesPanel /></q-tab-panel>
    </q-tab-panels>
  </q-page>
</template>

<script setup>
/* Shipping admin (WO-116): custom methods + zones sub-tabs. */
import { ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import AppListHeader from 'components/common/AppListHeader.vue'
import ShippingMethodsPanel from 'modules/shipping/components/ShippingMethodsPanel.vue'
import ShippingZonesPanel from 'modules/shipping/components/ShippingZonesPanel.vue'

const route = useRoute()
const router = useRouter()
const tab = ref(route.query.tab === 'zones' ? 'zones' : 'methods')
function onTab (value) { router.replace({ query: { ...route.query, tab: value } }) }
</script>
