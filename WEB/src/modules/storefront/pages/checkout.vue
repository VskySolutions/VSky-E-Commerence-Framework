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

              <q-input
                v-model="address.email"
                dense
                outlined
                type="email"
                label="Email"
                class="q-mb-sm"
                :rules="[req, emailRule]"
              />
              <div class="row q-col-gutter-sm">
                <div class="col-12 col-sm-6">
                  <q-input v-model="address.firstName" dense outlined label="First name" :rules="[req]" />
                </div>
                <div class="col-12 col-sm-6">
                  <q-input v-model="address.lastName" dense outlined label="Last name" :rules="[req]" />
                </div>
              </div>

              <q-select
                v-model="address.countryCode"
                dense
                outlined
                emit-value
                map-options
                use-input
                options-dense
                input-debounce="0"
                label="Country"
                class="q-mt-sm"
                :options="countryOptions"
                :rules="[req]"
                @filter="filterCountries"
              />

              <div class="row q-col-gutter-sm q-mt-none">
                <div class="col-12 col-sm-6">
                  <q-input v-model="address.region" dense outlined label="State / Region" />
                </div>
                <div class="col-12 col-sm-6">
                  <q-input v-model="address.postalCode" dense outlined label="Postal code" />
                </div>
              </div>

              <q-input v-model="address.city" dense outlined label="City" class="q-mt-sm" :rules="[req]" />
              <q-input v-model="address.line1" dense outlined label="Address line 1" class="q-mt-sm" :rules="[req]" />
              <q-input v-model="address.line2" dense outlined label="Address line 2 (optional)" class="q-mt-sm" />
            </q-card-section>
          </q-card>
        </q-form>

        <!-- Address hint before a quote is available -->
        <q-banner v-if="!quote && !quoting && !quoteError" dense class="bg-blue-1 text-blue-9 rounded-borders q-mb-lg">
          <template #avatar><q-icon name="o_info" color="blue-9" /></template>
          Enter your delivery address to see shipping options and live totals.
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

        <!-- Guest ordering not permitted (AC-CHK-003.2 / AC-STR-001.5) -->
        <q-card
          v-if="quote && isRoutable && !guestOrderingAllowed"
          flat
          bordered
          class="q-mb-lg bg-amber-1"
        >
          <q-card-section class="row items-center no-wrap">
            <q-icon name="o_lock" color="amber-9" size="28px" class="q-mr-md" />
            <div class="col">
              <div class="text-subtitle2 text-weight-bold">Sign in to complete this order</div>
              <div class="text-body2 text-grey-8">
                Guest checkout is not available for this store. Please log in or create an account to continue.
              </div>
            </div>
            <q-btn unelevated color="primary" no-caps label="Sign in" :to="{ name: 'login' }" class="q-ml-md" />
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
              No shipping methods are available for this address.
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
              :to="{ name: 'login' }"
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
import { ref, reactive, computed, watch, onMounted } from 'vue'
import { debounce } from 'lodash-es'
import { Country } from 'country-state-city'
import { checkoutApi } from 'modules/storefront/api'
import { useCart } from 'modules/storefront/composables/useCart'
import { useCurrency } from 'modules/storefront/composables/useCurrency'
import { useRecaptcha } from 'modules/storefront/composables/useRecaptcha'
import { useNotify } from 'composables/useNotify'
import { getApiErrorMessage } from 'services/api'

const { sessionId, cart, items, subtotal, appliedCouponCode, loading, refresh } = useCart()
const { format, load: loadCurrencies } = useCurrency()
const { getToken: getRecaptchaToken } = useRecaptcha()
const notify = useNotify()

// ---- Address form -----------------------------------------------------------
const formRef = ref(null)
const address = reactive({
  email: '',
  firstName: '',
  lastName: '',
  countryCode: '',
  region: '',
  postalCode: '',
  city: '',
  line1: '',
  line2: ''
})

const req = (v) => (!!v && String(v).trim().length > 0) || 'Required'
const emailRule = (v) => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(String(v || '').trim()) || 'Enter a valid email'
const emailValid = computed(() => /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(address.email.trim()))

const requiredComplete = computed(
  () =>
    !!address.firstName.trim() &&
    !!address.lastName.trim() &&
    emailValid.value &&
    !!address.line1.trim() &&
    !!address.city.trim() &&
    !!address.countryCode
)

// Country select (client-filtered over the full ISO list from country-state-city).
const allCountries = Country.getAllCountries().map((c) => ({ label: c.name, value: c.isoCode }))
const countryOptions = ref(allCountries)
function filterCountries (needle, doneFn) {
  doneFn(() => {
    const q = (needle || '').toLowerCase()
    countryOptions.value = q ? allCountries.filter((o) => o.label.toLowerCase().includes(q)) : allCountries
  })
}

// ---- Quote ------------------------------------------------------------------
const quote = ref(null)
const quoting = ref(false)
const quoteError = ref('')
const selectedShippingMethodId = ref(null)

const shippingOptions = computed(() => quote.value?.shippingOptions || [])
const isRoutable = computed(() => !!quote.value?.isRoutable)
const guestOrderingAllowed = computed(() => (quote.value ? quote.value.guestOrderingAllowed !== false : true))
const canCollectDetails = computed(() => !!quote.value && isRoutable.value && guestOrderingAllowed.value)
const selectedShipping = computed(
  () => shippingOptions.value.find((o) => o.methodId === selectedShippingMethodId.value) || null
)
const canQuote = computed(() => requiredComplete.value && items.value.length > 0)

function buildShipTo () {
  return {
    firstName: address.firstName.trim(),
    lastName: address.lastName.trim(),
    email: address.email.trim(),
    line1: address.line1.trim(),
    line2: address.line2.trim() || null,
    city: address.city.trim(),
    region: address.region.trim() || null,
    postalCode: address.postalCode.trim() || null,
    countryCode: address.countryCode
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

const runQuoteDebounced = debounce(runQuote, 450)

// Re-quote whenever the pricing-relevant address fields change (and the form is complete).
const quoteSignature = computed(() => {
  if (!canQuote.value) return ''
  return JSON.stringify([
    address.line1.trim(),
    address.line2.trim(),
    address.city.trim(),
    address.region.trim(),
    address.postalCode.trim(),
    address.countryCode
  ])
})

watch(quoteSignature, (sig) => {
  if (!sig) {
    quote.value = null
    quoteError.value = ''
    return
  }
  runQuoteDebounced()
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
    !!selectedShippingMethodId.value &&
    !!paymentMethod.value &&
    !placing.value
)

async function placeOrder () {
  placeError.value = ''
  const ok = await formRef.value.validate()
  if (!ok || !canPlace.value) return
  placing.value = true
  try {
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
})
</script>

<style scoped lang="scss">
.storefront-container {
  max-width: 1200px;
  margin: 0 auto;
}

.confirm-card {
  max-width: 640px;
}

.summary-card {
  position: sticky;
  top: 88px;
}
</style>
