export function addOnceEventListener (
  el: EventTarget,
  eventName: string,
  cb: (event: Event) => void,
  options: boolean | AddEventListenerOptions = false
): void {
  const once = (event: Event) => {
    cb(event)
    el.removeEventListener(eventName, once, options)
  }

  el.addEventListener(eventName, once, options)
}

let passiveSupported = false
try {
  if (typeof window !== 'undefined') {
    const testListenerOpts = Object.defineProperty({}, 'passive', {
      get: () => {
        passiveSupported = true
      },
    })

    window.addEventListener('testListener' as any, testListenerOpts as any, testListenerOpts)
    window.removeEventListener('testListener' as any, testListenerOpts as any, testListenerOpts)
  }
} catch (e) { console.warn(e) } /* eslint-disable-line no-console */
export { passiveSupported }

export function addPassiveEventListener (
  el: EventTarget,
  event: string,
  cb: ((e: any) => void),
  options: {}
): void {
  el.addEventListener(event, cb, passiveSupported ? options : false)
}

export function getZIndex (el?: Element | null): number {
  if (!el || el.nodeType !== Node.ELEMENT_NODE) return 0

  const index = +window.getComputedStyle(el).getPropertyValue('z-index')

  if (!index) return getZIndex(el.parentNode as Element)
  return index
}

// KeyboardEvent.keyCode aliases
export const keyCodes = Object.freeze({
  enter: 13,
  tab: 9,
  delete: 46,
  esc: 27,
  space: 32,
  up: 38,
  down: 40,
  left: 37,
  right: 39,
  end: 35,
  home: 36,
  del: 46,
  backspace: 8,
  insert: 45,
  pageup: 33,
  pagedown: 34,
  shift: 16,
})

/**  Polyfill for Event.prototype.composedPath */
export function composedPath (e: Event): EventTarget[] {
  if (e.composedPath) return e.composedPath()

  const path = []
  let el = e.target as Element

  while (el) {
    path.push(el)

    if (el.tagName === 'HTML') {
      path.push(document)
      path.push(window)

      return path
    }

    el = el.parentElement!
  }
  return path
}

export function getBlazorId(el) {
  let _bl_ = el.getAttributeNames().find(a => a.startsWith('_bl_'))
  if (_bl_) {
    _bl_ = _bl_.substring(4);
  }

  return _bl_;
}

export function getElementSelector(el) {
  if (!(el instanceof Element))
    return;
  var path = [];
  while (el.nodeType === Node.ELEMENT_NODE) {
    var selector = el.nodeName.toLowerCase();
    if (el.id) {
      selector += '#' + el.id;
      path.unshift(selector);
      break;
    } else {
      var sib = el, nth = 1;
      while (sib = sib.previousElementSibling) {
        if (sib.nodeName.toLowerCase() == selector)
          nth++;
      }
      if (nth != 1)
        selector += ":nth-of-type(" + nth + ")";
    }
    path.unshift(selector);
    el = el.parentNode;
  }
  return path.join(" > ");
}

export function getDom(element) {
  if (!element) {
    element = document.body;
  } else if (typeof element === 'string') {
    if (element === 'document') {
      return document.documentElement;
    } else if (element.indexOf('.') > 0) {
      // for example: el.parentElement
      let array = element.split('.');
      let el = document.querySelector(array[0]);
      if (!el) {
        return null;
      }

      element = el[array[1]];
    } else {
      element = document.querySelector(element);
    }
  }

  return element;
}

export const canUseDom = !!(
  typeof window !== 'undefined' &&
  typeof document !== 'undefined' &&
  window.document &&
  window.document.createElement
)
