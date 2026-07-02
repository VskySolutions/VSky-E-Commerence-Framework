<template>
  <AppFormDrawer
    :model-value="modelValue"
    :title="isEdit ? 'Edit manufacturer' : 'New manufacturer'"
    :saving="saving"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <AppTextField v-model="form.name" label="Name" required :v="v$.name" />
    <AppTextField v-model="form.slug" label="Slug" :v="v$.slug" />
    <AppTextField v-model="form.description" label="Description" type="textarea" autogrow />
    <AppTextField v-model="form.logoUrl" label="Logo URL" />

    <q-separator class="q-my-sm" />
    <div class="text-caption text-grey-7 q-mb-xs">SEO metadata</div>
    <AppTextField v-model="form.metaTitle" label="Meta title" />
    <AppTextField v-model="form.metaDescription" label="Meta description" type="textarea" autogrow />
    <AppTextField v-model="form.metaKeywords" label="Meta keywords" />

    <div class="row q-col-gutter-sm items-center q-mt-xs">
      <div class="col-6">
        <AppTextField v-model="form.displayOrder" label="Display order" type="number" />
      </div>
      <div class="col-6">
        <q-toggle v-model="form.isEnabled" label="Enabled" color="primary" />
      </div>
    </div>
  </AppFormDrawer>
</template>

<script setup>
/*
 * ManufacturerFormDrawer (WO-15): Vuelidate-validated create/edit form for a
 * manufacturer/brand (name, description, logo, slug, SEO metadata, ordering,
 * enabled state).
 */
import { reactive, computed, watch } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength } from 'validators'
import AppFormDrawer from 'components/common/AppFormDrawer.vue'
import AppTextField from 'components/common/AppTextField.vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  item: { type: Object, default: null },
  saving: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])

const isEdit = computed(() => !!(props.item && props.item.id))

const EMPTY = {
  name: '',
  slug: '',
  description: '',
  logoUrl: '',
  metaTitle: '',
  metaDescription: '',
  metaKeywords: '',
  displayOrder: 0,
  isEnabled: true
}
const form = reactive({ ...EMPTY })

const rules = {
  name: { required, maxLength: maxLength(200) },
  slug: { maxLength: maxLength(220) }
}
const v$ = useVuelidate(rules, form)

watch(
  () => props.item,
  (item) => {
    Object.assign(form, EMPTY, item || {})
    v$.value.$reset()
  },
  { immediate: true }
)

function toNumberOrZero (value) {
  const n = Number(value)
  return Number.isFinite(n) ? n : 0
}

async function onSubmit () {
  const ok = await v$.value.$validate()
  if (!ok) return
  emit('submit', {
    name: form.name,
    slug: form.slug || null,
    description: form.description || null,
    logoUrl: form.logoUrl || null,
    metaTitle: form.metaTitle || null,
    metaDescription: form.metaDescription || null,
    metaKeywords: form.metaKeywords || null,
    displayOrder: toNumberOrZero(form.displayOrder),
    isEnabled: form.isEnabled
  })
}
</script>
