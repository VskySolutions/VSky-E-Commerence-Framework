<template>
  <q-page class="app-page">
    <AppListHeader title="Customers" :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Customers' }]" :show-add="false">
      <template #actions>
        <q-input v-model="search" dense outlined debounce="400" placeholder="Search name or email" style="min-width: 240px" @update:model-value="reload">
          <template #prepend><q-icon name="o_search" /></template>
          <template v-if="search" #append><q-icon name="o_close" class="cursor-pointer" @click="search = ''; reload()" /></template>
        </q-input>
        <q-btn outline color="primary" no-caps icon="o_tune" label="Advanced" class="q-ml-sm" @click="filtersOpen = true">
          <q-badge v-if="activeFilterCount" color="red" floating>{{ activeFilterCount }}</q-badge>
        </q-btn>
      </template>
    </AppListHeader>

    <AppFilterDrawer v-model="filtersOpen" title="Filter customers" @clear="clearFilters">
      <AppSelect v-model="verifiedFilter" label="Email verified" :options="verifiedOptions" @update:model-value="reload" />
      <AppSelect v-model="activeFilter" label="Account status" :options="activeOptions" @update:model-value="reload" />
      <AppSelect v-model="taxExemptFilter" label="Tax exempt" :options="taxExemptOptions" @update:model-value="reload" />
    </AppFilterDrawer>

    <AppDataTable page-key="admin-customers" row-key="id" title="All customers" :rows="rows" :columns="columns" :loading="loading" :pagination="pagination" show-actions @request="onRequest" @refresh="reload">
      <template #body-cell-name="cell"><q-td :props="cell"><a class="text-primary cursor-pointer text-weight-medium" @click="view(cell.row)">{{ cell.row.firstName }} {{ cell.row.lastName }}</a></q-td></template>
      <template #body-cell-emailVerified="cell"><q-td :props="cell"><q-badge :color="cell.row.emailVerified ? 'positive' : 'orange'" :label="cell.row.emailVerified ? 'Verified' : 'Unverified'" /></q-td></template>
      <template #body-cell-createdOnUtc="cell"><q-td :props="cell">{{ formatDate(cell.row.createdOnUtc) }}</q-td></template>
      <template #actions="{ row }">
        <q-btn flat round dense icon="o_visibility" @click="view(row)"><q-tooltip>View</q-tooltip></q-btn>
        <q-btn flat round dense icon="o_manage_accounts" @click="manage(row)"><q-tooltip>Quick manage</q-tooltip></q-btn>
      </template>
    </AppDataTable>

    <!-- Manage dialog: roles + tax exemption -->
    <q-dialog v-model="dialog.open">
      <q-card style="min-width: 460px; max-width: 95vw">
        <q-card-section class="text-subtitle1 text-weight-medium">{{ dialog.customer?.firstName }} {{ dialog.customer?.lastName }}</q-card-section>
        <q-separator />
        <q-card-section>
          <q-inner-loading :showing="dialog.loading" />
          <AppFieldLabel label="Customer roles (group pricing)" />
          <q-select v-model="dialog.roleIds" multiple use-chips dense outlined emit-value map-options :options="roleOptions" placeholder="Assign roles" class="q-mb-md" />

          <q-separator class="q-my-md" />
          <q-toggle v-model="dialog.exemption.isTaxExempt" label="Tax exempt" color="primary" />
          <template v-if="dialog.exemption.isTaxExempt">
            <AppTextField v-model="dialog.exemption.certificateNumber" label="Exemption certificate #" placeholder="Optional" />
            <AppTextField v-model="dialog.exemption.vatId" label="VAT ID" placeholder="Optional" />
          </template>
        </q-card-section>
        <q-separator />
        <q-card-actions align="right">
          <q-btn flat no-caps label="Cancel" v-close-popup />
          <q-btn color="primary" unelevated no-caps label="Save" :loading="dialog.saving" @click="save" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup>
