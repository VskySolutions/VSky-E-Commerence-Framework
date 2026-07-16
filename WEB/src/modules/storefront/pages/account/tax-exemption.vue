<template>
  <div class="q-gutter-md tax-exemption">
    <q-inner-loading :showing="loading" />

    <template v-if="data">
      <!-- Status -->
      <q-card flat bordered>
        <q-card-section class="row items-center justify-between">
          <div class="text-subtitle1 text-weight-medium">Tax exemption</div>
          <q-chip :color="statusMeta.color" text-color="white" :icon="statusMeta.icon" :label="statusMeta.label" />
        </q-card-section>
        <q-separator />
        <q-card-section class="q-gutter-sm">
          <div class="text-body2 text-grey-8">{{ statusMeta.description }}</div>

          <div v-if="status === 'PendingReview' && latest" class="text-caption text-grey-6">
            Submitted {{ formatDate(latest.submittedOnUtc) }}
          </div>

          <q-banner v-if="status === 'Rejected' && latest && latest.adminNote" class="bg-red-1 text-red-9" rounded dense>
            <template #avatar><q-icon name="o_info" color="negative" /></template>
            {{ latest.adminNote }}
          </q-banner>

          <!-- Latest request details -->
          <template v-if="latest">
            <q-separator class="q-my-sm" />
            <div class="row q-col-gutter-md">
              <div v-if="latest.certificateNumber" class="col-12 col-sm-6">
                <div class="text-caption text-grey-6">Certificate number</div>
                <div class="text-body2">{{ latest.certificateNumber }}</div>
              </div>
              <div v-if="latest.vatId" class="col-12 col-sm-6">
                <div class="text-caption text-grey-6">VAT ID</div>
                <div class="text-body2">{{ latest.vatId }}</div>
              </div>
            </div>
            <div v-if="latest.documents && latest.documents.length" class="q-mt-sm">
              <div class="text-caption text-grey-6 q-mb-xs">Documents</div>
              <div class="column q-gutter-xs items-start">
                <a
                  v-for="doc in latest.documents"
                  :key="doc.id"
                  :href="$media(doc.url)"
                  target="_blank"
                  rel="noopener"
                  class="text-primary row items-center no-wrap"
                >
                  <q-icon name="o_description" size="18px" class="q-mr-xs" />
                  <span>{{ doc.fileName }}</span>
                </a>
              </div>
            </div>
          </template>
        </q-card-section>
      </q-card>

      <!-- Submit / re-submit -->
      <q-card v-if="canSubmit" flat bordered>
        <q-card-section class="text-subtitle1 text-weight-medium">
          {{ status === 'Rejected' ? 'Submit a new request' : 'Submit a request' }}
        </q-card-section>
        <q-separator />
        <q-form @submit.prevent="onSubmit">
          <q-card-section class="q-gutter-md">
            <div class="text-body2 text-grey-7">
              Provide your tax certificate number or VAT ID and upload supporting documentation. At least one identifier
              and one document are required.
            </div>

            <div class="row q-col-gutter-md">
              <div class="col-12 col-sm-6">
                <AppTextField v-model="form.certificateNumber" label="Certificate number" />
              </div>
              <div class="col-12 col-sm-6">
                <AppTextField v-model="form.vatId" label="VAT ID" />
              </div>
            </div>

            <!-- Documents -->
            <div>
              <q-file
                v-model="fileModel"
                label="Add a document"
                outlined
                dense
                accept=".pdf,.jpg,.jpeg,.png,.webp"
                max-file-size="5242880"
                :loading="uploading"
                :disable="uploading"
                hint="PDF, JPG, PNG or WEBP. Max 5 MB each."
                @update:model-value="onFileSelected"
                @rejected="onRejected"
              >
                <template #prepend><q-icon name="o_attach_file" /></template>
              </q-file>

              <div v-if="documents.length" class="q-mt-sm q-gutter-xs">
                <q-chip
                  v-for="(doc, i) in documents"
                  :key="doc.mediaId"
                  removable
                  color="grey-3"
                  text-color="grey-9"
                  icon="o_description"
                  :label="doc.fileName"
                  @remove="removeDocument(i)"
                />
              </div>
            </div>
          </q-card-section>
          <q-card-actions align="right" class="q-px-md q-pb-md">
            <q-btn
              type="submit"
              color="primary"
              unelevated
              no-caps
              label="Submit request"
              :loading="submitting"
              :disable="!canSend || uploading"
            />
          </q-card-actions>
        </q-form>
      </q-card>
    </template>
  </div>
