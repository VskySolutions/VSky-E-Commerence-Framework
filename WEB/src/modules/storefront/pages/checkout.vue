<template>
  <q-page class="q-pa-md storefront-container">
    <div class="text-h5 text-weight-bold q-mb-md">Checkout</div>

    <q-inner-loading :showing="loading && !cart" color="primary" />

    <!-- Order confirmation (AC-CHK-003.8) -->
    <q-card v-if="orderResult && orderResult.success" flat bordered class="q-mx-auto confirm-card">
      <q-card-section class="text-center q-pa-xl">
        <q-icon name="o_check_circle" color="positive" size="72px" />
        <div class="text-h5 text-weight-bold q-mt-md">Thank you for your order!</div>
        <div class="text-body1 text-grey-8 q-mt-sm">
          Your order <span class="text-weight-bold text-dark">{{ orderResult.orderNumber }}</span> has been placed.
        </div>
        <div class="row justify-center q-gutter-xl q-mt-lg">
          <div>
            <div class="text-caption text-grey-6">Order total</div>
            <div class="text-subtitle1 text-weight-bold">{{ format(orderResult.total) }}</div>
          </div>
          <div>
            <div class="text-caption text-grey-6">Payment</div>
            <div class="text-subtitle1 text-weight-bold">{{ orderResult.paymentStatus }}</div>
          </div>
        </div>
        <div class="text-caption text-grey-6 q-mt-lg">
          A confirmation email with your order details is on its way.
        </div>
        <q-btn
          unelevated
          color="primary"
          no-caps
          label="Continue shopping"
          class="q-mt-lg"
          :to="{ name: 'shop-home' }"
        />
      </q-card-section>
    </q-card>

    <!-- Empty cart -->
    <q-banner
      v-else-if="cart && !items.length"
      class="bg-grey-2 rounded-borders q-my-md"
    >
      Your cart is empty — there is nothing to check out.
      <template #action>
        <q-btn flat no-caps color="primary" label="Continue shopping" :to="{ name: 'shop-home' }" />
      </template>
    </q-banner>

    <!-- Checkout form -->
    <div v-else-if="items.length" class="row q-col-gutter-lg">
      <!-- Left: address, shipping, payment -->
      <div class="col-12 col-md-7">
        <q-form ref="formRef" greedy>
          <!-- Contact + delivery address -->
          <q-card flat bordered class="q-mb-lg">
            <q-card-section>
              <div class="text-subtitle1 text-weight-bold q-mb-md">Contact &amp; delivery address</div>

              <!-- Signed-in: pick a saved address or add a new one -->
              <div v-if="isAuthed && savedAddresses.length" class="column q-gutter-sm q-mb-md">
                <q-item
                  v-for="a in savedAddresses"
                  :key="a.id"
                  tag="label"
                  clickable
                  class="sf-address-option rounded-borders"
                  :class="{ 'sf-address-option--active': selectedAddressId === a.id }"
                >
                  <q-item-section avatar top>
                    <q-radio :model-value="selectedAddressId" :val="a.id" @update:model-value="selectSaved(a)" />
                  </q-item-section>
                  <q-item-section>
                    <q-item-label class="text-weight-medium">
                      {{ a.firstName }} {{ a.lastName }}
                      <q-badge v-if="a.isDefault" color="primary" label="Default" class="q-ml-xs" />
                    </q-item-label>
                    <q-item-label caption>{{ formatAddr(a) }}</q-item-label>
                    <q-item-label v-if="a.phoneNumber" caption>{{ a.phoneNumber }}</q-item-label>
                  </q-item-section>
                </q-item>

                <q-item
                  tag="label"
                  clickable
                  class="sf-address-option rounded-borders"
                  :class="{ 'sf-address-option--active': selectedAddressId === 'new' }"
                >
                  <q-item-section avatar>
                    <q-radio :model-value="selectedAddressId" val="new" @update:model-value="selectNew()" />
                  </q-item-section>
                  <q-item-section>
                    <q-item-label class="text-primary text-weight-medium">
                      <q-icon name="o_add" size="18px" class="q-mr-xs" />Use a new address
                    </q-item-label>
                  </q-item-section>
                </q-item>
              </div>

              <!-- Contact recap when a saved address is in use -->
              <div v-if="isAuthed && !showAddressForm" class="text-caption text-grey-7 q-mb-sm">
                Order updates go to <strong>{{ profileEmail }}</strong>
              </div>

              <!-- Manual entry: guest, no saved addresses, or "new address" chosen -->
              <template v-if="showAddressForm">
                <div class="row q-col-gutter-x-md q-mb-sm">
                  <div class="col-12 col-sm-6">
                    <AppTextField
                      label="Email"
                      type="email"
                      required
                      :model-value="email"
                      :v="emailV"
                      placeholder="you@example.com"
                      @update:model-value="email = $event"
                    />
                  </div>
                  <div class="col-12 col-sm-6">
                    <AppPhoneInput
                      label="Phone"
                      required
                      :model-value="addr.phoneNumber"
                      :default-country="addr.countryCode || 'US'"
                      @update:model-value="addr.phoneNumber = $event"
                    />
                  </div>
                </div>
                <AppAddressForm v-model="addr" required :show-company="false" :show-phone="false" />
                <q-toggle
                  v-if="isAuthed"
                  v-model="saveNewAddress"
                  label="Save this address to my account"
                  dense
                  class="q-mt-sm"
                />
              </template>

              <div class="q-mt-md">
                <q-btn
                  unelevated
                  color="primary"
                  no-caps
                  :label="quote ? 'Recalculate shipping' : 'Save & calculate shipping'"
                  icon="o_local_shipping"
                  :loading="quoting"
                  :disable="!canQuote || quoting"
                  @click="runQuote"
                />
              </div>
            </q-card-section>
          </q-card>
        </q-form>

        <!-- Address hint before a quote is available -->
        <q-banner v-if="!quote && !quoting && !quoteError" dense class="bg-blue-1 text-blue-9 rounded-borders q-mb-lg">
          <template #avatar><q-icon name="o_info" color="blue-9" /></template>
          Complete your delivery address, then <strong>Save &amp; calculate shipping</strong> to see options and live totals.
        </q-banner>

        <q-banner v-if="quoteError" dense class="bg-red-1 text-red-9 rounded-borders q-mb-lg">
          <template #avatar><q-icon name="o_error" color="red-9" /></template>
          {{ quoteError }}
        </q-banner>

        <!-- Not routable to any store -->
        <q-banner
          v-if="quote && !isRoutable"
          dense
          class="bg-orange-1 text-orange-9 rounded-borders q-mb-lg"
        >
          <template #avatar><q-icon name="o_local_shipping" color="orange-9" /></template>
          We're unable to deliver to this address right now. Please try a different address.
        </q-banner>

        <!-- Guest ordering not permitted — offer express account creation (AC-CHK-003.2 / AC-STR-001.5) -->
        <q-card
          v-if="quote && isRoutable && !guestOrderingAllowed"
          flat
          bordered
          class="q-mb-lg bg-amber-1"
        >
          <q-card-section>
            <div class="row items-center no-wrap q-mb-sm">
              <q-icon name="o_lock" color="amber-9" size="28px" class="q-mr-md" />
              <div class="col">
                <div class="text-subtitle2 text-weight-bold">One quick step to place your order</div>
                <div class="text-body2 text-grey-8">
                  This store requires an account. We'll create one from the details above and email you a password —
                  sign in and your cart &amp; details will be right here to finish, no re-typing.
                </div>
              </div>
            </div>
            <div class="row items-center justify-end q-gutter-sm">
              <q-btn flat no-caps color="primary" label="I already have an account" :to="loginTo" />
              <q-btn
                unelevated
                color="primary"
                no-caps
                icon="o_person_add"
                label="Create account &amp; finish"
                :loading="registering"
                :disable="!canQuote || registering"
                @click="registerAndContinue"
              />
            </div>
          </q-card-section>
        </q-card>

        <!-- Shipping method -->
        <q-card v-if="canCollectDetails" flat bordered class="q-mb-lg">
          <q-card-section>
            <div class="text-subtitle1 text-weight-bold q-mb-sm">Shipping method</div>
            <q-inner-loading :showing="quoting" color="primary" />

            <q-list v-if="shippingOptions.length" separator>
              <q-item
                v-for="opt in shippingOptions"
                :key="opt.methodId"
                tag="label"
                clickable
              >
                <q-item-section avatar>
                  <q-radio
                    :model-value="selectedShippingMethodId"
                    :val="opt.methodId"
                    color="primary"
                    @update:model-value="onShippingChange"
                  />
                </q-item-section>
                <q-item-section>
                  <q-item-label>{{ opt.name }}</q-item-label>
                  <q-item-label caption>
                    {{ opt.carrier }}
                    <span v-if="opt.estimatedDeliveryDays"> · {{ opt.estimatedDeliveryDays }} day(s)</span>
                  </q-item-label>
                </q-item-section>
                <q-item-section side>
                  <span class="text-weight-medium text-dark">{{ format(opt.rate) }}</span>
                </q-item-section>
              </q-item>
            </q-list>

            <div v-else-if="!quoting" class="text-body2 text-grey-7">
              No shipping is required for this order — you can place it directly.
            </div>
          </q-card-section>
        </q-card>

        <!-- Payment method (AC-PAY-001.4/5) -->
        <q-card v-if="canCollectDetails" flat bordered class="q-mb-lg">
          <q-card-section>
            <div class="text-subtitle1 text-weight-bold q-mb-sm">Payment method</div>
            <q-option-group v-model="paymentMethod" :options="paymentMethods" color="primary" />
            <div class="text-caption text-grey-6 q-mt-sm">
              You'll be charged when your order is placed. No card details are stored by the storefront.
            </div>
          </q-card-section>
        </q-card>
      </div>

      <!-- Right: order summary -->
      <div class="col-12 col-md-5">
        <q-card flat bordered class="summary-card">
          <q-card-section>
            <div class="text-subtitle1 text-weight-bold q-mb-md">Order summary</div>

            <!-- Line items -->
            <div v-for="item in items" :key="item.id" class="row items-start q-mb-sm">
              <div class="col">
                <div class="text-body2">{{ item.productName }}</div>
                <div class="text-caption text-grey-6">Qty {{ item.quantity }} · {{ format(item.unitPrice) }}</div>
              </div>
              <div class="text-body2 text-weight-medium">{{ format(item.lineTotal) }}</div>
            </div>

            <q-separator class="q-my-md" />

            <div v-if="appliedCouponCode" class="row items-center q-mb-sm">
              <q-chip
                dense
                color="green-1"
                text-color="green-9"
                icon="o_local_offer"
                :label="appliedCouponCode"
              />
            </div>

            <!-- Totals -->
            <div class="row items-center justify-between q-mb-xs">
              <span class="text-grey-8">Subtotal</span>
              <span>{{ format(quote ? quote.subtotal : subtotal) }}</span>
            </div>

            <template v-if="quote">
              <div
                v-for="d in quote.discounts"
                :key="d.discountId"
                class="row items-center justify-between q-mb-xs text-green-8"
              >
                <span>{{ d.name || 'Discount' }}</span>
                <span>-{{ format(d.amount) }}</span>
              </div>
              <div
                v-if="quote.discountTotal > 0 && !quote.discounts.length"
                class="row items-center justify-between q-mb-xs text-green-8"
              >
                <span>Discount</span>
                <span>-{{ format(quote.discountTotal) }}</span>
              </div>

              <div class="row items-center justify-between q-mb-xs">
                <span class="text-grey-8">
                  Shipping<span v-if="selectedShipping"> ({{ selectedShipping.name }})</span>
                </span>
                <span>{{ format(quote.shippingTotal) }}</span>
              </div>

              <div class="row items-center justify-between q-mb-xs">
                <span class="text-grey-8">Tax</span>
                <span>{{ format(quote.taxTotal) }}</span>
              </div>
              <div v-if="quote.tax && quote.tax.fallbackApplied" class="text-caption text-grey-6 q-mb-xs">
                Estimated tax — a fallback rate was applied.
              </div>

              <q-separator class="q-my-sm" />

              <div class="row items-center justify-between text-subtitle1 text-weight-bold">
                <span>Total</span>
                <span>{{ format(quote.total) }}</span>
              </div>
            </template>

            <div v-else class="text-caption text-grey-6 q-mt-sm">
              Shipping and taxes are calculated once your address is complete.
            </div>

            <!-- Place order / retry -->
            <q-banner v-if="placeError" dense class="bg-red-1 text-red-9 rounded-borders q-mt-md">
              <template #avatar><q-icon name="o_error" color="red-9" /></template>
              {{ placeError }}
            </q-banner>

            <q-btn
              v-if="!(quote && isRoutable && !guestOrderingAllowed)"
              unelevated
              color="primary"
              class="full-width q-mt-md"
              no-caps
              :label="placeError ? 'Retry payment' : 'Place order'"
              icon-right="o_lock"
              :loading="placing"
              :disable="!canPlace"
              @click="placeOrder"
            />
            <q-btn
              v-else
              unelevated
              color="primary"
              class="full-width q-mt-md"
              no-caps
              label="Sign in to continue"
              :to="loginTo"
            />

            <q-btn
              flat
              no-caps
              color="primary"
              class="full-width q-mt-sm"
              icon="o_arrow_back"
              label="Back to cart"
              :to="{ name: 'shop-cart' }"
            />
          </q-card-section>
        </q-card>
      </div>
    </div>
  </q-page>
