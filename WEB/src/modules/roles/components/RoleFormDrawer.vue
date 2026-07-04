<template>
  <AppFormDrawer
    :model-value="modelValue"
    :title="isEdit ? 'Edit role' : 'New role'"
    :saving="saving"
    :validate="!readOnly"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <q-banner v-if="readOnly" dense rounded class="bg-grey-2 text-grey-8 q-mb-md">
      <template #avatar><q-icon name="o_lock" color="grey-7" /></template>
      System roles are managed by the platform and cannot be edited.
    </q-banner>

    <AppTextField v-model="form.name" label="Name" required :v="v$.name" :disable="readOnly" placeholder="e.g. Catalog Manager" />
    <AppTextField
      v-model="form.description"
      label="Description"
      type="textarea"
      autogrow
      :disable="readOnly"
      placeholder="A short note describing what this role is for"
    />

    <div class="app-field">
      <AppFieldLabel label="Modules" />
      <div class="text-body2 text-muted q-mb-sm">Modules this role can access.</div>
      <q-option-group
        v-if="moduleOptions.length"
        v-model="form.accessibleModules"
        :options="moduleOptions"
        type="checkbox"
        color="primary"
        :disable="readOnly"
      />
      <div v-else class="text-body2 text-grey-6 q-py-sm">
        {{ modulesLoading ? 'Loading modules…' : 'No modules available.' }}
      </div>
    </div>

    <template v-if="readOnly" #footer="{ cancel }">
      <q-btn flat label="Close" color="grey-8" @click="cancel" />
    </template>
  </AppFormDrawer>
</template>

<script setup>
/*
 * RoleFormDrawer (WO-62 / REQ-ADM-004): Vuelidate-validated create/edit form for
 * a custom role. Fields are Name + Description + a checkbox group of accessible
 * modules loaded from roleApi.modules(). System roles (isSystemRole) are shown
 * read-only (AC-ADM-004.5). Emits `submit` with { name, description,
 * accessibleModules } when valid.
 */
import { reactive, ref, computed, watch, onMounted } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, maxLength } from 'validators'
import { getApiErrorMessage } from 'services/api'
import { roleApi } from 'modules/roles/api'
import { useNotify } from 'composables/useNotify'
import AppFormDrawer from 'components/common/AppFormDrawer.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppFieldLabel from 'components/common/AppFieldLabel.vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  item: { type: Object, default: null },
  saving: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])

const notify = useNotify()

const isEdit = computed(() => !!(props.item && props.item.id))
const readOnly = computed(() => !!(props.item && props.item.isSystemRole))

const form = reactive({ name: '', description: '', accessibleModules: [] })

const rules = {
  name: { required, maxLength: maxLength(100) }
}
const v$ = useVuelidate(rules, form)

const moduleOptions = ref([])
const modulesLoading = ref(false)

async function loadModules () {
  modulesLoading.value = true
  try {
    const list = await roleApi.modules()
    const items = Array.isArray(list) ? list : list?.items || []
    moduleOptions.value = items.map((m) => ({ label: m.displayName, value: m.key }))
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    modulesLoading.value = false
  }
}

watch(
  () => props.item,
  (item) => {
    form.name = item?.name ?? ''
    form.description = item?.description ?? ''
    form.accessibleModules = Array.isArray(item?.accessibleModules) ? [...item.accessibleModules] : []
    v$.value.$reset()
  },
  { immediate: true }
)

onMounted(loadModules)

async function onSubmit () {
  if (readOnly.value) return
  const ok = await v$.value.$validate()
  if (!ok) return
  emit('submit', {
    name: form.name,
    description: form.description || null,
    accessibleModules: [...form.accessibleModules]
  })
}
</script>
