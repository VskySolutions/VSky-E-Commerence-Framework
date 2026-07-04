<template>
  <q-page class="app-page">
    <AppDetailHeader
      :breadcrumbs="[
        { label: 'Home', icon: 'o_home', to: '/dashboard' },
        { label: 'Customers', to: '/customers' },
        { label: fullName || 'Customer' }
      ]"
      :status="customer ? (customer.emailVerified ? 'Verified' : 'Unverified') : ''"
      :status-color="customer && customer.emailVerified ? 'positive' : 'orange'"
      @back="$router.push('/customers')"
    />

    <q-inner-loading :showing="loading" />

    <template v-if="customer">
      <div class="row q-col-gutter-md">
        <!-- Left column: profile + roles/tax -->
        <div class="col-12 col-md-5">
          <AppSection title="Profile">
            <div class="text-h6 q-mb-xs">{{ fullName }}</div>
            <q-list dense>
              <q-item class="q-px-none"><q-item-section avatar><q-icon name="o_mail" color="grey-7" /></q-item-section><q-item-section>{{ customer.email || '—' }}</q-item-section></q-item>
              <q-item class="q-px-none"><q-item-section avatar><q-icon name="o_call" color="grey-7" /></q-item-section><q-item-section>{{ customer.phoneNumber || '—' }}</q-item-section></q-item>
              <q-item v-if="customer.whatsAppPhoneNumber" class="q-px-none"><q-item-section avatar><q-icon name="o_chat" color="green-7" /></q-item-section><q-item-section>{{ customer.whatsAppPhoneNumber }} <span class="text-caption text-grey-6">(WhatsApp{{ customer.whatsAppOptIn ? ', opted-in' : '' }})</span></q-item-section></q-item>
              <q-item class="q-px-none"><q-item-section avatar><q-icon name="o_event" color="grey-7" /></q-item-section><q-item-section>Joined {{ formatDate(customer.createdOnUtc) }}</q-item-section></q-item>
            </q-list>
          </AppSection>

          <AppSection title="Group roles &amp; tax exemption">
            <template #actions>
              <q-btn v-if="canWrite" color="primary" unelevated no-caps dense :loading="saving" label="Save" @click="save" />
            </template>
            <AppFieldLabel label="Customer roles (group pricing)" />
            <q-select
              v-model="form.roleIds"
              multiple use-chips dense outlined emit-value map-options
              :options="roleOptions"
              :disable="!canWrite"
              placeholder="Assign roles"
              hint="Roles drive group pricing and catalog visibility"
              class="q-mb-md"
            />
            <q-separator class="q-my-md" />
            <q-toggle v-model="form.isTaxExempt" :disable="!canWrite" label="Tax exempt" color="primary" />
            <template v-if="form.isTaxExempt">
              <AppTextField v-model="form.certificateNumber" label="Exemption certificate #" placeholder="e.g. TX-99213" hint="Reference for the exemption certificate on file" :disable="!canWrite" />
              <AppTextField v-model="form.vatId" label="VAT ID" placeholder="e.g. GB123456789" hint="Buyer's VAT / tax registration id" :disable="!canWrite" />
            </template>
          </AppSection>
        </div>

        <!-- Right column: stats + orders + addresses -->
        <div class="col-12 col-md-7">
          <div class="row q-col-gutter-md q-mb-md">
            <div class="col-6">
              <q-card flat bordered class="app-section text-center q-pa-md">
                <div class="text-caption text-grey-7">Orders</div>
                <div class="text-h5 text-primary">{{ customer.orderCount }}</div>
              </q-card>
            </div>
            <div class="col-6">
              <q-card flat bordered class="app-section text-center q-pa-md">
                <div class="text-caption text-grey-7">Lifetime spend</div>
                <div class="text-h5 text-primary">{{ formatMoney(customer.totalSpent) }}</div>
              </q-card>
            </div>
          </div>

          <AppSection title="Recent orders">
            <q-markup-table flat dense v-if="customer.recentOrders.length">
              <thead>
                <tr>
                  <th class="text-left">Order #</th>
                  <th class="text-left">Placed</th>
                  <th class="text-left">Status</th>
                  <th class="text-left">Payment</th>
                  <th class="text-right">Total</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="o in customer.recentOrders" :key="o.id" class="cursor-pointer" @click="openOrder(o)">
                  <td class="text-left text-primary">{{ o.orderNumber }}</td>
                  <td class="text-left">{{ formatDate(o.placedOnUtc) }}</td>
                  <td class="text-left"><q-badge :color="statusColor(o.status)" :label="o.status" /></td>
                  <td class="text-left">{{ o.paymentStatus }}</td>
                  <td class="text-right">{{ o.currencyCode }} {{ formatMoney(o.totalAmount) }}</td>
                </tr>
              </tbody>
            </q-markup-table>
            <div v-else class="text-grey-6 text-caption">No orders placed yet.</div>
          </AppSection>

          <AppSection title="Address book">
            <div v-if="!customer.addresses.length" class="text-grey-6 text-caption">No saved addresses.</div>
            <div v-else class="row q-col-gutter-sm">
              <div v-for="a in customer.addresses" :key="a.id" class="col-12 col-sm-6">
                <q-card flat bordered class="q-pa-sm full-height">
                  <div class="row items-center q-gutter-xs q-mb-xs">
                    <q-badge :color="a.addressType === 'Billing' ? 'purple' : 'teal'" :label="a.addressType" />
                    <q-badge v-if="a.isDefault" color="primary" outline label="Default" />
                  </div>
                  <div class="text-body2">{{ a.firstName }} {{ a.lastName }}</div>
                  <div v-if="a.company" class="text-caption text-grey-7">{{ a.company }}</div>
                  <div class="text-caption">{{ a.addressLine1 }}<template v-if="a.addressLine2">, {{ a.addressLine2 }}</template></div>
                  <div class="text-caption">{{ [a.city, a.stateProvince, a.postalCode].filter(Boolean).join(', ') }}</div>
                  <div class="text-caption">{{ a.countryCode }}<template v-if="a.phoneNumber"> · {{ a.phoneNumber }}</template></div>
                </q-card>
              </div>
            </div>
          </AppSection>
        </div>
      </div>
    </template>
  </q-page>
