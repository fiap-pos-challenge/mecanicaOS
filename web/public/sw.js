const SHELL_CACHE = 'oficina-shell-v1';
const SHELL = ['/', '/index.html', '/manifest.webmanifest', '/icons/icon.svg'];
const STATIC = /\.(?:js|css|woff2?|svg|ico|png)$/;

self.addEventListener('install', (event) => {
  event.waitUntil(
    caches
      .open(SHELL_CACHE)
      .then((cache) => cache.addAll(SHELL))
      .then(() => self.skipWaiting()),
  );
});

self.addEventListener('activate', (event) => {
  event.waitUntil(
    caches
      .keys()
      .then((keys) =>
        Promise.all(keys.filter((key) => key !== SHELL_CACHE).map((key) => caches.delete(key))),
      )
      .then(() => self.clients.claim()),
  );
});

self.addEventListener('fetch', (event) => {
  const request = event.request;
  const url = new URL(request.url);
  if (
    request.method !== 'GET' ||
    url.origin !== self.location.origin ||
    url.pathname.startsWith('/api/')
  ) {
    return;
  }

  if (request.mode === 'navigate') {
    if (url.pathname.startsWith('/o/') || url.pathname.startsWith('/v/')) {
      return;
    }

    event.respondWith(
      fetch(request).catch(() =>
        caches.open(SHELL_CACHE).then((cache) => cache.match('/index.html')),
      ),
    );
    return;
  }

  if (STATIC.test(url.pathname)) {
    event.respondWith(
      caches.open(SHELL_CACHE).then((cache) =>
        cache.match(request).then(
          (cached) =>
            cached ||
            fetch(request).then((response) => {
              if (response.ok) {
                cache.put(request, response.clone());
              }

              return response;
            }),
        ),
      ),
    );
  }
});
