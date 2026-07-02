<template>
  <q-page class="app-page">
    <AppListHeader
      title="Email & SMS accounts"
      subtitle="SMTP sending accounts and the Twilio SMS credential."
      :breadcrumbs="[{ label: 'Home', icon: 'o_home', to: '/dashboard' }, { label: 'Email & SMS accounts' }]"
    />

    <q-tabs
      v-model="tab"
      align="left"
      no-caps
      active-color="primary"
      indicator-color="primary"
      class="text-grey-7 q-mb-md"
    >
      <q-tab name="smtp" icon="o_mail" label="SMTP accounts" />
      <q-tab name="twilio" icon="o_sms" label="Twilio SMS" />
    </q-tabs>

    <q-tab-panels v-model="tab" animated class="bg-transparent">
      <!-- (A) SMTP accounts ------------------------------------------------- -->
      <q-tab-panel name="smtp" class="q-pa-none">
        <div class="row items-center q-mb-md">
          <div class="col text-subtitle1">SMTP accounts</div>
          <q-btn flat no-caps color="primary" icon="o_refresh" label="Reload" :loading="smtpLoading" @click="loadSmtp" />
          <q-btn
            v-if="canWrite"
            unelevated
            no-caps
            color="primary"
            icon="o_add"
            label="New account"
            class="q-ml-sm"
            @click="onAddSmtp"
          />
        </div>

        <q-banner
          v-for="cat in uncoveredCategories"
          :key="cat"
          dense
          rounded
          class="bg-orange-1 text-orange-9 q-mb-sm"
        >
          <template #avatar><q-icon name="o_warning" color="orange-9" /></template>
          No enabled SMTP account is assigned to the <strong>{{ cat }}</strong> category — those emails cannot be sent.
        </q-banner>

        <AppDataTable
          page-key="smtp-accounts"
          row-key="id"
          :rows="smtpRows"
          :columns="smtpColumns"
          :loading="smtpLoading"
          :pagination="smtpPagination"
          show-actions
          no-data-label="No SMTP accounts configured yet."
          @request="onSmtpRequest"
        >
          <template #body-cell-category="cell">
            <q-td :props="cell">
              <q-badge v-if="cell.row.category" :color="categoryColor(cell.row.category)" :label="cell.row.category" />
              <span v-else class="text-grey-6">Unassigned</span>
            </q-td>
          </template>

          <template #body-cell-enabled="cell">
            <q-td :props="cell">
              <q-toggle
                :model-value="cell.row.enabled"
                color="primary"
                :disable="!canWrite || togglingId === cell.row.id"
                @update:model-value="onToggleEnabled(cell.row, $event)"
              />
            </q-td>
          </template>

          <template #actions="{ row }">
            <q-btn flat round dense icon="o_send" @click="onTestSend(row)">
              <q-tooltip>Test send</q-tooltip>
            </q-btn>
            <q-btn v-if="canWrite" flat round dense icon="o_edit" @click="onEditSmtp(row)">
              <q-tooltip>Edit</q-tooltip>
            </q-btn>
            <q-btn v-if="canWrite" flat round dense icon="o_delete" color="negative" @click="onDeleteSmtp(row)">
              <q-tooltip>Delete</q-tooltip>
            </q-btn>
          </template>
        </AppDataTable>
      </q-tab-panel>

      <!-- (B) Twilio SMS ---------------------------------------------------- -->
      <q-tab-panel name="twilio" class="q-pa-none">
        <TwilioForm
          :config="twilioConfig"
          :loading="twilioLoading"
          :saving="twilioSaving"
          :testing="twilioTesting"
          :can-write="canWrite"
          @submit="onSubmitTwilio"
          @test="onOpenTwilioTest"
          @delete="onDeleteTwilio"
        />
      </q-tab-panel>
    </q-tab-panels>

    <!-- SMTP create / edit drawer -->
    <SmtpAccountFormDrawer
      v-model="smtpDrawerOpen"
      :item="editingSmtp"
      :saving="smtpSaving"
      @submit="onSubmitSmtp"
      @cancel="smtpDrawerOpen = false"
    />

    <!-- SMTP test-send dialog -->
    <q-dialog v-model="testDialogOpen">
      <q-card style="min-width: 380px">
        <q-card-section class="row items-center q-pb-none">
          <div class="text-h6">Send test email</div>
          <q-space />
          <q-btn v-close-popup flat round dense icon="o_close" />
        </q-card-section>
        <q-card-section>
          <div class="text-caption text-grey-7 q-mb-sm">
            Sends a test message through
            <strong>{{ testTarget && testTarget.displayName }}</strong>.
          </div>
          <AppTextField
            v-model="testForm.email"
            label="Recipient email"
            required
            :v="vTest$.email"
            type="email"
            @keyup.enter="onRunTestSend"
          />
          <q-banner
            v-if="testResult"
            dense
            rounded
            class="q-mt-md"
            :class="testResult.success ? 'bg-green-1 text-green-9' : 'bg-red-1 text-red-9'"
          >
            <template #avatar>
              <q-icon :name="testResult.success ? 'o_check_circle' : 'o_error'" />
            </template>
            {{ testResult.message }}
            <div v-if="testResult.testedAtUtc" class="text-caption">{{ fmtDate(testResult.testedAtUtc) }}</div>
          </q-banner>
        </q-card-section>
        <q-card-actions align="right" class="q-pa-md q-pt-none">
          <q-btn v-close-popup flat no-caps color="grey-8" label="Close" />
          <q-btn unelevated no-caps color="primary" icon="o_send" label="Send test" :loading="testSending" @click="onRunTestSend" />
        </q-card-actions>
      </q-card>
    </q-dialog>

    <!-- Twilio test dialog -->
    <q-dialog v-model="twilioTestDialogOpen">
      <q-card style="min-width: 380px">
        <q-card-section class="row items-center q-pb-none">
          <div class="text-h6">Test Twilio</div>
          <q-space />
          <q-btn v-close-popup flat round dense icon="o_close" />
        </q-card-section>
        <q-card-section>
          <div class="text-caption text-grey-7 q-mb-sm">
            Verifies the stored Twilio credentials. The current backend performs a connectivity check;
            the recipient below is used once live message delivery is enabled.
          </div>
          <AppTextField
            v-model="twilioTestForm.phone"
            label="Recipient phone"
            required
            :v="vTwilioTest$.phone"
            placeholder="+15005550006"
            @keyup.enter="onRunTwilioTest"
          />
          <q-banner
            v-if="twilioTestResult"
            dense
            rounded
            class="q-mt-md"
            :class="twilioTestResult.success ? 'bg-green-1 text-green-9' : 'bg-red-1 text-red-9'"
          >
            <template #avatar>
              <q-icon :name="twilioTestResult.success ? 'o_check_circle' : 'o_error'" />
            </template>
            {{ twilioTestResult.message }}
            <div v-if="twilioTestResult.testedAtUtc" class="text-caption">{{ fmtDate(twilioTestResult.testedAtUtc) }}</div>
          </q-banner>
        </q-card-section>
        <q-card-actions align="right" class="q-pa-md q-pt-none">
          <q-btn v-close-popup flat no-caps color="grey-8" label="Close" />
          <q-btn unelevated no-caps color="primary" icon="o_send" label="Run test" :loading="twilioTestSending" @click="onRunTwilioTest" />
        </q-card-actions>
      </q-card>
    </q-dialog>
  </q-page>
