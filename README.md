# Grid3-YouTube-Accessibility-Addon

A desktop app bridging Grid 3 AAC software with Chrome Canary to enable hands-free YouTube control for eye-tracking and switch-access users. Features home/Shorts navigation, search with scrolling, and automatic "Skip Ad" clicking the moment the button appears.

---

## YouTube Accessibility Controller for Eye-Tracking Users

**Grid3-YouTube-Accessibility-Addon** is a desktop application that integrates **Grid 3** communication software with **Chrome Canary**, giving YouTube navigation and control abilities to users relying on eye-tracking or switch-based assistive technology.

### Key Features

- **Navigate YouTube Home** — Browse recommended videos with Grid buttons (using computer-control grids)
- **Control Shorts** — Scroll through Shorts and interact with the feed hands-free
- **Navigate "Up Next"** — Browse the next/recommended videos while a video is playing
- **YouTube Search** — Enter pre-determined queries and scroll through search results using gaze or switches
- **Automatic "Skip Ad" Clicker** — Instantly clicks the "Skip Ad" button as soon as it appears, solving accessibility problems where the button is hard to find or triggers only on hover

---

## How It Works

- The program runs in the background, automatically starting when the matching Grid 3 board/command is activated.
- Launches **Chrome Canary** on port **15432** and opens YouTube’s home page, placing the browser window behind the active Grid 3 window for seamless interaction.
- Runs a background "Skip Ads" process (`skip_ads.exe`) that constantly monitors for and clicks the "Skip Ad" button to help users who can’t reach or time the click manually.
- Accepts commands from Grid cells via port **3000**: Each cell in Grid 3 represents a YouTube action (e.g., scroll, next video, search, Shorts navigation). When a user triggers a cell, the grid sends a script command (with parameters, like search terms) to the running app.
- During navigation, the currently focused item on YouTube is outlined in red for clear visual feedback.
- Actions include: open/close tab, go home, search, scroll, activate videos, toggle fullscreen, like, navigate Shorts, and more.

---

## Example: Core System Logic

```js name=core.js
// YouTube Navigator V6.9 - Main logic handling Chrome connection and user commands via HTTP, including automatic ad-skipping.
const CDP = require('chrome-remote-interface');
const http = require('http');
const { spawn, exec } = require('child_process');
const path = require('path');

// ...rest of your code provided above...
```

(See actual source files for details)

---

## Why Accessibility Matters

The standard YouTube interface assumes fast, precise mouse or touch input. The "Skip Ad" button is especially challenging for slow or motor-impaired users—it’s only visible on hover and changes position. This addon removes those barriers, letting eye-tracking and switch users enjoy YouTube more independently.

---

## Tech Stack

- Node.js (server and automation scripts)
- Chrome DevTools Protocol (`chrome-remote-interface`)
- Grid 3 (AAC software for eye-gaze/switch boards)
- Chrome Canary (must be launched with remote debugging enabled)
- Companion executable: `skip_ads.exe` (auto ad skipper)

**Keywords:**  
`grid3` `eye-tracking` `AAC` `accessibility` `youtube-automation` `chrome-canary` `assistive-technology` `switch-access`

---

## Getting Started

1. **Prerequisites**
    - [Node.js](https://nodejs.org/)
    - [Chrome Canary](https://www.google.com/chrome/canary/) (must run with `--remote-debugging-port=15432`)
    - `skip_ads.exe` utility in the working directory
    - Grid 3 software with compatible board configured to send actions to localhost:3000

2. **Installation**
    ```bash
    git clone https://github.com/<your_username>/Grid3-YouTube-Accessibility-Addon.git
    cd Grid3-YouTube-Accessibility-Addon
    npm install
    ```

3. **Running the App**
    - Start Chrome Canary with remote debugging enabled:
      ```bash
      "C:\Path\To\chrome-canary.exe" --remote-debugging-port=15432
      ```
    - Start the server:
      ```bash
      node core.js
      ```
    - Grid cells trigger HTTP calls (examples: `http://localhost:3000/up`, `http://localhost:3000/search:music`).

4. **Connecting with Grid 3**
    - Use the Grid 3 editor to configure cells that send HTTP requests to your server actions with required parameters (e.g., navigation, search terms).

---

## License

[MIT](LICENSE)
