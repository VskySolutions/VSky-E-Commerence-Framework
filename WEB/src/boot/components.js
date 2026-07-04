/*
 * Global component registration (WO-94 Step 4 / Step 10).
 *
 * Registers the reusable App* building blocks globally so feature modules can
 * drop them into templates without repetitive imports. Add new shared,
 * app-wide components here.
 */
import AppFieldLabel from 'components/common/AppFieldLabel.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppSelect from 'components/common/AppSelect.vue'
import AppDateField from 'components/common/AppDateField.vue'
import AppListHeader from 'components/common/AppListHeader.vue'
import AppDetailHeader from 'components/common/AppDetailHeader.vue'
import AppSection from 'components/common/AppSection.vue'
import AppDataTable from 'components/common/AppDataTable.vue'
import AppDrawer from 'components/common/AppDrawer.vue'
import AppFormDrawer from 'components/common/AppFormDrawer.vue'
import AppFilterDrawer from 'components/common/AppFilterDrawer.vue'
import AppViewDrawer from 'components/common/AppViewDrawer.vue'
import AppRichText from 'components/common/AppRichText.vue'
import AppFileUpload from 'components/common/AppFileUpload.vue'
import ConfirmDialog from 'components/common/ConfirmDialog.vue'

// NOTE: AppPhoneInput (libphonenumber-js) and AppAddressFields
// (country-state-city, ~large city dataset) are intentionally NOT registered
// globally so their heavy data deps stay out of the initial bundle and are
// only loaded when a page imports them directly.
const globalComponents = {
  AppFieldLabel,
  AppTextField,
  AppSelect,
  AppDateField,
  AppListHeader,
  AppDetailHeader,
  AppSection,
  AppDataTable,
  AppDrawer,
  AppFormDrawer,
  AppFilterDrawer,
  AppViewDrawer,
  AppRichText,
  AppFileUpload,
  ConfirmDialog
}

export default ({ app }) => {
  for (const [name, component] of Object.entries(globalComponents)) {
    app.component(name, component)
  }
}
