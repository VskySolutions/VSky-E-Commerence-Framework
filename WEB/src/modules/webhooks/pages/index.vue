<template>
  <q-page class="app-page">
    <AppListHeader
      title="Webhooks"
      :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Webhooks' }]"
      :show-add="canWrite"
      add-label="New webhook"
      @add="onAdd"
    />

    <AppDataTable
      page-key="admin-webhooks"
      row-key="id"
      title="Endpoints"
      :rows="rows"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      show-actions
      @refresh="load"
    >
      <template #body-cell-url="cell">
        <q-td :props="cell"><span class="text-weight-medium">{{ cell.row.url }}</span><div v-if="cell.row.description" class="text-caption text-grey-6">{{ cell.row.description }}</div></q-td>
      </template>
      <template #body-cell-events="cell">
        <q-td :props="cell">
          <q-badge v-for="e in cell.row.eventTypes" :key="e" outline color="primary" :label="e" class="q-mr-xs q-mb-xs" />
        </q-td>
      </template>
      <template #body-cell-isActive="cell">
        <q-td :props="cell"><q-badge :color="cell.row.isActive ? 'positive' : 'grey'" :label="cell.row.isActive ? 'Active' : 'Paused'" /></q-td>
      </template>
      <template #body-cell-createdOnUtc="cell">
        <q-td :props="cell">{{ formatDate(cell.row.createdOnUtc) }}</q-td>
      </template>
      <template #actions="{ row }">
        <q-btn flat round dense icon="o_history" @click="openDeliveries(row)"><q-tooltip>Delivery history</q-tooltip></q-btn>
        <q-btn v-if="canWrite" flat round dense icon="o_edit" @click="onEdit(row)"><q-tooltip>Edit</q-tooltip></q-btn>
        <q-btn v-if="canWrite" flat round dense icon="o_delete" color="negative" @click="onDelete(row)"><q-tooltip>Delete</q-tooltip></q-btn>
      </template>
    </AppDataTable>

    <WebhookFormDrawer v-model="drawerOpen" :item="editing" :saving="saving" @submit="onSubmit" @cancel="drawerOpen = false" />

    <!-- Secret reveal: shown exactly once, right after creation -->
    <q-dialog v-model="secretDialog.open" persistent>
      <q-card style="min-width: 480px; max-width: 95vw">
        <q-card-section class="row items-center q-gutter-sm">
          <q-icon name="o_vpn_key" color="primary" size="sm" />
          <span class="text-subtitle1 text-weight-medium">Signing secret</span>
        </q-card-section>
        <q-separator />
        <q-card-section>
          <q-banner class="bg-orange-1 text-orange-9 q-mb-md" rounded dense>
            <template #avatar><q-icon name="o_warning" color="orange" /></template>
            Copy this secret now — it is shown only once and cannot be retrieved later.
          </q-banner>
          <q-input :model-value="secretDialog.secret" readonly dense outlined>
            <template #append><q-btn flat round dense icon="o_content_copy" @click="copySecret"><q-tooltip>Copy</q-tooltip></q-btn></template>
          </q-input>
        </q-card-section>
        <q-separator />
        <q-card-actions align="right">
          <q-btn color="primary" unelevated no-caps label="Done" v-close-popup />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Delivery history -->
    <q-dialog v-model="deliveries.open">
      <q-card style="min-width: 720px; max-width: 96vw">
        <q-card-section class="row items-center">
          <div class="col">
            <div class="text-subtitle1 text-weight-medium">Delivery history</div>
            <div class="text-caption text-grey-6">{{ deliveries.subscription?.url }}</div>
          </div>
          <q-btn flat round dense icon="o_refresh" @click="loadDeliveries"><q-tooltip>Refresh</q-tooltip></q-btn>
        </q-card-section>
        <q-separator />
        <q-card-section style="max-height: 60vh; overflow: auto">
          <q-inner-loading :showing="deliveries.loading" />
          <div v-if="!deliveries.rows.length && !deliveries.loading" class="text-grey-6 text-caption q-pa-md text-center">No deliveries recorded yet.</div>
          <q-markup-table v-else flat dense>
            <thead>
              <tr>
                <th class="text-left">Event</th>
                <th class="text-left">Status</th>
                <th class="text-center">Attempts</th>
                <th class="text-center">Response</th>
                <th class="text-left">Occurred</th>
                <th class="text-left">Last attempt</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="d in deliveries.rows" :key="d.id">
                <td class="text-left">{{ d.eventType }}</td>
                <td class="text-left"><q-badge :color="statusColor(d.status)" :label="d.status" /></td>
                <td class="text-center">{{ d.attemptCount }}</td>
                <td class="text-center">{{ d.lastResponseStatus ?? '—' }}</td>
                <td class="text-left">{{ formatDate(d.occurredAtUtc, true) }}</td>
                <td class="text-left">{{ d.lastAttemptOnUtc ? formatDate(d.lastAttemptOnUtc, true) : '—' }}</td>
              </tr>
            </tbody>
          </q-markup-table>
        </q-card-section>
        <q-separator />
        <q-card-actions align="right">
          <q-btn flat no-caps label="Close" v-close-popup />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup>
