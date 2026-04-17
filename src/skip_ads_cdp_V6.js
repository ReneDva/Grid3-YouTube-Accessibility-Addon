/**
 * YouTube Ad-Skipper Service V6.9.2 - Stabilized Version
 * קובץ זה מיועד להרצה כ-skip_ads.exe
 * מתוקן למניעת גמגום (Play/Pause) בזמן פרסומות
 */

const CDP = require("chrome-remote-interface");
const fs = require("fs");
const path = require("path");

// ===============================
// 0. הגדרות בסיס
// ===============================
const CHROME_CONFIG = {
    host: '127.0.0.1',
    port: 15432 // פורט מותאם לסקריפט ה-BAT שלך
};

const LOG_FILE = path.join(process.cwd(), "debug_log_skipper.txt");
let lastStatus = "";
const IS_DEBUG = process.argv.includes('--debug');

function logger(message, forceConsole = false) {
    if (message === lastStatus && !forceConsole) return;
    lastStatus = message;

    const timestamp = new Date().toLocaleString('he-IL');
    const logEntry = `[${timestamp}] ${message}\n`;

    try {
        fs.appendFileSync(LOG_FILE, logEntry);
    } catch (e) {}

    const isAction = message.includes("⏭️") || message.includes("❌") || message.includes("✅");
    if (IS_DEBUG || forceConsole || isAction) {
        console.log(`[${new Date().toLocaleTimeString()}] ${message}`);
    }
}

// ===============================
// 1. פונקציות עזר וחיפוש טאב
// ===============================

async function findYouTubeTab() {
    try {
        const targets = await CDP.List({ host: CHROME_CONFIG.host, port: CHROME_CONFIG.port });
        return targets.find(t =>
            t.type === "page" &&
            typeof t.url === "string" &&
            // תיקון קריטי: מתחבר רק לסרטונים פעילים ולא לטאבים שקופים של פרסומות
            (t.url.includes("youtube.com/watch") || t.url.includes("youtube.com/shorts"))
        );
    } catch (e) {
        return null;
    }
}

// סקריפט הזרקה לדפדפן - משופר למניעת לחיצות כפולות
const browserSideScript = `
(function() {
  function isVisible(el) {
    if (!el) return false;
    const style = window.getComputedStyle(el);
    const rect = el.getBoundingClientRect();
    // בדיקה מחמירה: אלמנט חייב להיות גלוי, עם שקיפות מלאה ובמיקום הגיוני על המסך
    return (
        style.display !== 'none' && 
        style.visibility !== 'hidden' && 
        parseFloat(style.opacity) > 0.5 && 
        rect.width > 5 &&
        rect.height > 5 &&
        rect.left >= 0
    );
  }

  const skipSelectors = [
    '.ytp-skip-ad-button', 
    '.ytp-ad-skip-button-modern', 
    '.ytp-ad-skip-button',
    '.ytp-ad-skip-button-container', 
    '.ytp-skip-ad-button__text'
  ];

  for (const sel of skipSelectors) {
    const el = document.querySelector(sel);
    if (el && isVisible(el)) {
        const rect = el.getBoundingClientRect();
        // מחזירים קואורדינטות רק אם הכפתור באמת על המסך
        return { 
            status: "⏭️ Skip Found", 
            x: Math.round(rect.left + rect.width / 2), 
            y: Math.round(rect.top + rect.height / 2) 
        };
    }
  }

  // סגירת באנרים (X)
  const closeAd = document.querySelector('.ytp-ad-overlay-close-button');
  if (closeAd && isVisible(closeAd)) {
      const rect = closeAd.getBoundingClientRect();
      return { 
          status: "⏭️ Close Found", 
          x: Math.round(rect.left + rect.width / 2), 
          y: Math.round(rect.top + rect.height / 2) 
      };
  }

  return document.querySelector('.ad-showing, .ad-interrupting') ? "📢 Ad playing" : "▶️ No ad";
})();
`;

// ===============================
// 2. לוגיקת הרצה
// ===============================

async function runEngine(ytClient) {
    const { Input, Runtime } = ytClient;
    try {
        const res = await Runtime.evaluate({ expression: browserSideScript, returnByValue: true });
        const result = res.result.value;

        if (result && typeof result === 'object' && result.status && result.status.includes("Found")) {
            const { x, y } = result;
            
            // ביצוע לחיצה אמינה
            await Input.dispatchMouseEvent({ type: 'mousePressed', x, y, button: 'left', clickCount: 1 });
            await Input.dispatchMouseEvent({ type: 'mouseReleased', x, y, button: 'left', clickCount: 1 });
            
            logger(`⏭️ דילוג בוצע במיקום: ${x},${y}`, true);
            return "Skipped";
        }
        
        logger(typeof result === 'string' ? result : "Waiting...");
    } catch (e) {
        throw new Error("Connection lost");
    }
}

async function startApp() {
    logger("🚀 Ad-Skipper V6.9.2 Started", true);

    while (true) {
        let ytTab = await findYouTubeTab();
        
        if (!ytTab) {
            await new Promise(r => setTimeout(r, 4000));
            continue;
        }

        let client;
        try {
            client = await CDP({ 
                host: CHROME_CONFIG.host, 
                port: CHROME_CONFIG.port, 
                target: ytTab.id 
            });
            
            await client.Runtime.enable();
            logger(`✅ מחובר לטאב: ${ytTab.title.substring(0,30)}...`, true);

            while (true) {
                await runEngine(client);
                // הגדלת ההמתנה ל-1.5 שניות ליציבות ומניעת עומס על ה-CDP
                await new Promise(r => setTimeout(r, 1500)); 
            }
        } catch (err) {
            logger(`⚠️ ניתוק או שגיאה: ${err.message}`);
            if (client) await client.close().catch(() => {});
        }
        
        await new Promise(r => setTimeout(r, 3000));
    }
}

// הפעלה
startApp().catch(err => {
    logger(`❌ שגיאה קריטית: ${err.message}`, true);
    process.exit(1);
});