<template>
  <q-page class="app-page">
    <AppListHeader
      title="Credentials"
      subtitle="API keys and integration credentials."
      :show-add="canWrite"
      add-label="New credential"
      @add="notify.info('Credential creation UI to follow.')"
    />
    <AppDataTable
      page-key="credentials"
      :rows="rows"
      :columns="columns"
      :loading="false"
      no-data-label="No credentials yet."
    />
  </q-page>
</template>

<script setup>
import { ref, computed } from 'vue'
import { usePermissions, Permissions } from 'composables/usePermissions'
import { useNotify } from 'composables/useNotify'

const { has } = usePermissions()
const notify = useNotify()
const canWrite = computed(() => has(Permissions.CredentialsWrite))

const columns = [
  { name: 'name', label: 'Name', field: 'name', align: 'left', sortable: true },
  { name: 'type', label: 'Type', field: 'type', align: 'left' },
  { name: 'createdAt', label: 'Created', field: 'createdAt', align: 'left' }
]
const rows = ref([])
</script>
