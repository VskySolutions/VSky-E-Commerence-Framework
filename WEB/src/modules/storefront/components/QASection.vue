<template>
  <div class="sf-qa">
    <div class="row items-center q-mb-md">
      <div class="col">
        <div class="text-h6">Questions &amp; Answers</div>
        <div class="text-grey-6 text-body2">Ask about this product — our team will respond.</div>
      </div>
      <q-btn
        color="primary"
        no-caps
        unelevated
        icon="o_help_outline"
        label="Ask a Question"
        @click="showForm = !showForm"
      />
    </div>

    <!-- Ask form (public) -->
    <q-slide-transition>
      <q-card v-if="showForm" flat bordered class="q-mb-lg">
        <q-form @submit.prevent="submit">
          <q-card-section class="q-gutter-md">
            <div class="row q-col-gutter-md">
              <q-input
                v-model="form.askerName"
                class="col-12 col-sm-6"
                dense
                outlined
                label="Your name"
                :rules="[(v) => !!(v && v.trim()) || 'Name is required']"
              />
              <q-input
                v-model="form.askerEmail"
                class="col-12 col-sm-6"
                dense
                outlined
                type="email"
                label="Email (optional)"
                hint="Only used to notify you when it's answered"
              />
            </div>
            <q-input
              v-model="form.questionText"
              dense
              outlined
              type="textarea"
              label="Your question"
              autogrow
              :rules="[(v) => !!(v && v.trim()) || 'Please enter your question']"
            />
          </q-card-section>
          <q-card-actions align="right">
            <q-btn flat no-caps label="Cancel" @click="showForm = false" />
            <q-btn
              type="submit"
              color="primary"
              unelevated
              no-caps
              label="Submit question"
              :loading="submitting"
            />
          </q-card-actions>
        </q-form>
      </q-card>
    </q-slide-transition>

    <!-- List -->
    <div v-if="loading" class="q-py-lg text-center">
      <q-spinner color="primary" size="28px" />
    </div>
    <div
      v-else-if="!questions.length"
      class="text-grey-6 q-py-lg text-center bg-grey-1 rounded-borders"
    >
      <q-icon name="o_forum" size="36px" class="q-mb-sm" />
      <div>No questions yet. Be the first to ask.</div>
    </div>
    <div v-else class="q-gutter-y-md">
      <div v-for="(q, i) in questions" :key="i" class="sf-qa__item">
        <div class="row no-wrap items-start q-gutter-sm">
          <q-icon name="o_help" color="primary" size="20px" class="sf-qa__icon" />
          <div class="col">
            <div class="text-weight-medium">{{ q.questionText }}</div>
            <div class="text-caption text-grey-6">
              {{ q.askerName || 'Anonymous' }}<template v-if="q.createdOnUtc"> · {{ formatDate(q.createdOnUtc) }}</template>
            </div>
          </div>
        </div>
        <div v-if="q.answerText" class="row no-wrap items-start q-gutter-sm q-mt-sm sf-qa__answer">
          <q-icon name="o_verified" color="positive" size="20px" class="sf-qa__icon" />
          <div class="col">
            <div class="text-body2">{{ q.answerText }}</div>
            <div class="text-caption text-grey-6">
              Answer<template v-if="q.answeredOnUtc"> · {{ formatDate(q.answeredOnUtc) }}</template>
            </div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
/*
 * Product questions & answers (WO-58). Lists APPROVED + answered questions and
 * offers a PUBLIC "Ask a question" form (name, optional email, question). The
 * submit posts anonymously with a reCAPTCHA token (form type 'QaSubmit') and the
 * question is queued for moderation.
 */
import { ref, reactive, watch, onMounted } from 'vue'
import { useNotify } from 'composables/useNotify'
import { getApiErrorMessage } from 'services/api'
import { formatDate } from 'src/utils/datetime'
import { questionsApi } from 'modules/storefront/product-engagement-api'
import { useRecaptcha } from 'modules/storefront/composables/useRecaptcha'

const props = defineProps({
  productId: { type: [String, Number], default: null }
})

const notify = useNotify()
const { getToken } = useRecaptcha()

const loading = ref(false)
const submitting = ref(false)
const showForm = ref(false)
const questions = ref([])
const form = reactive({ askerName: '', askerEmail: '', questionText: '' })

async function load () {
  if (!props.productId) return
  loading.value = true
  try {
    const res = await questionsApi.list(props.productId)
    questions.value = Array.isArray(res) ? res : (res && (res.items || res.questions)) || []
  } catch (e) {
    // Endpoint may not exist yet — degrade to an empty list.
    questions.value = []
  } finally {
    loading.value = false
  }
}

async function submit () {
  if (!form.askerName.trim() || !form.questionText.trim()) {
    notify.warning('Please enter your name and question.')
    return
  }
  submitting.value = true
  try {
    const recaptchaToken = await getToken('QaSubmit')
    await questionsApi.submit(props.productId, {
      askerName: form.askerName.trim(),
      askerEmail: form.askerEmail.trim() || null,
      questionText: form.questionText.trim(),
      recaptchaToken
    })
    notify.success('Thanks! Your question was submitted for review.')
    showForm.value = false
    form.askerName = ''
    form.askerEmail = ''
    form.questionText = ''
  } catch (e) {
    notify.error(getApiErrorMessage(e))
  } finally {
    submitting.value = false
  }
}

watch(() => props.productId, load)
onMounted(load)
</script>

<style scoped lang="scss">
.sf-qa__item {
  border: 1px solid var(--sf-border);
  border-radius: var(--sf-radius);
  padding: 14px 16px;
}
.sf-qa__icon {
  margin-top: 2px;
  flex: 0 0 auto;
}
.sf-qa__answer {
  padding-left: 8px;
  border-left: 2px solid var(--sf-border);
}
</style>
