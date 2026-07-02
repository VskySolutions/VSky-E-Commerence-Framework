<template>
  <q-page class="app-page">
    <AppListHeader
      title="Users"
      subtitle="Admin user accounts and their assigned role."
      :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Users' }]"
      show-add
      add-label="New user"
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
      page-key="users"
      row-key="id"
      :rows="rows"
      :columns="columns"
      :loading="loading"
      :pagination="pagination"
      show-actions
      @request="onRequest"
    >
      <template #body-cell-roles="cell">
        <q-td :props="cell">
          <template v-if="cell.row.roles && cell.row.roles.length">
            <q-badge
              v-for="r in cell.row.roles"
              :key="r.id"
              color="primary"
              class="q-mr-xs"
              :label="r.name"
            />
          </template>
          <span v-else class="text-grey-6">—</span>
        </q-td>
      </template>

      <template #body-cell-isActive="cell">
        <q-td :props="cell">
          <q-badge
            :color="cell.row.isActive ? 'positive' : 'grey'"
            :label="cell.row.isActive ? 'Active' : 'Inactive'"
          />
        </q-td>
      </template>

      <template #actions="{ row }">
        <q-btn flat round dense icon="o_edit" @click="onEdit(row)">
          <q-tooltip>Edit</q-tooltip>
        </q-btn>
        <q-btn flat round dense icon="o_delete" color="negative" @click="onDelete(row)">
          <q-tooltip>Delete</q-tooltip>
        </q-btn>
      </template>
    </AppDataTable>

    <UserFormDrawer
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
 * Users list page (WO-62 / REQ-ADM-004): AppListHeader + AppDataTable (server
 * pagination) + a UserFormDrawer, following the widget template.
 *
 * Create is a single POST. Edit is two calls — update() for the profile/status,
 * then assignRoles() for the (single) role — because UpdateUserCommand does not
 * touch role assignments.
 */
import { ref, onMounted } from 'vue'
import { userApi } from 'modules/users/api'
import { getApiErrorMessage } from 'services/api'
import { useNotify } from 'composables/useNotify'
import { deleteConfirmation } from 'dialogs/delete_confirmation'
import UserFormDrawer from 'modules/users/components/UserFormDrawer.vue'

const notify = useNotify()

const columns = [
  { name: 'email', label: 'Email', field: 'email', align: 'left' },
  { name: 'fullName', label: 'Name', field: 'fullName', align: 'left' },
  { name: 'roles', label: 'Role', field: 'roles', align: 'left' },
  { name: 'isActive', label: 'Status', field: 'isActive', align: 'center' }
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
    const result = await userApi.list({
      page: p.page,
      pageSize: p.rowsPerPage,
      search: search.value || undefined
    })
    const items = Array.isArray(result) ? result : result?.items || result?.data || []
    const total = Array.isArray(result)
      ? result.length
      : result?.totalCount ?? result?.total ?? items.length
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
    const roleIds = payload.roleId ? [payload.roleId] : []
    if (editing.value && editing.value.id) {
      const id = editing.value.id
      await userApi.update(id, {
        firstName: payload.firstName,
        lastName: payload.lastName,
        isActive: payload.isActive
      })
      await userApi.assignRoles(id, roleIds)
      notify.success('User updated')
    } else {
      await userApi.create({
        email: payload.email,
        password: payload.password,
        firstName: payload.firstName,
        lastName: payload.lastName,
        roleIds
      })
      notify.success('User created')
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
  if (!(await deleteConfirmation(`the user "${row.email}"`))) return
  try {
    await userApi.remove(row.id)
    notify.success('User deleted')
    reload()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  }
}

onMounted(() => fetch())
</script>
