<template>
  <AppFormDrawer
    :model-value="modelValue"
    :title="isEdit ? 'Edit category' : 'New category'"
    :saving="saving"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <AppTextField v-model="form.name" label="Name" required :v="v$.name" />
    <AppSelect
      v-model="form.parentId"
      label="Parent category"
      :options="availableParents"
      clearable
    />
    <AppTextField v-model="form.slug" label="Slug" :v="v$.slug" />
    <AppTextField v-model="form.description" label="Description" type="textarea" autogrow />

    <q-separator class="q-my-sm" />
    <div class="text-caption text-grey-7 q-mb-xs">SEO metadata</div>
    <AppTextField v-model="form.metaTitle" label="Meta title" />
    <AppTextField v-model="form.metaDescription" label="Meta description" type="textarea" autogrow />
    <AppTextField v-model="form.metaKeywords" label="Meta keywords" />
    <AppTextField v-model="form.canonicalUrl" label="Canonical URL" />

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
 * CategoryFormDrawer (WO-15): create/edit a catalog category (name, parent, slug,
 * SEO metadata, display order, enabled). Parent options are supplied by the page
 * (the flattened tree); the current node is excluded to avoid a self-parent.
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
  saving: { type: Boolean, default: false },
  parentOptions: { type: Array, default: () => [] }
})

const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])

const isEdit = computed(() => !!(props.item && props.item.id))

const EMPTY = {
  name: '',
  parentId: null,
  slug: '',
  description: '',
  metaTitle: '',
  metaDescription: '',
  metaKeywords: '',
  canonicalUrl: '',
  displayOrder: 0,
  isEnabled: true
}
const form = reactive({ ...EMPTY })

const rules = {
  name: { required, maxLength: maxLength(200) },
  slug: { maxLength: maxLength(220) }
}
const v$ = useVuelidate(rules, form)

// Exclude the current category from parent options (cannot be its own parent).
const availableParents = computed(() =>
  props.parentOptions.filter((o) => !props.item || o.value !== props.item.id)
)

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
    parentId: form.parentId || null,
    slug: form.slug || null,
    description: form.description || null,
    metaTitle: form.metaTitle || null,
    metaDescription: form.metaDescription || null,
    metaKeywords: form.metaKeywords || null,
    canonicalUrl: form.canonicalUrl || null,
    displayOrder: toNumberOrZero(form.displayOrder),
    isEnabled: form.isEnabled
  })
}
</script>
