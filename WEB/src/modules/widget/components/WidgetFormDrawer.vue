<template>
  <AppFormDrawer
    :model-value="modelValue"
    :title="isEdit ? 'Edit widget' : 'New widget'"
    :saving="saving"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <AppTextField v-model="form.name" label="Name" required :v="v$.name" />
    <AppTextField v-model="form.slug" label="Slug" :v="v$.slug" />
    <AppTextField
      v-model="form.description"
      label="Description"
      type="textarea"
      autogrow
    />
    <AppSelect v-model="form.status" label="Status" :options="statusOptions" />
    <q-toggle v-model="form.isActive" label="Active" color="primary" />
  </AppFormDrawer>
</template>

<script setup>
/*
 * WidgetFormDrawer (WO-94 Step 12): a Vuelidate-validated create/edit form
 * inside AppFormDrawer. Emits `submit` with the payload when valid.
 */
import { reactive, computed, watch } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength } from 'validators'
import AppFormDrawer from 'components/common/AppFormDrawer.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppSelect from 'components/common/AppSelect.vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  item: { type: Object, default: null },
  saving: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])

const isEdit = computed(() => !!(props.item && props.item.id))

const EMPTY = { name: '', slug: '', description: '', status: 'draft', isActive: true }
const form = reactive({ ...EMPTY })

const rules = {
  name: { required, maxLength: maxLength(120) },
  slug: { maxLength: maxLength(120) }
}
const v$ = useVuelidate(rules, form)

const statusOptions = [
  { label: 'Draft', value: 'draft' },
  { label: 'Published', value: 'published' },
  { label: 'Archived', value: 'archived' }
]

watch(
  () => props.item,
  (item) => {
    Object.assign(form, EMPTY, item || {})
    v$.value.$reset()
  },
  { immediate: true }
)

async function onSubmit () {
  const ok = await v$.value.$validate()
  if (!ok) return
  emit('submit', { ...form })
}
</script>
