(function () {
    'use strict';

    var SCROLL_SPEED = 104;

    // static information about each outlet we use right now, used when showing the full-view of an article (both in overlay and in the seperate page)
    var OUTLET_INFO = {
        bbc: {
            name: 'BBC News',
            description: 'British public service broadcaster, founded by Royal Charter. Known for impartial international reporting.',
            founded: '1922',
            hq: 'London, UK',
            reach: 'Global'
        },
        aljazeera: {
            name: 'Al Jazeera',
            description: 'Qatari state-funded international news network. Provides extensive coverage of the Middle East and Global South.',
            founded: '1996',
            hq: 'Doha, Qatar',
            reach: 'Global'
        },
        dw: {
            name: 'Deutsche Welle',
            description: "Germany\'s international public broadcaster. Offers news in over 30 languages with a focus on European affairs.",
            founded: '1953',
            hq: 'Bonn, Germany',
            reach: 'Global'
        },
        cbc: {
            name: 'CBC News',
            description: "Canada\'s national public broadcaster. Covers Canadian politics, society, and international news.",
            founded: '1936',
            hq: 'Ottawa, Canada',
            reach: 'Canada / International'
        },
        guardian: {
            name: 'The Guardian',
            description: 'Independent British newspaper with a progressive editorial stance. Renowned for investigative journalism.',
            founded: '1821',
            hq: 'London, UK',
            reach: 'Global'
        }
    };

    var RECENT_STORAGE_KEY = 'gn_recent';
    var RECENT_MAX = 8;

    // returns the recently visited articles
    function getRecent() {
        try {
            return JSON.parse(localStorage.getItem(RECENT_STORAGE_KEY) || '[]');
        } catch (e) {
            return [];
        }
    }

    // saves an article to localstorage
    function saveRecent(list) {
        try {
            localStorage.setItem(RECENT_STORAGE_KEY, JSON.stringify(list));
        } catch (e) { }
    }

    // call this function to add to recently viewed
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

    // creates the recently viewed section
    function renderRecent() {
        var list = document.getElementById('recent-list');
        if (!list) return;

        var recent = getRecent();

        if (recent.length === 0) {
            list.innerHTML = '<span class="settings-placeholder">No articles visited yet.</span>';
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
            el.addEventListener('click', function () {
                var id = parseInt(this.dataset.articleId, 10);
                var match = getRecent().find(function (r) { return r.id === id; });
                if (match) {
                    openOverlay(id, match.url);
                }
            });
        });
    }

    // added to prvevent XSS
    function escapeHtml(str) {
        return String(str)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    // works like this: clones all article cards insdide the inner container, applies the CSS animation whose duration is calculated from content height and scroll speed.
    // result is a seamless loop
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
            requestAnimationFrame(function () { // thus we call it twice, giving it more time, which feels unoptimal but apparently normal according to stackoverflow
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

    // DOM elements
    var overlay = null;
    var overlayPanel = null;
    var overlayClose = null;
    var archiveToggle = null;
    var archiveStatus = null;
    var requestToken = null;
    var currentFetch = null;

    // opens article overlay, fetches the full article data from server
    function openOverlay(id, fallbackUrl) {
        overlay.classList.add('active');

        var titleEl = overlay.querySelector('.overlay-title');
        var bodyEl = overlay.querySelector('.overlay-body');
        var imgEl = overlay.querySelector('.overlay-image');
        var linkEl = overlay.querySelector('.overlay-link');
        var outletNameEl = overlay.querySelector('.outlet-name');
        var outletDescEl = overlay.querySelector('.outlet-desc');
        var outletStatsEl = overlay.querySelector('.outlet-stats');

        titleEl.textContent = '';
        bodyEl.innerHTML = '<span class="overlay-loading">loading...</span>';
        imgEl.style.display = 'none';
        linkEl.href = fallbackUrl || '#';
        outletNameEl.textContent = '';
        outletDescEl.textContent = '';
        outletStatsEl.innerHTML = '';
        if (archiveToggle) archiveToggle.textContent = 'Archive article';
        if (archiveStatus) archiveStatus.textContent = '';

        if (currentFetch) {
            currentFetch.abort();
        }

        var controller = new AbortController();
        currentFetch = controller;

        fetch('/Home/GetArticle/' + id, { signal: controller.signal }) // using promises to handle potential slow loading of article data
            .then(function (res) {
                if (!res.ok) throw new Error('not found');
                return res.json();
            })
            .then(function (article) {
                currentFetch = null;

                var sourceKey = (article.source || '').toLowerCase();
                var info = OUTLET_INFO[sourceKey] || {
                    name: article.source,
                    description: 'No description available.',
                    founded: 'Unknown',
                    hq: 'Unknown',
                    reach: 'Unknown'
                };

                outletNameEl.textContent = info.name;
                outletDescEl.textContent = info.description;
                outletStatsEl.innerHTML = [
                    ['Founded', info.founded],
                    ['HQ', info.hq],
                    ['Reach', info.reach]
                ].map(function (pair) {
                    return '<span class="outlet-stat">' + pair[0] + ': ' + escapeHtml(pair[1]) + '</span>';
                }).join('');

                if (archiveToggle) {
                    archiveToggle.dataset.articleId = article.id;
                    archiveToggle.dataset.archived = article.isArchived ? 'true' : 'false';
                    archiveToggle.textContent = article.isArchived ? 'Remove from archive' : 'Archive article';
                }

                if (archiveStatus) {
                    archiveStatus.textContent = article.isArchived ? 'Saved to your archive' : '';
                }

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
                    bodyEl.innerHTML = '<p class="overlay-loading">No article content available.</p>';
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
                bodyEl.innerHTML = '<p class="overlay-loading">Could not load the article.</p>';
            });
    }

    function closeOverlay() {
        overlay.classList.remove('active');
        if (currentFetch) {
            currentFetch.abort();
            currentFetch = null;
        }
    }

    // sets avatar element to first letter of username, stole the idea from Zika
    function initProfile() {
        var usernameEl = document.getElementById('profile-username');
        var avatarEl = document.getElementById('profile-avatar');
        if (!usernameEl && !avatarEl) return;

        var stored = usernameEl && usernameEl.textContent ? usernameEl.textContent.trim() : 'Reader';
        if (usernameEl) usernameEl.textContent = stored;
        if (avatarEl) avatarEl.textContent = stored.charAt(0).toUpperCase();
    }

    // uses the ASP.NET Core antiforgery token, result is cached after first call to avoid repeated DOM lookups
    function getToken() {
        if (requestToken) return requestToken;
        var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        requestToken = tokenInput ? tokenInput.value : null;
        return requestToken;
    }

    // sends POSt request to /Archive/Toggle to add/remove current article from user's archive, no page reload required
    function toggleArchive() {
        if (!archiveToggle) return;
        var articleId = archiveToggle.dataset.articleId;
        if (!articleId) return;

        fetch('/Archive/Toggle', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                'RequestVerificationToken': getToken() || ''
            },
            body: new URLSearchParams({ articleId: articleId })
        })
            .then(function (res) { return res.json(); })
            .then(function (data) {
                if (!data || !data.success) return;
                var archived = !!data.archived;
                archiveToggle.dataset.archived = archived ? 'true' : 'false';
                archiveToggle.textContent = archived ? 'Remove from archive' : 'Archive article';
                if (archiveStatus) archiveStatus.textContent = archived ? 'Saved to your archive' : 'Removed from archive';
            });
    }

    // formats current date
    function initHeaderDate() {
        var el = document.getElementById('header-date');
        if (!el) return;
        var now = new Date();
        var days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
        var months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        el.textContent = days[now.getDay()] + ', ' + months[now.getMonth()] + ' ' + now.getDate() + ' ' + now.getFullYear();
    }


    // main init, runs after the DOM is fully parsed, this makes everything else happen
    document.addEventListener('DOMContentLoaded', function () {
        overlay = document.getElementById('article-overlay');
        overlayPanel = overlay ? overlay.querySelector('.overlay-panel') : null;
        overlayClose = document.getElementById('overlay-close');
        archiveToggle = document.getElementById('archive-toggle');
        archiveStatus = document.getElementById('archive-status');

        if (overlay && overlayClose) {
            overlayClose.addEventListener('click', closeOverlay);
            overlay.addEventListener('click', function (e) {
                if (e.target === overlay) closeOverlay();
            });

            document.addEventListener('keydown', function (e) {
                if (e.key === 'Escape') closeOverlay();
            });

            initLane('lane-left', 'lane-left-inner', 'up');
            initLane('lane-right', 'lane-right-inner', 'down');
            renderRecent();
        }

        if (archiveToggle) archiveToggle.addEventListener('click', toggleArchive);

        initProfile();
        initHeaderDate();
    });
}());