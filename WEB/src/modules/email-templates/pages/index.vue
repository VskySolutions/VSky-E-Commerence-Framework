<template>
  <q-page class="app-page">
    <AppListHeader
      title="Email Templates"
      subtitle="Transactional and marketing notification templates."
      :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Email Templates' }]"
    >
      <template #actions>
        <div class="row items-center q-gutter-sm">
          <AppSelect
            v-model="category"
            :options="categoryOptions"
            style="min-width: 160px"
            @update:model-value="reload"
          />
          <AppSelect
            v-model="enabled"
            :options="statusOptions"
            style="min-width: 150px"
            @update:model-value="reload"
          />
          <q-input
            v-model="search"
            dense
            outlined
            debounce="400"
            placeholder="Search name or key"
            style="min-width: 200px"
            @update:model-value="reload"
          >
            <template #prepend><q-icon name="o_search" /></template>
          </q-input>
          <q-btn flat round dense icon="o_refresh" :loading="loading" @click="reload">
            <q-tooltip>Reload</q-tooltip>
          </q-btn>
        </div>
      </template>
    </AppListHeader>

    <AppDataTable
      page-key="email-templates"
      row-key="templateKey"
      :rows="rows"
      :columns="columns"
      :loading="loading"
      :pagination="tablePagination"
      no-data-label="No templates match your filters."
      show-actions
      @row-click="onRowClick"
    >
      <template #body-cell-name="cell">
        <q-td :props="cell">
          <div class="row items-center no-wrap q-gutter-xs">
            <span class="text-weight-medium">{{ cell.row.name }}</span>
            <q-icon v-if="cell.row.isCritical" name="o_lock" size="16px" color="orange-8">
              <q-tooltip>Critical template — disabling requires confirmation.</q-tooltip>
            </q-icon>
          </div>
          <div class="text-caption text-grey-6">{{ cell.row.templateKey }}</div>
        </q-td>
      </template>

      <template #body-cell-category="cell">
        <q-td :props="cell">
          <q-badge
            :color="cell.row.category === 'Marketing' ? 'purple' : 'primary'"
            :label="cell.row.category"
            outline
          />
        </q-td>
      </template>

      <template #body-cell-enabled="cell">
        <q-td :props="cell">
          <q-badge
            :color="cell.row.enabled ? 'positive' : 'grey'"
            :label="cell.row.enabled ? 'Enabled' : 'Disabled'"
          />
        </q-td>
      </template>

      <template #body-cell-smtp="cell">
        <q-td :props="cell">
          <div v-if="cell.row.hasSmtpConfigured" class="text-body2 ellipsis">
            {{ cell.row.assignedSmtpAccountName || '—' }}
          </div>
          <q-chip
            v-else
            dense
            square
            color="orange-1"
            text-color="orange-9"
            icon="o_warning"
            class="q-ma-none"
          >
            No SMTP account
            <q-tooltip>
              No enabled SMTP account is assigned to the {{ cell.row.category }} category.
              Test-sends and live delivery for this template will fail until one is configured.
            </q-tooltip>
          </q-chip>
        </q-td>
      </template>

      <template #actions="{ row }">
        <q-btn flat round dense icon="o_edit" @click.stop="openEditor(row)">
          <q-tooltip>Open editor</q-tooltip>
        </q-btn>
      </template>
    </AppDataTable>
  </q-page>
</template>

<script setup>
/*
 * Email template library (WO-80, REQ-ENT-002): a filterable list of the seeded
 * notification templates. Filters (category, enabled state, name/key search) are
 * applied server-side; rows are paginated client-side. Each row opens the editor.
 */
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { emailTemplatesApi } from 'modules/email-templates/api'
import { getApiErrorMessage } from 'services/api'
import { useNotify } from 'composables/useNotify'

const router = useRouter()
const notify = useNotify()

const columns = [
  { name: 'name', label: 'Template', field: 'name', align: 'left', sortable: true },
  { name: 'category', label: 'Category', field: 'category', align: 'left', sortable: true },
  { name: 'enabled', label: 'Status', field: 'enabled', align: 'left', sortable: true },
  { name: 'smtp', label: 'SMTP account', field: 'assignedSmtpAccountName', align: 'left' }
]

const categoryOptions = [
  { label: 'All categories', value: null },
  { label: 'Transactional', value: 'Transactional' },
  { label: 'Marketing', value: 'Marketing' }
]
const statusOptions = [
  { label: 'All statuses', value: null },
  { label: 'Enabled', value: true },
  { label: 'Disabled', value: false }
]

// Stable descriptor -> client-side pagination (no rowsNumber = client mode).
const tablePagination = { sortBy: 'name', rowsPerPage: 25 }

const rows = ref([])
const loading = ref(false)
const category = ref(null)
const enabled = ref(null)
const search = ref('')

async function load () {
  loading.value = true
  try {
    const result = await emailTemplatesApi.list({
      category: category.value ?? undefined,
      enabled: enabled.value ?? undefined,
      search: search.value || undefined
    })
    rows.value = Array.isArray(result) ? result : result?.items || []
  } catch (err) {
    rows.value = []
    notify.error(getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}

function reload () {
  load()
}

function openEditor (row) {
  router.push({ name: 'email-template-editor', query: { key: row.templateKey } })
}

function onRowClick (evt, row) {
  openEditor(row)
}

onMounted(load)
</script>
