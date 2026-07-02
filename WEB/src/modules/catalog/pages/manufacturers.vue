<template>
  <q-page class="app-page">
    <AppListHeader
      title="Manufacturers"
      subtitle="Manage brands / manufacturers."
      :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Manufacturers' }]"
      :show-add="canWrite"
      add-label="New manufacturer"
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
      page-key="catalog-manufacturers"
      row-key="id"
      :rows="rows"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      show-actions
      @request="onRequest"
    >
      <template #body-cell-isEnabled="cell">
        <q-td :props="cell">
          <q-badge
            :color="cell.row.isEnabled ? 'positive' : 'grey'"
            :label="cell.row.isEnabled ? 'Enabled' : 'Disabled'"
          />
        </q-td>
      </template>

      <template #actions="{ row }">
        <q-btn v-if="canWrite" flat round dense icon="o_edit" @click="onEdit(row)">
          <q-tooltip>Edit</q-tooltip>
        </q-btn>
        <q-btn v-if="canWrite" flat round dense icon="o_delete" color="negative" @click="onDelete(row)">
          <q-tooltip>Delete</q-tooltip>
        </q-btn>
      </template>
    </AppDataTable>

    <ManufacturerFormDrawer
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
 * Manufacturers list page (WO-15): AppListHeader + AppDataTable (server
 * pagination) + ManufacturerFormDrawer, following the widget CRUD template.
 */
import { ref, computed, onMounted } from 'vue'
import { getApiErrorMessage } from 'services/api'
import { manufacturerApi } from 'modules/catalog/api'
import { usePermissions } from 'composables/usePermissions'
import { useNotify } from 'composables/useNotify'
import { deleteConfirmation } from 'dialogs/delete_confirmation'
import ManufacturerFormDrawer from 'modules/catalog/components/ManufacturerFormDrawer.vue'

const notify = useNotify()
const { has } = usePermissions()
const canWrite = computed(() => has('Catalog.Write'))

const columns = [
  { name: 'name', label: 'Name', field: 'name', align: 'left' },
  { name: 'slug', label: 'Slug', field: 'slug', align: 'left' },
  { name: 'displayOrder', label: 'Order', field: 'displayOrder', align: 'right' },
  { name: 'isEnabled', label: 'Status', field: 'isEnabled', align: 'center' }
]

const rows = ref([])
const loading = ref(false)
const search = ref('')
const pagination = ref({ page: 1, rowsPerPage: 10, rowsNumber: 0 })

const drawerOpen = ref(false)
const editing = ref(null)
const saving = ref(false)

async function fetch (props) {
  const p = props?.pagination || pagination.value
  loading.value = true
  try {
    const result = await manufacturerApi.list({
      page: p.page,
      pageSize: p.rowsPerPage,
      search: search.value || undefined
    })
    const items = Array.isArray(result) ? result : result?.items || []
    const total = Array.isArray(result) ? result.length : result?.totalCount ?? items.length
    rows.value = items
    pagination.value = { ...p, rowsNumber: total }
  } catch (err) {
    rows.value = []
    pagination.value = { ...p, rowsNumber: 0 }
    notify.error(getApiErrorMessage(err))
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

async function onSubmit (payload) {
  saving.value = true
  try {
    if (editing.value && editing.value.id) {
      await manufacturerApi.update(editing.value.id, payload)
      notify.success('Manufacturer updated')
    } else {
      await manufacturerApi.create(payload)
      notify.success('Manufacturer created')
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
  if (!(await deleteConfirmation(`the manufacturer "${row.name}"`))) return
  try {
    await manufacturerApi.remove(row.id)
    notify.success('Manufacturer deleted')
    reload()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  }
}

onMounted(() => fetch())
</script>
