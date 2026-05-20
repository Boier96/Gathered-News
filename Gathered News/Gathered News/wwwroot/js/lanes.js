(function () {
    // define some variables (allegedly)
    'use strict';

    var SCROLL_SPEED = 104;

    var OUTLET_INFO = {
        bbc: {
            name: 'BBC News',
            description: 'brihis.',
            founded: '1922',
            hq: 'London, UK',
            reach: 'Global'
        },
        aljazeera: {
            name: 'Al Jazeera',
            description: 'middle-eastern sauce.',
            founded: '1996',
            hq: 'Doha, Qatar',
            reach: 'Global'
        },
        dw: {
            name: 'Deutsche Welle',
            description: "deutchlnader.",
            founded: '1953',
            hq: 'Bonn, Germany',
            reach: 'Global'
        },
        cbc: {
            name: 'CBC News',
            description: "MAple Zyrup.",
            founded: '1936',
            hq: 'Ottawa, Canada',
            reach: 'Canada / International'
        },
        guardian: {
            name: 'The Guardian',
            description: 'not ass brihihs.',
            founded: '1821',
            hq: 'London, UK',
            reach: 'Global'
        }
    };

    var RECENT_STORAGE_KEY = 'gn_recent';
    var RECENT_MAX = 8;

    function getRecent() {
        try {
            return JSON.parse(localStorage.getItem(RECENT_STORAGE_KEY) || '[]');
        } catch (e) {
            return [];
        }
    }

    function saveRecent(list) {
        try {
            localStorage.setItem(RECENT_STORAGE_KEY, JSON.stringify(list)); // never has so much been carrried by so few localstorages
        } catch (e) { }
    }

    function pushRecent(article) {
        var list = getRecent();
        list = list.filter(function (r) { return r.id !== article.id; });
        list.unshift({
            id: article.id,
            title: article.title,
            source: article.source,
            url: article.url
        });
        if (list.length > RECENT_MAX) list.length = RECENT_MAX;
        saveRecent(list);
        renderRecent();
    }

    function renderRecent() {
        var list = document.getElementById('recent-list');
        if (!list) return;

        var recent = getRecent();

        if (recent.length === 0) {
            list.innerHTML = '<span class="settings-placeholder">No articles visited yet.</span>'; // magically set allat HYPErTEXtMARKUpLANGUAGe
            return;
        }

        var html = recent.map(function (r) {
            return (
                '<div class="recent-item" data-article-id="' + r.id + '">' +
                '<div class="recent-source">' + escapeHtml(r.source) + '</div>' +
                '<div class="recent-title">' + escapeHtml(r.title) + '</div>' +
                '</div>'
            );
        }).join('');

        list.innerHTML = html;

        list.querySelectorAll('.recent-item').forEach(function (el) {
            el.addEventListener('click', function () { // listen to me, I am the click now
                var id = parseInt(this.dataset.articleId, 10);
                var match = getRecent().find(function (r) { return r.id === id; });
                if (match) {
                    openOverlay(id, match.url);
                }
            });
        });
    }

    function escapeHtml(str) { // stop putting arrows in your text gurt
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function initLane(wrapperId, innerId, direction) {
        var wrapper = document.getElementById(wrapperId);
        var inner = document.getElementById(innerId);
        if (!wrapper || !inner) return;

        var originals = Array.from(inner.querySelectorAll('.article-card'));
        if (originals.length === 0) return;

        originals.forEach(function (card) {
            inner.appendChild(card.cloneNode(true));
        });

        attachCardClicks(inner);

        requestAnimationFrame(function () { // there seems to be some issue with the time it takes to init the other things
            requestAnimationFrame(function () { // thus we call it twice, giving it more time, which is dirty and not good, but hasn't failed yet
                var halfH = inner.scrollHeight / 2;
                var duration = halfH / SCROLL_SPEED;
                inner.style.animationDuration = duration + 's';
                inner.classList.add(direction === 'up' ? 'scroll-up' : 'scroll-down');
            });
        });

        wrapper.addEventListener('mouseenter', function () {
            inner.classList.add('paused');
        });

        wrapper.addEventListener('mouseleave', function () {
            inner.classList.remove('paused');
        });
    }

    function attachCardClicks(container) {
        container.querySelectorAll('.article-card').forEach(function (card) {
            card.addEventListener('click', function () {
                var id = parseInt(this.dataset.articleId, 10);
                openOverlay(id, null);
            });
        });
    }

    var overlay = null;
    var overlayPanel = null;
    var overlayClose = null;
    var currentFetch = null;

    function openOverlay(id, fallbackUrl) {
        overlay.classList.add('active');

        // gimme allat
        var titleEl = overlay.querySelector('.overlay-title');
        var bodyEl = overlay.querySelector('.overlay-body');
        var imgEl = overlay.querySelector('.overlay-image');
        var linkEl = overlay.querySelector('.overlay-link');
        var outletNameEl = overlay.querySelector('.outlet-name');
        var outletDescEl = overlay.querySelector('.outlet-desc');
        var outletStatsEl = overlay.querySelector('.outlet-stats');

        // there is a tendency to tweak
        titleEl.textContent = '';
        bodyEl.innerHTML = '<span class="overlay-loading">loading...</span>';
        imgEl.style.display = 'none';
        linkEl.href = fallbackUrl || '#';
        outletNameEl.textContent = '';
        outletDescEl.textContent = '';
        outletStatsEl.innerHTML = '';

        if (currentFetch) {
            currentFetch.abort();
        }

        var controller = new AbortController();
        currentFetch = controller;

        fetch('/Home/GetArticle/' + id, { signal: controller.signal }) // fetch my cup peasant
            .then(function (res) { 
                if (!res.ok) throw new Error('not found');
                return res.json();
            })
            .then(function (article) { // ever heard about a promise?
                currentFetch = null;

                var sourceKey = (article.source || '').toLowerCase();
                var info = OUTLET_INFO[sourceKey] || {
                    name: article.source,
                    description: 'type shit.',
                    founded: 'if ur seeing',
                    hq: 'all this',
                    reach: 'ur cooked'
                };

                outletNameEl.textContent = info.name; // set the stats n shi
                outletDescEl.textContent = info.description;
                outletStatsEl.innerHTML = [
                    ['Founded', info.founded],
                    ['HQ', info.hq],
                    ['Reach', info.reach]
                ].map(function (pair) {
                    return '<span class="outlet-stat">' + pair[0] + ': ' + escapeHtml(pair[1]) + '</span>';
                }).join('');

                if (article.imageUrl) {
                    imgEl.src = article.imageUrl;
                    imgEl.style.display = 'block';
                } else {
                    imgEl.style.display = 'none';
                }

                titleEl.textContent = article.title || '';
                linkEl.href = article.url || '#';

                var content = article.content || '';
                var paragraphs = content
                    .split('\n\n')
                    .map(function (p) { return p.trim(); })
                    .filter(function (p) { return p.length > 0; });

                if (paragraphs.length > 0) {
                    bodyEl.innerHTML = paragraphs.map(function (p) {
                        return '<p>' + escapeHtml(p) + '</p>';
                    }).join('');
                } else {
                    bodyEl.innerHTML = '<p class="overlay-loading">ur article sucks gurt</p>';
                }

                overlayPanel.scrollTop = 0;

                pushRecent({
                    id: article.id,
                    title: article.title,
                    source: article.source,
                    url: article.url
                });
            })
            .catch(function (err) {
                if (err.name === 'AbortError') return;
                bodyEl.innerHTML = '<p class="overlay-loading">we couldnt load allat cuz</p>';
            });
    }

    function closeOverlay() {
        overlay.classList.remove('active');
        if (currentFetch) {
            currentFetch.abort();
            currentFetch = null;
        }
    }

    function initProfile() {
        var usernameEl = document.getElementById('profile-username');
        var avatarEl = document.getElementById('profile-avatar');
        var stored = "Reader"; // hardcoded for now
        if (usernameEl) usernameEl.textContent = stored;
        if (avatarEl) avatarEl.textContent = stored.charAt(0).toUpperCase();
    }

    function initHeaderDate() {
        var el = document.getElementById('header-date');
        if (!el) return;
        var now = new Date();
        var days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
        var months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        el.textContent = days[now.getDay()] + ', ' + months[now.getMonth()] + ' ' + now.getDate() + ' ' + now.getFullYear();
    }

    document.addEventListener('DOMContentLoaded', function () { // master
        overlay = document.getElementById('article-overlay');
        overlayPanel = overlay ? overlay.querySelector('.overlay-panel') : null;
        overlayClose = document.getElementById('overlay-close');

        if (!overlay || !overlayClose) return;

        overlayClose.addEventListener('click', closeOverlay);

        overlay.addEventListener('click', function (e) {
            if (e.target === overlay) closeOverlay();
        });

        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') closeOverlay();
        });

        initLane('lane-left', 'lane-left-inner', 'up');
        initLane('lane-right', 'lane-right-inner', 'down');

        initProfile();
        initHeaderDate();
        renderRecent();
    });
}());