</template>

<script setup>
/*
 * Storefront one-page checkout (WO-30, REQ-CHK-003). A single view that collects
 * the contact + delivery address and, as soon as it is complete, calls
 * checkout/quote to price the cart — routing result, shipping options, discounts,
 * tax and live totals (AC-CHK-003.7). Choosing a shipping method re-quotes so the
 * totals stay real-time. A payment-method selector plus a live order summary lead
 * to checkout/place, which shows an order-confirmation panel on success
 * (AC-CHK-003.8) or a retryable error on a declined payment (AC-PAY-001.3). When
 * the quote reports guest ordering is not allowed, the buyer is prompted to sign
 * in instead (AC-CHK-003.2 / AC-STR-001.5).
 */
import { ref, computed, watch, onMounted } from 'vue'
import { LocalStorage } from 'quasar'
import { useRouter, useRoute } from 'vue-router'
import AppAddressForm from 'components/common/AppAddressForm.vue'
import AppTextField from 'components/common/AppTextField.vue'
import AppPhoneInput from 'components/common/AppPhoneInput.vue'
import { isPossiblePhoneNumber } from 'libphonenumber-js'
import { emptyAddress } from 'composables/useAddress'
import { checkoutApi } from 'modules/storefront/api'
import { customerAuthApi, accountApi } from 'modules/storefront/account-api'
import { useCustomerAuthStore } from 'stores/customerAuth'
import { useCart } from 'modules/storefront/composables/useCart'
import { useCurrency } from 'modules/storefront/composables/useCurrency'
import { useRecaptcha } from 'modules/storefront/composables/useRecaptcha'
import { useNotify } from 'composables/useNotify'
import { getApiErrorMessage } from 'services/api'

