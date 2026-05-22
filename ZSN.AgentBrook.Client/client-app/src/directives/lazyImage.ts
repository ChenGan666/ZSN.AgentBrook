import type { Directive } from 'vue'

export const lazyImage: Directive<HTMLImageElement> = {
  mounted(el) {
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            el.src = el.dataset.src || ''
            observer.unobserve(el)
          }
        })
      },
      { rootMargin: '200px' },
    )
    observer.observe(el)
  },
}
