import { ref } from 'vue'

const isOnline = ref(typeof navigator !== 'undefined' ? navigator.onLine : true)
let monitorStarted = false
let monitorInterval = null
let checkInFlight = null

async function probeInternet() {
  if (typeof navigator !== 'undefined' && !navigator.onLine) {
    return false
  }

  const controller = new AbortController()
  const timeoutId = window.setTimeout(() => controller.abort(), 4000)

  try {
    await fetch(`https://www.gstatic.com/generate_204?ts=${Date.now()}`, {
      method: 'GET',
      mode: 'no-cors',
      cache: 'no-store',
      signal: controller.signal,
    })

    return true
  } catch {
    return false
  } finally {
    window.clearTimeout(timeoutId)
  }
}

export async function checkConnectivity() {
  if (checkInFlight) {
    return checkInFlight
  }

  checkInFlight = probeInternet()
    .then((connected) => {
      isOnline.value = connected
      return connected
    })
    .finally(() => {
      checkInFlight = null
    })

  return checkInFlight
}

function handleBrowserOnline() {
  checkConnectivity()
}

function handleBrowserOffline() {
  isOnline.value = false
}

function handleVisibilityOrFocus() {
  if (document.visibilityState === 'visible') {
    checkConnectivity()
  }
}

export function startConnectivityMonitor() {
  if (monitorStarted || typeof window === 'undefined') {
    return
  }

  monitorStarted = true
  window.addEventListener('online', handleBrowserOnline)
  window.addEventListener('offline', handleBrowserOffline)
  window.addEventListener('focus', handleVisibilityOrFocus)
  document.addEventListener('visibilitychange', handleVisibilityOrFocus)
  monitorInterval = window.setInterval(() => {
    if (document.visibilityState === 'visible') {
      checkConnectivity()
    }
  }, 15000)

  checkConnectivity()
}

export function useConnectivityStatus() {
  startConnectivityMonitor()
  return { isOnline, checkConnectivity }
}