const { sessionId, cart, items, subtotal, appliedCouponCode, loading, refresh } = useCart()
const { format, load: loadCurrencies } = useCurrency()
const { getToken: getRecaptchaToken } = useRecaptcha()
const notify = useNotify()
const router = useRouter()
const route = useRoute()
const customerAuth = useCustomerAuthStore()

// ---- Address form (standard AppAddressForm; email kept separate as the order contact) ----------
const formRef = ref(null)
const email = ref('')
const addr = ref(emptyAddress())

// ---- Signed-in address book (Amazon-style: pick a saved address or add a new one) ---------------
const isAuthed = computed(() => customerAuth.isAuthenticated)
const savedAddresses = ref([])
const selectedAddressId = ref('new') // a saved address id, or 'new' for manual entry
const profile = ref(null)
const profileEmail = ref('')
const saveNewAddress = ref(true)

// Show the manual form for guests, signed-in users with no saved address, or when "new" is chosen.
const showAddressForm = computed(() =>
  !isAuthed.value || !savedAddresses.value.length || selectedAddressId.value === 'new'
)

function fillFromAddress (a) {
  addr.value = {
    ...emptyAddress(),
    firstName: a.firstName || '', lastName: a.lastName || '', company: a.company || '',
    addressLine1: a.addressLine1 || '', addressLine2: a.addressLine2 || '', landmark: a.landmark || '',
    city: a.city || '', stateProvince: a.stateProvince || '', postalCode: a.postalCode || '',
    countryCode: a.countryCode || '', phoneNumber: a.phoneNumber || ''
  }
}
function selectSaved (a) {
  selectedAddressId.value = a.id
  fillFromAddress(a)
}
function selectNew () {
  selectedAddressId.value = 'new'
  addr.value = {
    ...emptyAddress(),
    firstName: profile.value?.firstName || '',
    lastName: profile.value?.lastName || '',
    phoneNumber: profile.value?.phoneNumber || ''
  }
}
function formatAddr (a) {
  return [a.addressLine1, a.addressLine2, a.landmark, a.city, a.stateProvince, a.postalCode, a.countryCode]
    .filter(Boolean).join(', ')
}