</template>

<script setup>
/*
 * Customer detail (WO-117): full admin view of a single customer — profile, editable group roles +
 * tax exemption, lifetime order stats, recent order history (links to the order detail) and the
 * saved address book. Reuses the same role/tax-exemption endpoints as the list's quick-manage flow.
 */
import { ref, reactive, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { getApiErrorMessage } from 'services/api'
import { usePermissions } from 'composables/usePermissions'
import { useNotify } from 'composables/useNotify'
import { customerAdminApi, customerRoleApi } from 'modules/customers/api'
import { formatMoney, formatDate, orderStatusColor as statusColor } from 'modules/orders/api'
import AppDetailHeader from 'components/common/AppDetailHeader.vue'
import AppSection from 'components/common/AppSection.vue'
import AppFieldLabel from 'components/common/AppFieldLabel.vue'
import AppTextField from 'components/common/AppTextField.vue'

const route = useRoute()
const router = useRouter()
const notify = useNotify()
const { has } = usePermissions()
const canWrite = computed(() => has('Users.Write'))

const customer = ref(null)
const loading = ref(false)
const saving = ref(false)
const roleOptions = ref([])
const form = reactive({ roleIds: [], isTaxExempt: false, certificateNumber: '', vatId: '' })

const fullName = computed(() => customer.value ? `${customer.value.firstName} ${customer.value.lastName}`.trim() : '')

async function load () {
  loading.value = true
  try {
    const c = await customerAdminApi.get(route.params.id)
    customer.value = c
    form.roleIds = (c.roles || []).map((r) => r.id)
    form.isTaxExempt = c.isTaxExempt || false
    form.certificateNumber = c.taxExemptionCertificate || ''
    form.vatId = c.vatId || ''
  } catch (e) { notify.error(getApiErrorMessage(e)) } finally { loading.value = false }
}

async function loadRoleOptions () {
  try {
    const r = await customerRoleApi.list({ page: 1, pageSize: 200 })
    const items = Array.isArray(r) ? r : r?.items || []
    roleOptions.value = items.map((x) => ({ label: x.name, value: x.id }))
  } catch (e) { roleOptions.value = [] }
}

async function save () {
  saving.value = true
  try {
    await customerAdminApi.setRoles(customer.value.id, form.roleIds)
    await customerAdminApi.setTaxExemption(customer.value.id, {
      isTaxExempt: form.isTaxExempt,
      certificateNumber: form.certificateNumber || null,
      vatId: form.vatId || null
    })
    notify.success('Customer updated')
    load()
  } catch (e) { notify.error(getApiErrorMessage(e)) } finally { saving.value = false }
}

function openOrder (o) { router.push({ name: 'admin-order-detail', params: { id: o.id } }) }

onMounted(() => { load(); loadRoleOptions() })
</script>