/* Admin customer management (WO-117): list + role assignment + tax exemption. */
import { ref, reactive, computed, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { getApiErrorMessage } from 'services/api'
import { useNotify } from 'composables/useNotify'
import { customerAdminApi, customerRoleApi } from 'modules/customers/api'
import { formatDateTime as formatDate } from 'src/utils/datetime'
import AppListHeader from 'components/common/AppListHeader.vue'
import AppDataTable from 'components/common/AppDataTable.vue'
import AppFieldLabel from 'components/common/AppFieldLabel.vue'
import AppTextField from 'components/common/AppTextField.vue'

const notify = useNotify()
const router = useRouter()

function view (row) { router.push({ name: 'admin-customer-detail', params: { id: row.id } }) }

const columns = [
  { name: 'name', label: 'Name', field: 'firstName', align: 'left' },
  { name: 'email', label: 'Email', field: 'email', align: 'left' },
  { name: 'phoneNumber', label: 'Phone', field: (r) => r.phoneNumber || '—', align: 'left' },
  { name: 'createdOnUtc', label: 'Joined', field: 'createdOnUtc', align: 'left' },
  { name: 'emailVerified', label: 'Email', field: 'emailVerified', align: 'center' }
]

const verifiedOptions = [
  { label: 'All', value: null },
  { label: 'Verified', value: true },
  { label: 'Unverified', value: false }
]
const activeOptions = [
  { label: 'All', value: null },
  { label: 'Active', value: true },
  { label: 'Inactive', value: false }
]
const taxExemptOptions = [
  { label: 'All', value: null },
  { label: 'Tax exempt', value: true },
  { label: 'Not exempt', value: false }
]

const rows = ref([])
const loading = ref(false)
const search = ref('')
const verifiedFilter = ref(null)
const activeFilter = ref(null)
const taxExemptFilter = ref(null)
const filtersOpen = ref(false)
const pagination = ref({ page: 1, rowsPerPage: 20, rowsNumber: 0 })
const roleOptions = ref([])

const activeFilterCount = computed(() =>
  (verifiedFilter.value !== null ? 1 : 0) + (activeFilter.value !== null ? 1 : 0) + (taxExemptFilter.value !== null ? 1 : 0)
)

function clearFilters () {
  verifiedFilter.value = null
  activeFilter.value = null
  taxExemptFilter.value = null
  reload()
}

const dialog = reactive({ open: false, loading: false, saving: false, customer: null, roleIds: [], exemption: { isTaxExempt: false, certificateNumber: '', vatId: '' } })


async function fetch (props) {
  const p = props?.pagination || pagination.value
  loading.value = true
  try {
    const r = await customerAdminApi.list({
      page: p.page,
      pageSize: p.rowsPerPage,
      search: search.value || undefined,
      emailVerified: verifiedFilter.value === null ? undefined : verifiedFilter.value,
      isActive: activeFilter.value === null ? undefined : activeFilter.value,
      isTaxExempt: taxExemptFilter.value === null ? undefined : taxExemptFilter.value
    })
    rows.value = Array.isArray(r?.items) ? r.items : []
    pagination.value = { ...p, rowsNumber: r?.totalCount ?? rows.value.length }
  } catch (e) { rows.value = []; notify.error(getApiErrorMessage(e)) } finally { loading.value = false }
}
function onRequest (props) { fetch(props) }
function reload () { fetch({ pagination: { ...pagination.value, page: 1 } }) }

async function loadRoleOptions () {
  try {
    const r = await customerRoleApi.list({ page: 1, pageSize: 200 })
    const items = Array.isArray(r) ? r : r?.items || []
    roleOptions.value = items.map((x) => ({ label: x.name, value: x.id }))
  } catch (e) { roleOptions.value = [] }
}

async function manage (row) {
  dialog.customer = row
  dialog.open = true
  dialog.loading = true
  try {
    const [roles, exemption] = await Promise.all([
      customerAdminApi.getRoles(row.id).catch(() => []),
      customerAdminApi.getTaxExemption(row.id).catch(() => null)
    ])
    dialog.roleIds = (Array.isArray(roles) ? roles : []).map((r) => r.id)
    dialog.exemption = {
      isTaxExempt: exemption?.isTaxExempt || false,
      certificateNumber: exemption?.certificateNumber || '',
      vatId: exemption?.vatId || ''
    }
  } catch (e) { notify.error(getApiErrorMessage(e)) } finally { dialog.loading = false }
}

async function save () {
  dialog.saving = true
  try {
    await customerAdminApi.setRoles(dialog.customer.id, dialog.roleIds)
    await customerAdminApi.setTaxExemption(dialog.customer.id, {
      isTaxExempt: dialog.exemption.isTaxExempt,
      certificateNumber: dialog.exemption.certificateNumber || null,
      vatId: dialog.exemption.vatId || null
    })
    notify.success('Customer updated')
    dialog.open = false
  } catch (e) { notify.error(getApiErrorMessage(e)) } finally { dialog.saving = false }
}

onMounted(() => { fetch(); loadRoleOptions() })
</script>