// Load the signed-in buyer's profile (contact email) + saved shipping addresses, and pre-select the
// default so checkout is one tap away.
async function loadAccountAddresses () {
  try {
    const [prof, book] = await Promise.all([
      accountApi.getProfile().catch(() => null),
      accountApi.addressBook().catch(() => null)
    ])
    profile.value = prof
    if (prof && prof.email) { email.value = prof.email; profileEmail.value = prof.email }
    const shipping = Array.isArray(book && book.shipping) ? book.shipping : []
    savedAddresses.value = shipping
    if (shipping.length) selectSaved(shipping.find((a) => a.isDefault) || shipping[0])
    else selectNew()
  } catch (e) {
    selectedAddressId.value = 'new'
  }
}

// Persist the just-entered delivery address to the signed-in buyer's address book.
async function saveCurrentAddress () {
  const a = addr.value
  await accountApi.createAddress({
    addressType: 'Shipping',
    isDefault: savedAddresses.value.length === 0,
    firstName: (a.firstName || '').trim(),
    lastName: (a.lastName || '').trim(),
    company: null,
    addressLine1: (a.addressLine1 || '').trim(),
    addressLine2: (a.addressLine2 || '').trim() || null,
    landmark: (a.landmark || '').trim() || null,
    city: (a.city || '').trim(),
    stateProvince: (a.stateProvince || '').trim() || null,
    postalCode: (a.postalCode || '').trim(),
    countryCode: (a.countryCode || '').toUpperCase().trim(),
    phoneNumber: (a.phoneNumber || '').trim() || null
  })
}

