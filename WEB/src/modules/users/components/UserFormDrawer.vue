<template>
  <AppFormDrawer
    :model-value="modelValue"
    :title="isEdit ? 'Edit user' : 'New user'"
    :saving="saving"
    @update:model-value="$emit('update:modelValue', $event)"
    @submit="onSubmit"
    @cancel="$emit('cancel')"
  >
    <AppTextField
      v-model="form.email"
      label="Email"
      type="email"
      required
      :v="v$.email"
      :disable="isEdit"
    />
    <AppTextField v-model="form.firstName" label="First name" required :v="v$.firstName" />
    <AppTextField v-model="form.lastName" label="Last name" :v="v$.lastName" />
    <AppTextField
      v-if="!isEdit"
      v-model="form.password"
      label="Password"
      type="password"
      required
      :v="v$.password"
    />
    <AppSelect
      v-model="form.roleId"
      label="Role"
      required
      :v="v$.roleId"
      :options="roleOptions"
      :loading="rolesLoading"
    />
    <q-toggle v-if="isEdit" v-model="form.isActive" label="Active" color="primary" />
  </AppFormDrawer>
</template>

<script setup>
/*
 * UserFormDrawer (WO-62 / REQ-ADM-004): Vuelidate-validated create/edit form for
 * an admin user. Create collects email, first/last name, password and a single
 * role; edit allows changing the name, status and role (email + password are
 * fixed). Role is a single-select (AC-ADM-004.3) that the page wraps into a
 * one-element RoleIds list for the API. Emits `submit` with the raw field values.
 *
 * The list/get DTO only exposes a concatenated fullName, so on edit it is split
 * back into first/last (first token = first name, remainder = last name).
 */
import { reactive, ref, computed, watch, onMounted } from 'vue'
import useVuelidate from '@vuelidate/core'
import { required, email, minLength, maxLength } from 'validators'
import { getApiErrorMessage } from 'services/api'
import { userApi } from 'modules/users/api'
import { useNotify } from 'composables/useNotify'
import AppFormDrawer from 'components/common/AppFormDrawer.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppSelect from 'components/common/AppSelect.vue'

const props = defineProps({
  modelValue: { type: Boolean, default: false },
  item: { type: Object, default: null },
  saving: { type: Boolean, default: false }
})

const emit = defineEmits(['update:modelValue', 'submit', 'cancel'])

const notify = useNotify()

const isEdit = computed(() => !!(props.item && props.item.id))

const form = reactive({
  email: '',
  firstName: '',
  lastName: '',
  password: '',
  roleId: null,
  isActive: true
})

const rules = computed(() => ({
  email: { required, email, maxLength: maxLength(256) },
  firstName: { required, maxLength: maxLength(200) },
  lastName: { maxLength: maxLength(200) },
  password: isEdit.value ? {} : { required, minLength: minLength(8) },
  roleId: { required }
}))
const v$ = useVuelidate(rules, form)

const roleOptions = ref([])
const rolesLoading = ref(false)

async function loadRoles () {
  rolesLoading.value = true
  try {
    const list = await userApi.roles()
    const items = Array.isArray(list) ? list : list?.items || []
    roleOptions.value = items.map((r) => ({ label: r.name, value: r.id }))
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    rolesLoading.value = false
  }
}

watch(
  () => props.item,
  (item) => {
    form.email = item?.email ?? ''
    form.password = ''
    form.roleId = item?.roles && item.roles.length ? item.roles[0].id : null
    form.isActive = item?.isActive ?? true
    const parts = (item?.fullName || '').trim().split(/\s+/).filter(Boolean)
    form.firstName = parts.shift() || ''
    form.lastName = parts.join(' ')
    v$.value.$reset()
  },
  { immediate: true }
)

onMounted(loadRoles)

async function onSubmit () {
  const ok = await v$.value.$validate()
  if (!ok) return
  emit('submit', {
    email: form.email,
    firstName: form.firstName,
    lastName: form.lastName,
    password: form.password,
    roleId: form.roleId,
    isActive: form.isActive
  })
}
</script>
