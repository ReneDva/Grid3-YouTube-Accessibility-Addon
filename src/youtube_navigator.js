/**
 * YouTube Navigator V6.9 - Server Edition
 * מבוסס על גרסה 5 - כולל האזנה בפורט 3000 ותיקון לדילוג פרסומות
 */

const CDP = require('chrome-remote-interface');
const http = require('http'); // נוסף עבור השרת
const { spawn, exec } = require('child_process');
const path = require('path');

// קונפיגורציית חיבור לדפדפן
const CHROME_CONFIG = { host: '127.0.0.1', port: 15432 };
const delay = (ms) => new Promise(resolve => setTimeout(resolve, ms));
const IS_SYSTEM_DEBUG = process.argv.includes('debug');

/**
 * פונקציה לווידוא שתוכנת דילוג המודעות (Skipper) רצה ברקע.
 */
async function ensureSkipperRunning() {
    return new Promise((resolve) => {
        exec('tasklist', (err, stdout) => {
            if (stdout.toLowerCase().includes('skip_ads.exe')) {
                if (IS_SYSTEM_DEBUG) console.log("[Debug] Skip Ads is already running.");
                resolve(true); 
            } else {
                if (IS_SYSTEM_DEBUG) console.log("[Debug] Starting Skip Ads...");
                const skipperPath = path.join(process.cwd(), 'skip_ads.exe');
                const child = spawn(skipperPath, [], {
                    detached: true,
                    stdio: 'ignore',
                    windowsHide: true
                });
                child.unref(); 
                resolve(true);
            }
        });
    });
}

/**
 * הפונקציה המרכזית לניהול פקודות הניווט (הלוגיקה המקורית של V5)
 */