// ---- Guest contact persistence ----------------------------------------------
// Save the contact + delivery details to localStorage so a guest who must create an account can sign
// in and come straight back to a fully-filled checkout (cleared once the order is placed). Restored on
// mount for everyone, so returning after login/registration re-populates the form automatically.
const CHECKOUT_KEY = 'sf.checkout.contact'
function persistContact () {
  try {
    LocalStorage.set(CHECKOUT_KEY, { email: email.value, addr: addr.value })
  } catch (e) { /* storage disabled — non-fatal */ }
}
function restoreContact () {
  try {
    const saved = LocalStorage.getItem(CHECKOUT_KEY)
    if (saved && typeof saved === 'object') {
      if (typeof saved.email === 'string' && !email.value) email.value = saved.email
      if (saved.addr && typeof saved.addr === 'object') addr.value = { ...emptyAddress(), ...saved.addr }
    }
  } catch (e) { /* ignore malformed */ }
}
function clearContact () {
  try { LocalStorage.remove(CHECKOUT_KEY) } catch (e) { /* ignore */ }
}
watch([email, addr], persistContact, { deep: true })

const emailValid = computed(() => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email.value.trim()))

// Vuelidate-style field object driving AppTextField's inline error (stacked label, shown on blur).
const emailTouched = ref(false)
const emailV = computed(() => {
  const errors = []
  if (emailTouched.value) {
    const val = email.value.trim()
    if (!val) errors.push({ $message: 'Email is required' })
    else if (!emailValid.value) errors.push({ $message: 'Enter a valid email' })
  }
  return { $error: errors.length > 0, $errors: errors, $touch: () => { emailTouched.value = true } }
})

// The phone emits E.164 when valid, otherwise raw digits; isValidPhoneNumber gates on a real number.
const phoneValid = computed(() => {
  const p = (addr.value.phoneNumber || '').trim()
  return !!p && isPossiblePhoneNumber(p)
})