</template>

<script setup>
/*
 * Email & SMS accounts (WO-77). Two tabs:
 *  (A) SMTP accounts — CRUD list (TenantSmtpAccountsController) with a per-row
 *      enabled toggle, NotificationCategory badge, a test-send dialog, and a
 *      warning when a category has no enabled account.
 *  (B) Twilio SMS — a single credential stored via TenantCredentialsController
 *      under service type "twilio" (no dedicated Twilio backend). The auth token
 *      is the encrypted secret; Account SID / From number / enabled round-trip
 *      through the credential `description` as JSON.
 * The page owns all API calls + notifications; the drawer and Twilio form are
 * presentational and emit events.
 */
import { ref, reactive, computed, onMounted } from 'vue'
import { format, parseISO, isValid } from 'date-fns'
import useVuelidate from '@vuelidate/core'
import { required, email, phone } from 'validators'
import { smtpAccountApi, twilioApi } from 'modules/email-accounts/api'
import { getApiErrorMessage } from 'services/api'
import { useNotify } from 'composables/useNotify'
import { usePermissions } from 'composables/usePermissions'
import { deleteConfirmation } from 'dialogs/delete_confirmation'
import { useAuthStore } from 'stores/auth'
import SmtpAccountFormDrawer from 'modules/email-accounts/components/SmtpAccountFormDrawer.vue'
import TwilioForm from 'modules/email-accounts/components/TwilioForm.vue'

