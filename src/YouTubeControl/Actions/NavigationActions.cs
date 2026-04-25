// src/YouTubeControl/Actions/NavigationActions.cs
// Provides JavaScript snippets for YouTube navigation and interaction actions.
namespace YouTubeControl.Actions;

internal static class NavigationActions
{
    private const string ActionPlaceholder = "__ACTION__";

    private const string NavScriptTemplate = """
        (function() {
            const action = __ACTION__;
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

            if (action === 'fullscreen' || action === 'toggle') {
                const fullscreenBtn = document.querySelector('button.ytp-fullscreen-button');
                if (fullscreenBtn) { fullscreenBtn.click(); return "Fullscreen Toggled"; }

                const video = document.querySelector('video');
                if (video) {
                    document.dispatchEvent(new KeyboardEvent('keydown', { key: 'f', code: 'KeyF', bubbles: true }));
                    document.dispatchEvent(new KeyboardEvent('keyup', { key: 'f', code: 'KeyF', bubbles: true }));
                    return "Fullscreen Key Toggled";
                }

                return "Fullscreen target not found";
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
                const link = target.querySelector('a#wc-endpoint, a#thumbnail, a#video-title, a.yt-lockup-metadata-view-model__title, a.reel-item-endpoint, a');
                if (link) { window.navIndex = undefined; link.click(); return "Navigating..."; }
            }
            return "Ready";
        })();
        """;

    public static string BuildNavScript(string action)
    {
        var encodedAction = QuoteJsString(action ?? string.Empty);
        return NavScriptTemplate.Replace(ActionPlaceholder, encodedAction, StringComparison.Ordinal);
    }

    private static string QuoteJsString(string value)
    {
        return "'" + value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("'", "\\'", StringComparison.Ordinal)
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal)
            + "'";
    }
}