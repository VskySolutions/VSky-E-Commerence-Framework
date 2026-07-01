<template>
  <q-page class="app-page">
    <AppListHeader
      title="Stores"
      subtitle="Manage the storefronts for this tenant."
      :show-add="canWrite"
      add-label="New store"
      @add="notify.info('Store creation UI to follow.')"
    />
    <AppDataTable
      page-key="stores"
      :rows="rows"
      :columns="columns"
      :loading="false"
      no-data-label="No stores yet."
    />
  </q-page>
</template>

<script setup>
import { ref, computed } from 'vue'
import { usePermissions, Permissions } from 'composables/usePermissions'
import { useNotify } from 'composables/useNotify'

const { has } = usePermissions()
const notify = useNotify()
const canWrite = computed(() => has(Permissions.StoresWrite))

const columns = [
  { name: 'name', label: 'Name', field: 'name', align: 'left', sortable: true },
  { name: 'domain', label: 'Domain', field: 'domain', align: 'left' },
  { name: 'currency', label: 'Currency', field: 'currency', align: 'left' }
]
const rows = ref([])
</script>
