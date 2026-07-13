/*
 * Catalogue for the Integrations hub. Each entry drives the left-hand nav list (icon + label, grouped by
 * category) and the right-hand panel. `kind` selects which panel renders:
 *   - 'credential' (default): typed secret CRUD for a Credentials_* table (fields drive the form).
 *   - 'storage': the file-storage provider settings panel.
 *   - 'smtp': the SMTP sending-accounts panel.
 * `key` is the API route slug for credential items (`/api/integration-credentials/{key}`).
 */

export const INTEGRATIONS = [
  // ---- Payments ----
  {
    key: 'stripe', kind: 'credential', label: 'Stripe', category: 'Payments', icon: 'o_credit_card',
    description: 'Cards, wallets and more via Stripe.',
    fields: [
      { key: 'publishableKey', label: 'Publishable Key', placeholder: 'pk_live_…' },
      { key: 'secretKey', label: 'Secret Key', secret: true, required: true, placeholder: 'sk_live_…' }
    ]
  },
  {
    key: 'paypal', kind: 'credential', label: 'PayPal', category: 'Payments', icon: 'o_account_balance_wallet',
    description: 'PayPal Checkout (REST app credentials).',
    fields: [
      { key: 'clientId', label: 'Client ID', required: true },
      { key: 'secretKey', label: 'Secret', secret: true, required: true }
    ]
  },
  {
    key: 'razorpay', kind: 'credential', label: 'Razorpay', category: 'Payments', icon: 'o_payments',
    description: 'Razorpay payments.',
    fields: [
      { key: 'keyId', label: 'Key ID', required: true, placeholder: 'rzp_live_…' },
      { key: 'keySecret', label: 'Key Secret', secret: true, required: true }
    ]
  },
  {
    key: 'square', kind: 'credential', label: 'Square', category: 'Payments', icon: 'o_qr_code_2',
    description: 'Square payments.',
    fields: [
      { key: 'applicationId', label: 'Application ID' },
      { key: 'accessToken', label: 'Access Token', secret: true, required: true },
      { key: 'applicationSecret', label: 'Application Secret', secret: true }
    ]
  },
  {
    key: 'authorizenet', kind: 'credential', label: 'Authorize.Net', category: 'Payments', icon: 'o_credit_score',
    description: 'Authorize.Net payment gateway.',
    fields: [
      { key: 'applicationLoginId', label: 'API Login ID', required: true },
      { key: 'transactionKey', label: 'Transaction Key', secret: true, required: true },
      { key: 'signatureKey', label: 'Signature Key', secret: true }
    ]
  },

  // ---- Tax ----
  {
    key: 'taxjar', kind: 'credential', label: 'TaxJar', category: 'Tax', icon: 'o_receipt_long',
    description: 'TaxJar sales-tax API.',
    fields: [
      { key: 'baseUrl', label: 'Base URL', placeholder: 'https://api.taxjar.com' },
      { key: 'secretKey', label: 'API Token', secret: true, required: true }
    ]
  },
  {
    key: 'stripe-tax', kind: 'credential', label: 'Stripe Tax', category: 'Tax', icon: 'o_receipt',
    description: 'Stripe Tax calculation.',
    fields: [
      { key: 'publishableKey', label: 'Publishable Key', placeholder: 'pk_live_…' },
      { key: 'secretKey', label: 'Secret Key', secret: true, required: true, placeholder: 'sk_live_…' }
    ]
  },

  // ---- Shipping ----
  {
    key: 'fedex', kind: 'credential', label: 'FedEx', category: 'Shipping', icon: 'o_local_shipping',
    description: 'FedEx rates and labels.',
    fields: [
      { key: 'apiKey', label: 'API Key', required: true },
      { key: 'apiSecret', label: 'API Secret', secret: true, required: true }
    ]
  },
  {
    key: 'dhl', kind: 'credential', label: 'DHL Express', category: 'Shipping', icon: 'o_local_shipping',
    description: 'DHL Express rates and labels.',
    fields: [
      { key: 'apiKey', label: 'API Key', required: true },
      { key: 'apiSecret', label: 'API Secret', secret: true, required: true }
    ]
  },
  {
    key: 'usps', kind: 'credential', label: 'USPS', category: 'Shipping', icon: 'o_markunread_mailbox',
    description: 'USPS shipping.',
    fields: [
      { key: 'consumerKey', label: 'Consumer Key', required: true },
      { key: 'consumerSecret', label: 'Consumer Secret', secret: true, required: true }
    ]
  },

  // ---- Communication ----
  {
    key: 'smtp', kind: 'smtp', label: 'Email (SMTP) Accounts', category: 'Communication', icon: 'o_mail',
    description: 'SMTP sending accounts, categories and test-send.'
  },
  {
    key: 'twilio', kind: 'credential', label: 'Twilio', category: 'Communication', icon: 'o_sms',
    description: 'Twilio SMS / WhatsApp.',
    fields: [
      { key: 'accountSid', label: 'Account SID', required: true, placeholder: 'AC…' },
      { key: 'authToken', label: 'Auth Token', secret: true, required: true },
      { key: 'whatsAppNumber', label: 'WhatsApp Number', placeholder: '+1…' }
    ]
  },

  // ---- Storage ----
  {
    key: 'storage-settings', kind: 'storage', label: 'Storage Settings', category: 'Storage', icon: 'o_settings',
    description: 'Active file-storage provider, container and CDN.'
  },
  {
    key: 'azure-blob', kind: 'credential', label: 'Azure Blob Storage', category: 'Storage', icon: 'o_cloud',
    description: 'Azure Blob object storage connection string.',
    fields: [
      { key: 'connectionString', label: 'Connection String', secret: true, required: true }
    ]
  },

  // ---- Security ----
  {
    key: 'recaptcha', kind: 'recaptcha', label: 'reCAPTCHA', category: 'Security', icon: 'o_verified_user',
    description: 'Google reCAPTCHA bot protection for storefront forms.'
  }
]

/** Category display order for the grouped left-hand list. */
const CATEGORY_ORDER = ['Payments', 'Tax', 'Shipping', 'Communication', 'Storage', 'Security']

const CATEGORY_ICON = {
  Payments: 'o_payments',
  Tax: 'o_receipt_long',
  Shipping: 'o_local_shipping',
  Communication: 'o_forum',
  Storage: 'o_cloud',
  Security: 'o_shield'
}

/** Integrations grouped into ordered categories for rendering the left nav. */
export const INTEGRATION_GROUPS = CATEGORY_ORDER
  .map((category) => ({
    category,
    icon: CATEGORY_ICON[category],
    items: INTEGRATIONS.filter((i) => i.category === category)
  }))
  .filter((g) => g.items.length > 0)

export function findIntegration (key) {
  return INTEGRATIONS.find((i) => i.key === key) || null
}