</template>

<script setup>
import { ref, reactive, computed, onMounted } from 'vue'
import { useQuasar } from 'quasar'
import { accountApi } from 'modules/storefront/account-api'
import { getApiErrorMessage } from 'services/api'
import { formatDateTime as formatDate } from 'src/utils/datetime'

const $q = useQuasar()

const loading = ref(false)
const submitting = ref(false)
const uploading = ref(false)

const data = ref(null)
const form = reactive({ certificateNumber: '', vatId: '' })
const fileModel = ref(null)
const documents = ref([]) // [{ mediaId, fileName, url }]

const status = computed(() => (data.value && data.value.status) || 'NotSubmitted')
const latest = computed(() => (data.value && data.value.latestRequest) || null)
const canSubmit = computed(() => !!(data.value && data.value.canSubmit))
const hasCertOrVat = computed(() => !!(form.certificateNumber.trim() || form.vatId.trim()))
const canSend = computed(() => documents.value.length > 0 && hasCertOrVat.value)

const statusMeta = computed(() => {
  switch (status.value) {
    case 'PendingReview':
      return {
        color: 'orange',
        icon: 'o_hourglass_top',
        label: 'Pending review',
        description: "Your request is being reviewed. We'll email you once a decision is made."
      }
    case 'Approved':
      return {
        color: 'green',
        icon: 'o_verified',
        label: 'Approved',
        description: 'Approved — tax will not be charged at checkout.'
      }
    case 'Rejected':
      return {
        color: 'red',
        icon: 'o_cancel',
        label: 'Rejected',
        description: 'Your request was rejected. You can submit a new request below.'
      }
    default:
      return {
        color: 'grey',
        icon: 'o_receipt_long',
        label: 'Not submitted',
        description: 'You have not submitted a tax exemption request yet.'
      }
  }
})

async function load () {
  loading.value = true
  try {
    data.value = await accountApi.taxExemption()
  } catch (e) {
    $q.notify({ type: 'negative', message: getApiErrorMessage(e) })
  } finally {
    loading.value = false
  }
}

// Auto-upload each selected file, then collect the returned media reference.
async function onFileSelected (file) {
  if (!file) return
  uploading.value = true
  try {
    const res = await accountApi.uploadTaxDocument(file)
    documents.value.push({ mediaId: res.mediaId, fileName: res.fileName, url: res.url })
  } catch (e) {
    $q.notify({ type: 'negative', message: getApiErrorMessage(e) })
  } finally {
    uploading.value = false
    fileModel.value = null // reset so the next file (or the same one) can be picked
  }
}

function removeDocument (i) {
  documents.value.splice(i, 1)
}

function onRejected () {
  $q.notify({ type: 'negative', message: 'File rejected — must be a PDF, JPG, PNG or WEBP under 5 MB.' })
}

async function onSubmit () {
  if (!hasCertOrVat.value) {
    $q.notify({ type: 'negative', message: 'Enter a certificate number or VAT ID.' })
    return
  }
  if (!documents.value.length) {
    $q.notify({ type: 'negative', message: 'Upload at least one supporting document.' })
    return
  }
  submitting.value = true
  try {
    await accountApi.submitTaxExemption({
      certificateNumber: form.certificateNumber.trim() || null,
      vatId: form.vatId.trim() || null,
      documentMediaIds: documents.value.map((d) => d.mediaId)
    })
    $q.notify({ type: 'positive', message: 'Tax exemption request submitted.' })
    form.certificateNumber = ''
    form.vatId = ''
    documents.value = []
    fileModel.value = null
    await load()
  } catch (e) {
    $q.notify({ type: 'negative', message: getApiErrorMessage(e) })
  } finally {
    submitting.value = false
  }
}

onMounted(load)
</script>

<style scoped>
.tax-exemption {
  position: relative;
  min-height: 160px;
}
</style>
