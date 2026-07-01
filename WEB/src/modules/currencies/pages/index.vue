<template>
  <q-page class="app-page">
    <AppListHeader
      title="Currencies"
      subtitle="Currencies available across stores."
      :show-add="canWrite"
      add-label="New currency"
      @add="notify.info('Currency creation UI to follow.')"
    />
    <AppDataTable
      page-key="currencies"
      :rows="rows"
      :columns="columns"
      :loading="false"
      no-data-label="No currencies yet."
    />
  </q-page>
</template>

<script setup>
import { ref, computed } from 'vue'
import { usePermissions, Permissions } from 'composables/usePermissions'
import { useNotify } from 'composables/useNotify'

const { has } = usePermissions()
const notify = useNotify()
const canWrite = computed(() => has(Permissions.CurrenciesWrite))

const columns = [
  { name: 'code', label: 'Code', field: 'code', align: 'left', sortable: true },
  { name: 'name', label: 'Name', field: 'name', align: 'left' },
  { name: 'symbol', label: 'Symbol', field: 'symbol', align: 'center' }
]
const rows = ref([])
</script>
