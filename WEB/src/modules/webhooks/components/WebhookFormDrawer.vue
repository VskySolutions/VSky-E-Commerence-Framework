<template>
  <AppFormDrawer
    :model-value="modelValue"
    :title="isEdit ? 'Edit webhook' : 'New webhook'"
    :saving="saving"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <AppTextField
      v-model="form.url"
      label="Payload URL"
      required
      :v="v$.url"
      placeholder="https://example.com/webhooks/vsky"
      hint="The https endpoint that will receive signed POST requests"
    />

    <AppFieldLabel label="Events" />
    <q-select
      v-model="form.eventTypes"
      multiple use-chips dense outlined emit-value map-options
      :options="eventOptions"
      placeholder="Select one or more events"
      hint="The domain events that trigger a delivery to this endpoint"
      :error="v$.eventTypes.$error"
      class="q-mb-md"
      @blur="v$.eventTypes.$touch()"
    />

    <AppTextField
      v-model="form.description"
      label="Description"
      placeholder="e.g. Notify fulfilment service of new orders"
      hint="Optional note to identify this endpoint"
    />

    <q-toggle v-if="isEdit" v-model="form.isActive" label="Active" color="primary" />
  </AppFormDrawer>
</template>

<script setup>
/*
 * WebhookFormDrawer (WO-118): create/edit a webhook subscription — payload URL, subscribed events
 * (from the server's event-type catalog), description and (on edit) the active toggle. The signing
 * secret is never edited here; it is shown once by the parent right after creation.
 */
import { reactive, ref, computed, watch, onMounted } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength, url as urlRule } from 'validators'
import { webhookApi } from 'modules/webhooks/api'
import AppFormDrawer from 'components/common/AppFormDrawer.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppFieldLabel from 'components/common/AppFieldLabel.vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  item: { type: Object, default: null },
  saving: { type: Boolean, default: false }
})
const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])

const isEdit = computed(() => !!(props.item && props.item.id))

const EMPTY = { url: '', eventTypes: [], description: '', isActive: true }
const form = reactive({ ...EMPTY })
const eventOptions = ref([])

const rules = {
  url: { required, maxLength: maxLength(2048), url: urlRule },
  eventTypes: { required }
}
const v$ = useVuelidate(rules, form)

watch(() => props.item, (item) => {
  Object.assign(form, EMPTY, item ? {
    url: item.url,
    eventTypes: [...(item.eventTypes || [])],
    description: item.description || '',
    isActive: item.isActive
  } : {})
  v$.value.$reset()
}, { immediate: true })

async function loadEventTypes () {
  try {
    const types = await webhookApi.eventTypes()
    eventOptions.value = (Array.isArray(types) ? types : []).map((t) => ({ label: t, value: t }))
  } catch (e) { eventOptions.value = [] }
}

async function onSubmit () {
  const ok = await v$.value.$validate()
  if (!ok) return
  emit('submit', {
    url: form.url.trim(),
    eventTypes: form.eventTypes,
    description: form.description?.trim() || null,
    isActive: form.isActive
  })
}

onMounted(loadEventTypes)
</script>