const requiredComplete = computed(() => {
  const a = addr.value
  return (
    !!(a.firstName || '').trim() &&
    !!(a.lastName || '').trim() &&
    emailValid.value &&
    phoneValid.value &&
    !!(a.addressLine1 || '').trim() &&
    !!(a.stateProvince || '').trim() &&
    !!(a.postalCode || '').trim() &&
    !!a.countryCode
  )
})

// ---- Quote ------------------------------------------------------------------
const quote = ref(null)
const quoting = ref(false)
const quoteError = ref('')
const selectedShippingMethodId = ref(null)

const shippingOptions = computed(() => quote.value?.shippingOptions || [])
// A store may have no shipping methods configured for this address; in that case checkout skips the
// shipping-method requirement and lets the order be placed directly.
const shippingRequired = computed(() => shippingOptions.value.length > 0)
const isRoutable = computed(() => !!quote.value?.isRoutable)
const guestOrderingAllowed = computed(() => (quote.value ? quote.value.guestOrderingAllowed !== false : true))
const canCollectDetails = computed(() => !!quote.value && isRoutable.value && guestOrderingAllowed.value)
const selectedShipping = computed(
  () => shippingOptions.value.find((o) => o.methodId === selectedShippingMethodId.value) || null
)
const canQuote = computed(() => requiredComplete.value && items.value.length > 0)

// ---- Guest → account (express registration) ---------------------------------
// When a store disallows guest ordering, turn the already-entered checkout details into an account:
// the backend creates a verified customer, emails a password and saves the delivery address. The buyer
// signs in and returns here (details preserved) to place the order.
const registering = ref(false)
const loginTo = computed(() => ({ name: 'shop-login', query: { redirect: route.fullPath } }))

async function registerAndContinue () {
  if (!canQuote.value || registering.value) return
  registering.value = true
  try {
    const recaptchaToken = await getRecaptchaToken('register')
    const a = addr.value
    const res = await customerAuthApi.registerAtCheckout({
      email: email.value.trim(),
      firstName: (a.firstName || '').trim(),
      lastName: (a.lastName || '').trim(),
      phoneNumber: (a.phoneNumber || '').trim() || null,
      addressLine1: (a.addressLine1 || '').trim(),
      addressLine2: (a.addressLine2 || '').trim() || null,
      landmark: (a.landmark || '').trim() || null,
      city: (a.city || '').trim(),
      stateProvince: (a.stateProvince || '').trim() || null,
      postalCode: (a.postalCode || '').trim(),
      countryCode: a.countryCode,
      recaptchaToken
    })
    persistContact()
    notify.success(
      `We've emailed a password to ${res.email}. Sign in to finish your order — your details are saved.`
    )
    router.push({ name: 'shop-login', query: { redirect: route.fullPath } })
  } catch (err) {
    notify.error(getApiErrorMessage(err))
  } finally {
    registering.value = false
  }
}

function buildShipTo () {
  const a = addr.value
  return {
    firstName: (a.firstName || '').trim(),
    lastName: (a.lastName || '').trim(),
    email: email.value.trim(),
    line1: (a.addressLine1 || '').trim(),
    line2: (a.addressLine2 || '').trim() || null,
    city: (a.city || '').trim(),
    region: (a.stateProvince || '').trim() || null,
    postalCode: (a.postalCode || '').trim() || null,
    countryCode: a.countryCode,
    landmark: (a.landmark || '').trim() || null,
    phoneNumber: (a.phoneNumber || '').trim() || null
  }
}

async function runQuote () {
  if (!canQuote.value) return
  quoting.value = true
  quoteError.value = ''
  try {
    const q = await checkoutApi.quote({
      cartId: null,
      sessionId: sessionId.value,
      shipTo: buildShipTo(),
      shippingMethodId: selectedShippingMethodId.value || null
    })
    quote.value = q
    // Keep a valid shipping selection: default to the cheapest option (which is what
    // the quote priced) without triggering another quote.
    const opts = q.shippingOptions || []
    if (!opts.some((o) => o.methodId === selectedShippingMethodId.value)) {
      const cheapest = [...opts].sort((a, b) => (a.rate ?? 0) - (b.rate ?? 0))[0]
      selectedShippingMethodId.value = cheapest ? cheapest.methodId : null
    }
  } catch (err) {
    quote.value = null
    quoteError.value = getApiErrorMessage(err)
  } finally {
    quoting.value = false
  }
}

