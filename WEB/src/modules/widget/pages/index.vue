<template>
  <q-page class="app-page">
    <AppListHeader
      title="Widgets"
      subtitle="The feature-module template."
      :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Widgets' }]"
      show-add
      add-label="New widget"
      @add="onAdd"
    >
      <template #actions>
        <q-input
          v-model="search"
          dense
          outlined
          debounce="400"
          placeholder="Search"
          class="q-mr-sm"
          @update:model-value="reload"
        >
          <template #prepend><q-icon name="o_search" /></template>
        </q-input>
      </template>
    </AppListHeader>

    <AppDataTable
      page-key="widgets"
      row-key="id"
      :rows="rows"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      show-actions
      @request="onRequest"
    >
      <template #body-cell-isActive="cell">
        <q-td :props="cell">
          <q-badge :color="cell.row.isActive ? 'positive' : 'grey'" :label="cell.row.isActive ? 'Active' : 'Inactive'" />
        </q-td>
      </template>

      <template #actions="{ row }">
        <q-btn flat round dense icon="o_visibility" @click="onView(row)">
          <q-tooltip>View</q-tooltip>
        </q-btn>
        <q-btn flat round dense icon="o_edit" @click="onEdit(row)">
          <q-tooltip>Edit</q-tooltip>
        </q-btn>
        <q-btn flat round dense icon="o_delete" color="negative" @click="onDelete(row)">
          <q-tooltip>Delete</q-tooltip>
        </q-btn>
      </template>
    </AppDataTable>

    <WidgetFormDrawer
      v-model="drawerOpen"
      :item="editing"
      :saving="saving"
      @submit="onSubmit"
      @cancel="drawerOpen = false"
    />
  </q-page>
</template>

<script setup>
/*
 * Widget list page (WO-94 Step 12): AppListHeader + AppDataTable (server
 * pagination) + WidgetFormDrawer. Degrades gracefully when the /api/widgets
 * endpoint is not yet implemented.
 */
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { widgetApi, getApiErrorMessage } from 'services/api'
import { useNotify } from 'composables/useNotify'
import { deleteConfirmation } from 'dialogs/delete_confirmation'
import WidgetFormDrawer from 'modules/widget/components/WidgetFormDrawer.vue'

const router = useRouter()
const notify = useNotify()

const columns = [
  { name: 'name', label: 'Name', field: 'name', align: 'left', sortable: true },
  { name: 'slug', label: 'Slug', field: 'slug', align: 'left' },
  { name: 'status', label: 'Status', field: 'status', align: 'left', sortable: true },
  { name: 'isActive', label: 'Active', field: 'isActive', align: 'center' }
]

const rows = ref([])
const loading = ref(false)
const search = ref('')
const pagination = ref({ page: 1, rowsPerPage: 10, rowsNumber: 0, sortBy: 'name', descending: false })

const drawerOpen = ref(false)
const editing = ref(null)
const saving = ref(false)

async function fetch (props) {
  const p = props?.pagination || pagination.value
  loading.value = true
  try {
    const result = await widgetApi.list({
      page: p.page,
      pageSize: p.rowsPerPage,
      sortBy: p.sortBy,
      descending: p.descending,
      search: search.value || undefined
    })
    // Tolerate several common list-response shapes.
    const items = Array.isArray(result) ? result : result?.items || result?.data || []
    const total = Array.isArray(result) ? result.length : result?.total ?? result?.totalCount ?? items.length
    rows.value = items
    pagination.value = { ...p, rowsNumber: total }
  } catch (err) {
    rows.value = []
    pagination.value = { ...p, rowsNumber: 0 }
    // Endpoint may not exist yet — inform but do not crash.
    notify.warning('Widgets are not available yet: ' + getApiErrorMessage(err))
  } finally {
    loading.value = false
  }
}

function onRequest (props) {
  fetch(props)
}

function reload () {
  fetch({ pagination: { ...pagination.value, page: 1 } })
}

function onAdd () {
  editing.value = null
  drawerOpen.value = true
}

function onEdit (row) {
  editing.value = { ...row }
  drawerOpen.value = true
}

function onView (row) {
  router.push({ name: 'widget-detail', params: { id: row.id } })
}

async function onSubmit (payload) {
  saving.value = true
  try {
    if (editing.value && editing.value.id) {
      await widgetApi.update(editing.value.id, payload)
      notify.success('Widget updated')
    } else {
      await widgetApi.create(payload)
      notify.success('Widget created')
    }
    drawerOpen.value = false
    reload()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    saving.value = false
  }
}

async function onDelete (row) {
  if (!(await deleteConfirmation(`the widget "${row.name}"`))) return
  try {
    await widgetApi.remove(row.id)
    notify.success('Widget deleted')
    reload()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  }
}

onMounted(() => fetch())
</script>