/*
 * Webhooks admin (WO-118): list registered endpoints, create/edit/delete subscriptions, reveal the
 * signing secret once on creation, and inspect per-endpoint delivery history. UI over the existing
 * AdminWebhooksController (subscriptions + deliveries + event-types).
 */
import { ref, reactive, computed, onMounted } from 'vue'
import { copyToClipboard } from 'quasar'
import { getApiErrorMessage } from 'services/api'
import { usePermissions } from 'composables/usePermissions'
import { useNotify } from 'composables/useNotify'
import { deleteConfirmation } from 'dialogs/delete_confirmation'
import { webhookApi, webhookDeliveryStatusColor as statusColor } from 'modules/webhooks/api'
import { formatDate } from 'modules/orders/api'
import AppListHeader from 'components/common/AppListHeader.vue'
import AppDataTable from 'components/common/AppDataTable.vue'
import WebhookFormDrawer from 'modules/webhooks/components/WebhookFormDrawer.vue'

const notify = useNotify()
const { has } = usePermissions()
const canWrite = computed(() => has('Webhooks.Write'))

const columns = [
  { name: 'url', label: 'Endpoint', field: 'url', align: 'left' },
  { name: 'events', label: 'Events', field: 'eventTypes', align: 'left' },
  { name: 'isActive', label: 'Status', field: 'isActive', align: 'center' },
  { name: 'createdOnUtc', label: 'Created', field: 'createdOnUtc', align: 'left' }
]

const rows = ref([])
const loading = ref(false)
const pagination = ref({ rowsPerPage: 0 })
const drawerOpen = ref(false)
const editing = ref(null)
const saving = ref(false)

const secretDialog = reactive({ open: false, secret: '' })
const deliveries = reactive({ open: false, loading: false, subscription: null, rows: [] })

async function load () {
  loading.value = true
  try {
    const r = await webhookApi.list()
    rows.value = Array.isArray(r) ? r : r?.items || []
  } catch (e) { rows.value = []; notify.error(getApiErrorMessage(e)) } finally { loading.value = false }
}

function onAdd () { editing.value = null; drawerOpen.value = true }
function onEdit (row) { editing.value = { ...row }; drawerOpen.value = true }

async function onSubmit (payload) {
  saving.value = true
  try {
    if (editing.value?.id) {
      await webhookApi.update(editing.value.id, payload)
      notify.success('Webhook updated')
    } else {
      const created = await webhookApi.create(payload)
      notify.success('Webhook created')
      if (created?.secret) { secretDialog.secret = created.secret; secretDialog.open = true }
    }
    drawerOpen.value = false
    load()
  } catch (e) { notify.error(getApiErrorMessage(e)) } finally { saving.value = false }
}

async function onDelete (row) {
  if (!(await deleteConfirmation(`the webhook "${row.url}"`))) return
  try { await webhookApi.remove(row.id); notify.success('Webhook deleted'); load() } catch (e) { notify.error(getApiErrorMessage(e)) }
}

async function copySecret () {
  try { await copyToClipboard(secretDialog.secret); notify.success('Secret copied') } catch (e) { /* clipboard unavailable */ }
}

function openDeliveries (row) {
  deliveries.subscription = row
  deliveries.open = true
  loadDeliveries()
}

async function loadDeliveries () {
  deliveries.loading = true
  try {
    const r = await webhookApi.deliveries({ subscriptionId: deliveries.subscription.id, page: 1, pageSize: 100 })
    deliveries.rows = Array.isArray(r?.items) ? r.items : []
  } catch (e) { deliveries.rows = []; notify.error(getApiErrorMessage(e)) } finally { deliveries.loading = false }
}

onMounted(load)
</script>
