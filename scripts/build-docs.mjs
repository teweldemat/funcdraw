import { promises as fs } from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
import { marked } from 'marked';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const rootDir = path.resolve(__dirname, '..');
const docsSourceDir = path.join(rootDir, 'docs');
const docsSiteDir = path.join(docsSourceDir, 'site');
const docsAssetsDir = path.join(docsSourceDir, 'assets');

const template = ({ title, nav, content }) => `<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1" />
  <title>${title}</title>
  <link rel="preconnect" href="https://fonts.googleapis.com">
  <link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
  <link href="https://fonts.googleapis.com/css2?family=Inter:wght@400;600&display=swap" rel="stylesheet">
  <style>
    :root {
      color-scheme: light dark;
      --bg: #f8fafc;
      --panel: #ffffff;
      --border: #e2e8f0;
      --text: #0f172a;
      --accent: #0ea5e9;
    }
    * { box-sizing: border-box; }
    body {
      font-family: 'Inter', system-ui, -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
      margin: 0;
      background: var(--bg);
      color: var(--text);
      line-height: 1.6;
    }
    header {
      background: #0f172a;
      color: #f8fafc;
      padding: 1.5rem;
    }
    header h1 { margin: 0 0 0.25rem 0; font-size: 1.75rem; }
    nav a {
      color: #f8fafc;
      margin-right: 1rem;
      text-decoration: none;
      font-weight: 600;
    }
    nav a:hover { text-decoration: underline; }
    main {
      max-width: 860px;
      margin: 0 auto;
      padding: 2rem 1.25rem 3rem;
    }
    article {
      background: var(--panel);
      border: 1px solid var(--border);
      border-radius: 18px;
      padding: 2rem;
      box-shadow: 0 10px 30px rgba(15, 23, 42, 0.08);
    }
    article h1:first-child { margin-top: 0; }
    pre {
      background: #0f172a;
      color: #f8fafc;
      padding: 1rem;
      border-radius: 12px;
      overflow-x: auto;
    }
    code { font-family: 'SFMono-Regular', Consolas, 'Liberation Mono', monospace; }
    a { color: var(--accent); }
    @media (max-width: 640px) {
      article { padding: 1.25rem; }
      header { padding: 1rem; }
      nav { display: flex; flex-wrap: wrap; gap: 0.5rem; }
    }
  </style>
</head>
<body>
  <header>
    <h1>FuncDraw Documentation</h1>
    <nav>${nav}</nav>
  </header>
  <main>
    <article>
      ${content}
    </article>
  </main>
</body>
</html>`;

const slugify = (filename) => {
  const name = filename.replace(/\.md$/i, '');
  return name.toLowerCase() === 'readme' || name.toLowerCase() === 'index' ? 'index' : name.replace(/\s+/g, '-').toLowerCase();
};

const titleFromMarkdown = (markdown, fallback) => {
  const match = markdown.match(/^#\s+(.+)/m);
  if (match) {
    return match[1].trim();
  }
  return fallback;
};

async function getMarkdownFiles(dir) {
  const entries = await fs.readdir(dir, { withFileTypes: true });
  const files = [];
  for (const entry of entries) {
    if (entry.isDirectory()) {
      continue;
    }
    if (entry.name.toLowerCase().endsWith('.md')) {
      files.push(path.join(dir, entry.name));
    }
  }
  return files;
}

async function buildDocs() {
  const markdownFiles = await getMarkdownFiles(docsSourceDir);
  if (markdownFiles.length === 0) {
    console.warn('No markdown files found under', docsSourceDir);
    return;
  }

  await fs.rm(docsSiteDir, { recursive: true, force: true });
  await fs.mkdir(docsSiteDir, { recursive: true });

  const docs = [];
  for (const filePath of markdownFiles) {
    const raw = await fs.readFile(filePath, 'utf8');
    const fileName = path.basename(filePath);
    const slug = slugify(fileName);
    const title = titleFromMarkdown(raw, slug === 'index' ? 'Overview' : slug.replace(/-/g, ' ').replace(/\b\w/g, (c) => c.toUpperCase()));
    const html = marked.parse(raw, { mangle: false, headerIds: true });
    docs.push({ slug, title, html });
  }

  const navLinks = docs
    .map(({ slug, title }) => {
      const href = slug === 'index' ? '/docs/index.html' : `/docs/${slug}.html`;
      return `<a href="${href}">${title}</a>`;
    })
    .join('');

  for (const doc of docs) {
    const filename = doc.slug === 'index' ? 'index.html' : `${doc.slug}.html`;
    const targetPath = path.join(docsSiteDir, filename);
    const page = template({ title: `${doc.title} · FuncDraw Docs`, nav: navLinks, content: doc.html });
    await fs.writeFile(targetPath, page, 'utf8');
  }

  try {
    const siteAssetsTarget = path.join(docsSiteDir, 'assets');
    await fs.rm(siteAssetsTarget, { recursive: true, force: true });
    await fs.cp(docsAssetsDir, siteAssetsTarget, { recursive: true });
    console.log(`Copied docs assets to ${siteAssetsTarget}`);
  } catch (err) {
    if (err.code !== 'ENOENT') {
      throw err;
    }
  }

  console.log(`Built ${docs.length} documentation page(s) into ${docsSiteDir}`);
}

buildDocs().catch((err) => {
  console.error('Failed to build docs:', err);
  process.exitCode = 1;
});
