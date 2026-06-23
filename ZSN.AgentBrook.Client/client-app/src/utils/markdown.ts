import { marked } from 'marked'
import hljs from 'highlight.js'
import DOMPurify from 'dompurify'

marked.setOptions({
  breaks: true,
  gfm: true,
})

const renderer = new marked.Renderer()

renderer.code = function ({ text, lang }: { text: string; lang?: string }) {
  const codeText = String(text ?? '')
  const language = lang && hljs.getLanguage(lang) ? lang : 'plaintext'

  let highlighted = ''
  try {
    highlighted =
      language === 'plaintext'
        ? hljs.highlightAuto(codeText).value
        : hljs.highlight(codeText, { language }).value
  } catch {
    highlighted = DOMPurify.sanitize(codeText)
  }

  return `
    <div class="code-block">
      <div class="code-header">
        <span class="code-language">${language}</span>
        <button class="code-copy" onclick="copyCode(this)" title="复制代码">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
            <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
            <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
          </svg>
        </button>
      </div>
      <pre><code class="hljs language-${language}">${highlighted}</code></pre>
    </div>
  `
}

const _origLink = renderer.link.bind(renderer)
renderer.link = function (token: any) {
  const href = token?.href || ''
  const title = token?.title || ''
  const text = token?.text || ''
  const titleAttr = title ? ` title="${title}"` : ''
  return `<a href="${href}"${titleAttr} target="_blank" rel="noopener noreferrer">${text}</a>`
}

const _origImage = renderer.image.bind(renderer)
renderer.image = function (token: any) {
  const href = token?.href || ''
  const title = token?.title || ''
  const text = token?.text || ''
  const titleAttr = title ? ` title="${title}"` : ''
  const altAttr = text ? ` alt="${text}"` : ' alt="图片"'
  return `<img class="img_show" src="${href}"${altAttr}${titleAttr} style="max-width:100%;height:auto;display:block;margin:8px 0;cursor:pointer;" onclick="window.open('${href}','_blank')" />`
}

marked.use({ renderer })

export function renderMarkdown(markdown: any): string {
  if (markdown == null) return ''

  try {
    let normalized = ''

    if (typeof markdown === 'string') {
      normalized = markdown
    } else if (Array.isArray(markdown)) {
      normalized = markdown
        .map((item: any) => {
          if (item == null) return ''
          if (typeof item === 'string') return item
          if (typeof item === 'object') {
            const t = item.text ?? item.content ?? item.value ?? ''
            if (typeof t === 'string') return t
            try { return '```json\n' + JSON.stringify(t ?? item, null, 2) + '\n```' } catch { return String(t) }
          }
          return String(item)
        })
        .filter(Boolean)
        .join('\n\n')
    } else if (typeof markdown === 'object') {
      const pick = (markdown as any).text ?? (markdown as any).content ?? (markdown as any).value
      if (typeof pick === 'string') normalized = pick
      else {
        try { normalized = '```json\n' + JSON.stringify(markdown, null, 2) + '\n```' } catch { normalized = String(markdown) }
      }
    } else {
      normalized = String(markdown)
    }

    if (!normalized) return ''

    let processed = normalized
    processed = processed.replace(/^(#{1,6})([^#\s])/gm, '$1 $2')
    processed = processed.replace(/^([-*])([^\s])/gm, '$1 $2')
    processed = processed.replace(/^(\d+\.)([^\s])/gm, '$1 $2')
    processed = processed.replace(
      /(?<!\]\()(?<!\()(https?:\/\/[^\s<>)\]]+)/g,
      (m: string) => `[${m}](${m})`,
    )

    let html = marked.parse(processed) as string
    html = html
      .replace(/<table>/g, '<div class="table-wrapper"><table>')
      .replace(/<\/table>/g, '</table></div>')

    return DOMPurify.sanitize(html, {
      ALLOWED_TAGS: [
        'p', 'br', 'strong', 'em', 'u', 's', 'code', 'pre',
        'h1', 'h2', 'h3', 'h4', 'h5', 'h6',
        'ul', 'ol', 'li', 'blockquote', 'a', 'img',
        'table', 'thead', 'tbody', 'tr', 'th', 'td',
        'div', 'span', 'hr',
        'button', 'svg', 'path', 'rect', 'line', 'polyline', 'polygon',
      ],
      ALLOWED_ATTR: [
        'href', 'title', 'target', 'rel', 'src', 'alt', 'width', 'height',
        'class', 'id', 'style', 'onclick',
        'viewBox', 'fill', 'stroke', 'stroke-width',
        'x', 'y', 'x1', 'y1', 'x2', 'y2', 'rx', 'ry', 'd', 'points',
      ],
    })
  } catch {
    try { return typeof markdown === 'string' ? markdown : JSON.stringify(markdown, null, 2) } catch { return String(markdown) }
  }
}

if (typeof window !== 'undefined') {
  ;(window as any).copyCode = function (button: HTMLButtonElement) {
    const codeBlock = button.closest('.code-block')!
    const code = codeBlock.querySelector('code')!.textContent!
    navigator.clipboard.writeText(code).then(() => {
      const originalHTML = button.innerHTML
      button.innerHTML = `<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"></polyline></svg>`
      button.style.color = '#10b981'
      setTimeout(() => { button.innerHTML = originalHTML; button.style.color = '' }, 2000)
    })
  }
}