// The buyer explicitly calculates shipping via the button. Any change to a pricing-relevant field
// afterwards invalidates the quote so they must recalculate before Place order re-enables.
const quoteSignature = computed(() => {
  const a = addr.value
  return JSON.stringify([
    (a.addressLine1 || '').trim(),
    (a.addressLine2 || '').trim(),
    (a.city || '').trim(),
    (a.stateProvince || '').trim(),
    (a.postalCode || '').trim(),
    a.countryCode || '',
    email.value.trim()
  ])
})

watch(quoteSignature, () => {
  quote.value = null
  quoteError.value = ''
  selectedShippingMethodId.value = null
})

// Selecting a different shipping method re-quotes immediately for real-time totals.
function onShippingChange (methodId) {
  selectedShippingMethodId.value = methodId
  runQuote()
}

// ---- Payment ----------------------------------------------------------------
const paymentMethods = [
  { value: 'CashOnDelivery', label: 'Cash on delivery' },
  { value: 'Stripe', label: 'Credit / debit card (Stripe)' },
  { value: 'PayPal', label: 'PayPal' },
  { value: 'BankTransfer', label: 'Bank transfer' }
]
const paymentMethod = ref('CashOnDelivery')

// ---- Place ------------------------------------------------------------------
const placing = ref(false)
const placeError = ref('')
const orderResult = ref(null)

const canPlace = computed(
  () =>
    !!quote.value &&
    isRoutable.value &&
    guestOrderingAllowed.value &&
    items.value.length > 0 &&
    (!shippingRequired.value || !!selectedShippingMethodId.value) &&
    !!paymentMethod.value &&
    !placing.value
)

async function placeOrder () {
  placeError.value = ''
  const ok = await formRef.value.validate()
  if (!ok || !canPlace.value) return
  placing.value = true
  try {
    // Signed-in buyer entering a new address who opted to save it → add it to their account (best-effort).
    if (isAuthed.value && selectedAddressId.value === 'new' && saveNewAddress.value) {
      await saveCurrentAddress().catch(() => {})
    }
    // reCAPTCHA token for the guest-checkout form (no-op when reCAPTCHA is disabled/unconfigured).
    const recaptchaToken = await getRecaptchaToken('guestCheckout')
    const result = await checkoutApi.place({
      cartId: null,
      sessionId: sessionId.value,
      shipTo: buildShipTo(),
      selectedShippingMethodId: selectedShippingMethodId.value,
      paymentMethod: paymentMethod.value,
      paymentToken: null,
      couponCode: null,
      recaptchaToken
    })
    if (result && result.success) {
      orderResult.value = result
      // Order placed — the saved checkout details are no longer needed.
      clearContact()
      // The cart is now checked out; refreshing yields a fresh empty cart (badge resets).
      await refresh().catch(() => {})
      window.scrollTo({ top: 0, behavior: 'smooth' })
    } else {
      placeError.value = (result && result.error) || 'Payment could not be processed. Please try again.'
      notify.error(placeError.value)
    }
  } catch (err) {
    placeError.value = getApiErrorMessage(err)
    notify.error(placeError.value)
  } finally {
    placing.value = false
  }
}

onMounted(() => {
  loadCurrencies()
  refresh().catch((err) => notify.error(getApiErrorMessage(err)))
  // Signed-in buyers get their profile + saved addresses (pick-or-add); guests restore any draft.
  if (isAuthed.value) loadAccountAddresses()
  else restoreContact()
})
</script>

<style scoped lang="scss">
.storefront-container {
  max-width: 1200px;
  margin: 0 auto;
}

.sf-address-option {
  border: 1px solid rgba(0, 0, 0, 0.16);
  transition: border-color 0.15s ease, background 0.15s ease;
}
.sf-address-option--active {
  border-color: var(--q-primary);
  background: rgba(0, 0, 0, 0.02);
}
body.body--dark .sf-address-option {
  border-color: rgba(255, 255, 255, 0.22);
}
body.body--dark .sf-address-option--active {
  background: rgba(255, 255, 255, 0.05);
}

.confirm-card {
  max-width: 640px;
}

.summary-card {
  position: sticky;
  top: 88px;
}
</style>
