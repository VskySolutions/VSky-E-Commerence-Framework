<template>
  <q-page class="app-page">
    <AppListHeader
      title="Credentials"
      subtitle="Encrypted API keys and integration credentials."
      :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Credentials' }]"
      :show-add="canWrite"
      add-label="New credential"
      @add="onAdd"
    >
      <template #actions>
        <q-btn flat color="primary" icon="o_refresh" label="Reload" no-caps :loading="loading" @click="load" />
      </template>
    </AppListHeader>

    <AppDataTable
      page-key="credentials"
      row-key="serviceType"
      :rows="rows"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      show-actions
      no-data-label="No credentials configured yet."
      @request="onRequest"
    >
      <template #body-cell-serviceType="cell">
        <q-td :props="cell"><a class="text-primary cursor-pointer text-weight-medium" @click="onManage(cell.row)">{{ cell.row.serviceType }}</a></q-td>
      </template>

      <template #body-cell-maskedValue="cell">
        <q-td :props="cell">
          <span v-if="cell.row.maskedValue" class="cred-value">{{ cell.row.maskedValue }}</span>
          <span v-else class="text-grey-6">—</span>
        </q-td>
      </template>

      <template #body-cell-description="cell">
        <q-td :props="cell">{{ cell.row.description || '—' }}</q-td>
      </template>

      <template #body-cell-isConfigured="cell">
        <q-td :props="cell">
          <q-badge
            :color="cell.row.isConfigured ? 'positive' : 'grey'"
            :label="cell.row.isConfigured ? 'Configured' : 'Empty'"
          />
        </q-td>
      </template>

      <template #body-cell-updatedAtUtc="cell">
        <q-td :props="cell">{{ fmtDate(cell.row.updatedAtUtc) }}</q-td>
      </template>

      <template #actions="{ row }">
        <q-btn
          flat
          round
          dense
          icon="o_wifi_tethering"
          :loading="testing === row.serviceType"
          @click="onTest(row)"
        >
          <q-tooltip>Test connection</q-tooltip>
        </q-btn>
        <q-btn v-if="canWrite" flat round dense icon="o_edit" @click="onManage(row)">
          <q-tooltip>Edit</q-tooltip>
        </q-btn>
        <q-btn v-if="canWrite" flat round dense icon="o_delete" color="negative" @click="onDelete(row)">
          <q-tooltip>Delete</q-tooltip>
        </q-btn>
      </template>
    </AppDataTable>
  </q-page>
</template>

<script setup>
/*
 * Credentials page (WO-9 REQ-TEN-002): lists masked credentials, creates/edits
 * them via a form drawer (PUT upsert keyed by serviceType), runs per-row
 * connectivity tests, and deletes with confirmation. Secrets are never returned
 * by the API — the table shows only the masked (last-four) value.
 */
import { ref, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { format, parseISO, isValid } from 'date-fns'
import { credentialApi } from 'modules/credentials/api'
import { getApiErrorMessage } from 'services/api'
import { useNotify } from 'composables/useNotify'
import { usePermissions, Permissions } from 'composables/usePermissions'
import { deleteConfirmation } from 'dialogs/delete_confirmation'

const router = useRouter()
const notify = useNotify()
const { has } = usePermissions()

const canWrite = computed(() => has(Permissions.CredentialsWrite))

const columns = [
  { name: 'serviceType', label: 'Service', field: 'serviceType', align: 'left', sortable: true },
  { name: 'maskedValue', label: 'Value', field: 'maskedValue', align: 'left' },
  { name: 'description', label: 'Description', field: 'description', align: 'left' },
  { name: 'isConfigured', label: 'Status', field: 'isConfigured', align: 'center' },
  { name: 'updatedAtUtc', label: 'Updated', field: 'updatedAtUtc', align: 'left', sortable: true }
]

const rows = ref([])
const loading = ref(false)
const testing = ref(null)
// No rowsNumber -> AppDataTable/q-table paginate + sort the full list client-side.
const pagination = ref({ page: 1, rowsPerPage: 10, sortBy: 'serviceType', descending: false })

function fmtDate (value) {
  if (!value) return '—'
  const d = typeof value === 'string' ? parseISO(value) : new Date(value)
  return isValid(d) ? format(d, 'dd MMM yyyy, HH:mm') : '—'
}

function onRequest (props) {
  if (props && props.pagination) pagination.value = props.pagination
}

async function load () {
  loading.value = true
  try {
    const result = await credentialApi.list()
    rows.value = Array.isArray(result) ? result : result?.items || result?.data || []
  } catch (err) {
    rows.value = []
    notify.error(getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}

function onAdd () { router.push({ name: 'credential-new' }) }
function onManage (row) { router.push({ name: 'credential-detail', params: { id: row.serviceType } }) }

async function onTest (row) {
  testing.value = row.serviceType
  try {
    const result = await credentialApi.test(row.serviceType)
    if (result && result.success) {
      notify.success(result.message || 'Connection succeeded')
    } else {
      notify.error((result && result.message) || 'Connection failed')
    }
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    testing.value = null
  }
}

async function onDelete (row) {
  if (!(await deleteConfirmation(`the "${row.serviceType}" credential`))) return
  try {
    await credentialApi.remove(row.serviceType)
    notify.success('Credential deleted')
    load()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  }
}

onMounted(load)
</script>

<style scoped>
.cred-value {
  font-family: 'Roboto Mono', monospace;
  letter-spacing: 1px;
}
</style>