const notify = useNotify()
const auth = useAuthStore()
const { has } = usePermissions()

// No SmtpAccountsWrite constant exists in the frozen catalog yet; gate on the
// raw permission string (auth store resolves it, incl. role-based full access).
const canWrite = computed(() => has('SmtpAccounts.Write'))

const NOTIFICATION_CATEGORIES = ['Transactional', 'Marketing']

const tab = ref('smtp')

function fmtDate (value) {
  if (!value) return ''
  const d = typeof value === 'string' ? parseISO(value) : new Date(value)
  return isValid(d) ? format(d, 'dd MMM yyyy, HH:mm') : ''
}

function categoryColor (cat) {
  if (cat === 'Transactional') return 'blue-7'
  if (cat === 'Marketing') return 'purple-6'
  return 'grey'
}

// ---- SMTP accounts ----------------------------------------------------------
const smtpColumns = [
  { name: 'displayName', label: 'Name', field: 'displayName', align: 'left', sortable: true },
  { name: 'host', label: 'Host', field: 'host', align: 'left' },
  { name: 'port', label: 'Port', field: 'port', align: 'left' },
  { name: 'fromEmail', label: 'From', field: 'fromEmail', align: 'left' },
  { name: 'category', label: 'Category', field: 'category', align: 'left' },
  { name: 'enabled', label: 'Enabled', field: 'enabled', align: 'center' }
]

const smtpRows = ref([])
const smtpLoading = ref(false)
const smtpSaving = ref(false)
const togglingId = ref(null)
const smtpDrawerOpen = ref(false)
const editingSmtp = ref(null)
// No rowsNumber -> AppDataTable paginates + sorts the full list client-side.
const smtpPagination = ref({ page: 1, rowsPerPage: 10, sortBy: 'displayName', descending: false })

const uncoveredCategories = computed(() =>
  NOTIFICATION_CATEGORIES.filter(
    (cat) => !smtpRows.value.some((a) => a.enabled && a.category === cat)
  )
)

function onSmtpRequest (props) {
  if (props && props.pagination) smtpPagination.value = props.pagination
}

async function loadSmtp () {
  smtpLoading.value = true
  try {
    const result = await smtpAccountApi.list()
    smtpRows.value = Array.isArray(result) ? result : result?.items || result?.data || []
  } catch (err) {
    smtpRows.value = []
    notify.error(getApiErrorMessage(err))
  } finally {
    smtpLoading.value = false
  }
}

function onAddSmtp () {
  editingSmtp.value = null
  smtpDrawerOpen.value = true
}

function onEditSmtp (row) {
  editingSmtp.value = { ...row }
  smtpDrawerOpen.value = true
}

async function onSubmitSmtp (payload) {
  const editingId = editingSmtp.value && editingSmtp.value.id
  smtpSaving.value = true
  try {
    if (editingId) {
      await smtpAccountApi.update(editingId, payload)
      notify.success('SMTP account updated')
    } else {
      await smtpAccountApi.create(payload)
      notify.success('SMTP account created')
    }
    smtpDrawerOpen.value = false
    loadSmtp()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    smtpSaving.value = false
  }
}

async function onToggleEnabled (row, value) {
  togglingId.value = row.id
  try {
    await smtpAccountApi.update(row.id, {
      displayName: row.displayName,
      host: row.host,
      port: row.port,
      username: row.username || null,
      password: null, // omit -> API keeps the stored password
      fromName: row.fromName,
      fromEmail: row.fromEmail,
      encryptionMode: row.encryptionMode,
      authMethod: row.authMethod,
      category: row.category || null,
      enabled: value
    })
    notify.success(value ? 'Account enabled' : 'Account disabled')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    togglingId.value = null
    // Re-fetch: enabling one account disables any other in the same category.
    loadSmtp()
  }
}

