<template>
  <q-page class="app-page">
    <AppListHeader
      title="Roles"
      subtitle="Custom roles and the admin modules they grant."
      :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Roles' }]"
      show-add
      add-label="New role"
      @add="onAdd"
    />

    <AppDataTable
      page-key="roles"
      row-key="id"
      :rows="rows"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      show-actions
      @request="onRequest"
    >
      <template #body-cell-isSystemRole="cell">
        <q-td :props="cell">
          <q-badge
            :color="cell.row.isSystemRole ? 'primary' : 'grey'"
            :label="cell.row.isSystemRole ? 'System' : 'Custom'"
          />
        </q-td>
      </template>

      <template #body-cell-modules="cell">
        <q-td :props="cell">
          {{ (cell.row.accessibleModules || []).length }}
        </q-td>
      </template>

      <template #actions="{ row }">
        <q-btn flat round dense icon="o_edit" :disable="row.isSystemRole" @click="onEdit(row)">
          <q-tooltip>{{ row.isSystemRole ? 'System roles cannot be edited' : 'Edit' }}</q-tooltip>
        </q-btn>
        <q-btn
          flat
          round
          dense
          icon="o_delete"
          color="negative"
          :disable="row.isSystemRole"
          @click="onDelete(row)"
        >
          <q-tooltip>{{ row.isSystemRole ? 'System roles cannot be deleted' : 'Delete' }}</q-tooltip>
        </q-btn>
      </template>
    </AppDataTable>

    <RoleFormDrawer
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
 * Roles list page (WO-62 / REQ-ADM-004): AppListHeader + AppDataTable + a
 * RoleFormDrawer, following the widget template. System roles (isSystemRole)
 * are read-only, so their Edit/Delete actions are disabled (AC-ADM-004.5).
 *
 * The list endpoint returns a plain (unpaged) array; rowsPerPage is 0 ("All")
 * so the table shows every role in one page.
 */
import { ref, onMounted } from 'vue'
import { roleApi } from 'modules/roles/api'
import { getApiErrorMessage } from 'services/api'
import { useNotify } from 'composables/useNotify'
import { deleteConfirmation } from 'dialogs/delete_confirmation'
import RoleFormDrawer from 'modules/roles/components/RoleFormDrawer.vue'

const notify = useNotify()

const columns = [
  { name: 'name', label: 'Name', field: 'name', align: 'left' },
  { name: 'isSystemRole', label: 'Type', field: 'isSystemRole', align: 'left' },
  { name: 'modules', label: 'Modules', field: 'accessibleModules', align: 'left' }
]

const rows = ref([])
const loading = ref(false)
const pagination = ref({ page: 1, rowsPerPage: 0, rowsNumber: 0 })

const drawerOpen = ref(false)
const editing = ref(null)
const saving = ref(false)

async function fetch (props) {
  const p = props?.pagination || pagination.value
  loading.value = true
  try {
    const result = await roleApi.list()
    const items = Array.isArray(result) ? result : result?.items || result?.data || []
    rows.value = items
    pagination.value = { ...p, rowsNumber: items.length }
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
  if (row.isSystemRole) return
  editing.value = { ...row }
  drawerOpen.value = true
}

async function onSubmit (payload) {
  saving.value = true
  try {
    if (editing.value && editing.value.id) {
      await roleApi.update(editing.value.id, payload)
      notify.success('Role updated')
    } else {
      await roleApi.create(payload)
      notify.success('Role created')
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
  if (row.isSystemRole) return
  if (!(await deleteConfirmation(`the role "${row.name}"`))) return
  try {
    await roleApi.remove(row.id)
    notify.success('Role deleted')
    reload()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  }
}

onMounted(() => fetch())
</script>
