export interface RetryOptions {
  maxRetries: number
  delay: number | ((attempt: number) => number)
  retryCondition?: (error: any) => boolean
  onRetry?: (attempt: number, error: any) => void
}

const defaultOptions: RetryOptions = {
  maxRetries: 3,
  delay: (attempt) => attempt * 1000,
}

export async function withRetry<T>(
  fn: () => Promise<T>,
  options: Partial<RetryOptions> = {},
): Promise<T> {
  const opts = { ...defaultOptions, ...options }
  let lastError: any

  for (let attempt = 0; attempt <= opts.maxRetries; attempt++) {
    try {
      return await fn()
    } catch (error) {
      lastError = error
      if (attempt === opts.maxRetries) break
      if (opts.retryCondition && !opts.retryCondition(error)) break

      opts.onRetry?.(attempt + 1, error)

      const delayMs =
        typeof opts.delay === 'function' ? opts.delay(attempt + 1) : opts.delay
      await new Promise((r) => setTimeout(r, delayMs))
    }
  }

  throw lastError
}