async function onDeleteSmtp (row) {
  if (!(await deleteConfirmation(`the "${row.displayName}" SMTP account`))) return
  try {
    await smtpAccountApi.remove(row.id)
    notify.success('SMTP account deleted')
    loadSmtp()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  }
}

// ---- SMTP test-send dialog --------------------------------------------------
const testDialogOpen = ref(false)
const testTarget = ref(null)
const testSending = ref(false)
const testResult = ref(null)
const testForm = reactive({ email: '' })
const vTest$ = useVuelidate({ email: { required, email } }, testForm)

function onTestSend (row) {
  testTarget.value = row
  testForm.email = auth.user?.email || ''
  testResult.value = null
  vTest$.value.$reset()
  testDialogOpen.value = true
}

async function onRunTestSend () {
  const ok = await vTest$.value.$validate()
  if (!ok) return
  testSending.value = true
  testResult.value = null
  try {
    const result = await smtpAccountApi.testSend(testTarget.value.id, testForm.email.trim())
    testResult.value = result
    if (result && result.success) notify.success(result.message || 'Test email sent')
    else notify.error((result && result.message) || 'Test send failed')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    testSending.value = false
  }
}

// ---- Twilio SMS -------------------------------------------------------------
const twilioConfig = ref(emptyTwilio())
const twilioLoading = ref(false)
const twilioSaving = ref(false)
const twilioTesting = ref(false)

function emptyTwilio () {
  return { accountSid: '', fromNumber: '', enabled: false, maskedToken: '', isConfigured: false, updatedAtUtc: null }
}

function decodeTwilio (dto) {
  let meta = {}
  if (dto && dto.description) {
    try {
      meta = JSON.parse(dto.description) || {}
    } catch (e) {
      meta = {}
    }
  }
  return {
    accountSid: meta.accountSid || '',
    fromNumber: meta.fromNumber || '',
    enabled: meta.enabled ?? false,
    maskedToken: (dto && dto.maskedValue) || '',
    isConfigured: !!(dto && dto.isConfigured),
    updatedAtUtc: (dto && dto.updatedAtUtc) || null
  }
}

async function loadTwilio () {
  twilioLoading.value = true
  try {
    const dto = await twilioApi.get()
    twilioConfig.value = decodeTwilio(dto)
  } catch (err) {
    twilioConfig.value = emptyTwilio()
    // A 404 simply means Twilio has never been configured — not an error.
    if (!(err && err.response && err.response.status === 404)) {
      notify.error(getApiErrorMessage(err))
    }
  } finally {
    twilioLoading.value = false
  }
}

async function onSubmitTwilio (fields) {
  twilioSaving.value = true
  try {
    const description = JSON.stringify({
      accountSid: fields.accountSid,
      fromNumber: fields.fromNumber,
      enabled: fields.enabled
    })
    await twilioApi.upsert({ value: fields.authToken, description })
    notify.success('Twilio settings saved')
    loadTwilio()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    twilioSaving.value = false
  }
}

async function onDeleteTwilio () {
  if (!(await deleteConfirmation('the Twilio configuration'))) return
  try {
    await twilioApi.remove()
    notify.success('Twilio configuration deleted')
    loadTwilio()
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  }
}

// ---- Twilio test dialog -----------------------------------------------------
const twilioTestDialogOpen = ref(false)
const twilioTestSending = ref(false)
const twilioTestResult = ref(null)
const twilioTestForm = reactive({ phone: '' })
const vTwilioTest$ = useVuelidate({ phone: { required, phone: phone() } }, twilioTestForm)

function onOpenTwilioTest () {
  twilioTestForm.phone = twilioConfig.value?.fromNumber || ''
  twilioTestResult.value = null
  vTwilioTest$.value.$reset()
  twilioTestDialogOpen.value = true
}

async function onRunTwilioTest () {
  const ok = await vTwilioTest$.value.$validate()
  if (!ok) return
  twilioTestSending.value = true
  twilioTesting.value = true
  twilioTestResult.value = null
  try {
    const result = await twilioApi.test()
    twilioTestResult.value = result
    if (result && result.success) notify.success(result.message || 'Twilio credentials verified')
    else notify.error((result && result.message) || 'Twilio test failed')
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    twilioTestSending.value = false
    twilioTesting.value = false
  }
}

onMounted(() => {
  loadSmtp()
  loadTwilio()
})
</script>