async function navigateYouTube(action, query = '') {
    
    if (action === 'exit') {
        console.log("Initiating Graceful Shutdown...");
        try {
            const targets = await CDP.List(CHROME_CONFIG);
            const target = targets.find(t => t.type === 'page');
            if (target) {
                const clientExit = await CDP({ target });
                // שליחת פקודת סגירה רשמית לדפדפן כדי למנוע הודעת "שחזור דפים"
                await clientExit.Browser.close();
                await clientExit.close();
            }
        } catch (e) {
            // Fallback במקרה שהדפדפן כבר סגור או לא מגיב
        }
        
        exec('taskkill /F /IM skip_ads.exe >nul 2>&1');
        
        // המתנה קצרה לסגירה סופית של התהליכים לפני יציאה מהסקריפט
        setTimeout(() => {
            process.exit(0);
        }, 1000);
        
        return "System Exit";
    }

    await ensureSkipperRunning();

    let client;
    try {
        const targets = await CDP.List(CHROME_CONFIG);
        let target = targets.find(t => t.type === 'page' && t.url.includes('youtube.com'));
        if (!target) target = targets.find(t => t.type === 'page');
        if (!target) return "Error: YouTube tab not found.";

        client = await CDP({ target });
        const { Runtime, Page, Browser, Target, Input } = client;
        
        await Runtime.enable();
        await Page.enable();

        if (action === 'home') {
            await Page.navigate({ url: 'https://www.youtube.com' });
            await delay(4000);
            action = 'open'; 
        }

        if (action === 'refresh') {
            await Page.reload({ ignoreCache: false });
            return "Refreshed";
        }

        if (action === 'open' || action === 'search') {
            try {
                await Target.activateTarget({ targetId: target.id }); 
                const { windowId } = await Browser.getWindowForTarget({ targetId: target.id });
                await Browser.setWindowBounds({ windowId, bounds: { windowState: 'maximized' } });
            } catch (e) {}
        }

        if (action === 'toggle' || action === 'fullscreen') {
            await Input.dispatchKeyEvent({ type: 'keyDown', key: 'f', code: 'KeyF', windowsVirtualKeyCode: 70 });
            await Input.dispatchKeyEvent({ type: 'keyUp', key: 'f', code: 'KeyF', windowsVirtualKeyCode: 70 });
            return "Toggle FS";
        }

        if (action === 'open' && query) {
            await Page.navigate({ url: query.trim() });
        } else if (action === 'search' && query) {
            await Page.navigate({ url: `https://www.youtube.com/results?search_query=${encodeURIComponent(query.trim())}` });
            await delay(2500);
        }

        if (action === 'back') {
            await Runtime.evaluate({ expression: "window.history.back()" });
            await delay(3000);
        }

        const navScript = `
            (function() {
                const action = '${action}';
                const isShortsPage = window.location.href.includes('/shorts/');
                
                if (action === 'play_pause') {
                    if (isShortsPage) {
                        const shortsPlayBtn = document.querySelector('.ytd-shorts-player-controls button, #play-pause-button-shape button');
                        if (shortsPlayBtn) { shortsPlayBtn.click(); return "Shorts Play/Pause Toggled"; }
                    } else {
                        const video = document.querySelector('video');
                        if (video) { video.paused ? video.play() : video.pause(); return "Video Play/Pause Toggled"; }
                    }
                    return "Play button not found";
                }

                if (action === 'like') {
                    let likeBtn;
                    if (isShortsPage) {
                        likeBtn = document.querySelector('ytd-reel-video-renderer[is-active] #like-button button, reel-action-bar-view-model button[aria-label*="לייק"]');
                    } else {
                        likeBtn = document.querySelector('#top-level-buttons-computed segmented-like-dislike-button-view-model button[aria-label*="לייק"]');
                    }
                    if (likeBtn) { likeBtn.click(); return "Liked!"; }
                    return "Like button not found";
                }

                const clearHighlights = () => {
                    document.querySelectorAll('*').forEach(i => {
                        if (i.style && i.style.outlineColor === 'rgb(255, 0, 0)') { i.style.outline = 'none'; }
                    });
                };

                const highlight = (el) => {
                    if (!el) return;
                    clearHighlights();
                    el.style.outline = '8px solid #FF0000';
                    el.style.outlineOffset = '-8px';
                    el.style.zIndex = '9999';
                    el.scrollIntoView({ behavior: 'smooth', block: 'center' });
                };

                const getItems = () => {
                    if (isShortsPage) return [];
                    const isWatchPage = window.location.href.includes('watch?v=');
                    const isHomePage = window.location.pathname === '/' || window.location.pathname === '';
                    if (isWatchPage) {
                        const playlistItems = Array.from(document.querySelectorAll('ytd-playlist-panel-video-renderer')).filter(el => el.offsetParent !== null);
                        if (playlistItems.length > 0) return playlistItems;
                        return Array.from(document.querySelectorAll('ytd-compact-video-renderer, yt-lockup-view-model')).filter(el => {
                            const isAd = el.querySelector('ytd-ad-slot-renderer') || el.closest('ytd-ad-slot-renderer');
                            return el.offsetParent !== null && !isAd;
                        });
                    } 
                    if (isHomePage) {
                        return Array.from(document.querySelectorAll('ytd-rich-item-renderer')).filter(el => !el.querySelector('ytd-ad-slot-renderer') && el.offsetParent !== null);
                    }
                    // תיקון: הוספת ytm-shorts-lockup-view-model-v2 לתוצאות החיפוש
                    return Array.from(document.querySelectorAll('ytd-video-renderer, ytd-grid-video-renderer, yt-lockup-view-model, ytm-shorts-lockup-view-model-v2')).filter(el => el.offsetParent !== null && !el.closest('ytd-shelf-renderer'));
                };

                if (isShortsPage) {
                    if (action === 'down') {
                        const btnDown = document.querySelector('#navigation-button-down button');
                        if (btnDown) { btnDown.click(); return "Shorts Down"; }
                    }
                    if (action === 'up') {
                        const btnUp = document.querySelector('#navigation-button-up button');
                        if (btnUp) { btnUp.click(); return "Shorts Up"; }
                    }
                }

                let items = getItems();
                if (action === 'search' || action === 'open' || action === 'back' || window.navIndex === undefined) {
                    window.navIndex = 0;
                    if (items.length > 0) highlight(items[0]);
                    return "Focus Reset";
                } 
                if (action === 'down' || action === 'up') {
                    action === 'down' ? window.navIndex++ : window.navIndex--;
                    window.navIndex = Math.max(0, Math.min(window.navIndex, items.length - 1));
                    highlight(items[window.navIndex]);
                    return "Moved to " + window.navIndex;
                } 
                if (action === 'enter') {
                    const target = items[window.navIndex || 0];
                    if (!target) return "No target";
                    // תיקון: הוספת reel-item-endpoint עבור לחיצה על שורטס בחיפוש
                    const link = target.querySelector('a#wc-endpoint, a#thumbnail, a#video-title, a.yt-lockup-metadata-view-model__title, a.reel-item-endpoint, a');
                    if (link) { window.navIndex = undefined; link.click(); return "Navigating..."; }
                }
                return "Ready";
            })();
        `;
        const result = await Runtime.evaluate({ expression: navScript });
        return result.result?.value || "Done";

    } catch (err) {
        return "Error: " + err.message;
    } finally {
        if (client) {
            await delay(100);
            await client.close(); // שחרור הדיבאגר - קריטי לדילוג פרסומות
        }
    }
}

// שרת ה-HTTP להאזנה לפקודות - גרסה מתוקנת לתמיכה בעברית
const server = http.createServer(async (req, res) => {
    // תיקון 1: הוספת charset=utf-8 כדי שהדפדפן והשרת יסכימו על פורמט הטקסט
    res.writeHead(200, { 
        'Content-Type': 'text/plain; charset=utf-8', 
        'Access-Control-Allow-Origin': '*' 
    });

    try {
        // תיקון 2: פענוח ה-URL רק לאחר הסרת הלוכסן, למניעת שגיאות בתווים מיוחדים
        const rawPath = req.url.startsWith('/') ? req.url.slice(1) : req.url;
        const decodedPath = decodeURIComponent(rawPath);
        
        // פיצול לפי נקודתיים ראשונות בלבד
        const firstColon = decodedPath.indexOf(':');
        let action, query;
        
        if (firstColon !== -1) {
            action = decodedPath.substring(0, firstColon).trim();
            query = decodedPath.substring(firstColon + 1).trim();
        } else {
            action = decodedPath.trim();
            query = '';
        }

        if (action) {
            console.log(`Executing: ${action} ${query ? '[' + query + ']' : ''}`);
            const result = await navigateYouTube(action, query);
            res.end(result);
        } else {
            res.end("No action provided");
        }
    } catch (e) {
        console.error("Server Error:", e);
        res.end("Error occurred: " + e.message);
    }
});

server.listen(3000, () => {
    console.log("YouTube Navigator V6.9 - Hebrew Fixed");
    console.log("Server running on http://localhost:3000");
    ensureSkipperRunning();
});