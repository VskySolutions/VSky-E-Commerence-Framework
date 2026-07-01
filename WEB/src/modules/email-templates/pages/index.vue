<template>
  <q-page class="app-page">
    <AppListHeader
      title="Email Templates"
      subtitle="Transactional email templates."
      :show-add="canWrite"
      add-label="New template"
      @add="notify.info('Template editor UI to follow.')"
    />
    <AppDataTable
      page-key="email-templates"
      :rows="rows"
      :columns="columns"
      :loading="false"
      no-data-label="No templates yet."
    />
  </q-page>
</template>

<script setup>
import { ref, computed } from 'vue'
import { usePermissions, Permissions } from 'composables/usePermissions'
import { useNotify } from 'composables/useNotify'

const { has } = usePermissions()
const notify = useNotify()
const canWrite = computed(() => has(Permissions.EmailTemplatesWrite))

const columns = [
  { name: 'name', label: 'Name', field: 'name', align: 'left', sortable: true },
  { name: 'subject', label: 'Subject', field: 'subject', align: 'left' },
  { name: 'updatedAt', label: 'Updated', field: 'updatedAt', align: 'left' }
]
const rows = ref([])
</script>
