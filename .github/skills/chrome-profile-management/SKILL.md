---
name: chrome-profile-management
description: "Implement Chrome Local State profile discovery and visual profile selection UI, then persist profileDirectory mapping for silent future launches. Use for first-run profile setup."
---

# Skill 04 — Chrome Profile Management

**Project:** Grid3-YouTube-Accessibility-Addon V7  
**File scope:** `src/YouTubeControl/ProfileSetupForm.cs`, `src/YouTubeControl/Models/AppConfig.cs`  
**Goal:** Turn profile selection into a visual, Chrome-familiar experience — bridging the gap between the display name the user knows ("Rene") and the technical folder path Chrome uses ("Profile 2") — then persist only what is needed for silent future launches.

---

## 1. Visual Profile Discovery

**What to implement:**  
Scan Chrome's `Local State` file to extract the visual metadata for every profile on the machine.

**Location of the file:**
```
<userDataDir>\Local State
```
`userDataDir` is read from `AppConfig`. Default is `%LOCALAPPDATA%\Google\Chrome SxS\User Data` (Canary) or `%LOCALAPPDATA%\Google\Chrome\User Data` (stable).

**JSON structure to parse:**
```json
{
  "profile": {
    "info_cache": {
      "Default": {
        "name": "Person 1",
        "user_name": "email@gmail.com",
        "gaia_picture_file_name": "Google Profile Picture.png"
      },
      "Profile 2": {
        "name": "Rene",
        "user_name": "rene@example.com",
        "gaia_picture_file_name": "Google Profile Picture.png"
      }
    }
  }
}
```

**Fields to extract per profile:**

| JSON key | Purpose |
|---|---|
| Object key (e.g. `"Profile 2"`) | Folder name — used in the `--profile-directory` flag |
| `name` | Display name — shown on the card |
| `user_name` | Email — shown below the name on the card |
| `gaia_picture_file_name` | File name of the avatar image, located at `<userDataDir>\<folderName>\<filename>` |

If `Local State` is missing or unreadable, return an empty list gracefully — never throw.

---

## 2. Chrome-Like Identity Mapping

**What to implement:**  
A `Dictionary<string, ChromeProfileInfo>` (or equivalent) that maps the **Display Name** to the profile's full metadata including the **Folder Name**.

**Why this matters:**  
The user picks "Rene" from the UI. The flag that Chrome needs is `--profile-directory="Profile 2"`. The dictionary is the bridge between what the user sees and what the OS needs.

**`ChromeProfileInfo` record fields:**

| Field | Type | Source |
|---|---|---|
| `FolderName` | `string` | JSON object key |
| `DisplayName` | `string` | `name` field |
| `Email` | `string` | `user_name` field |
| `LocalAvatarPath` | `string?` | `<userDataDir>\<folderName>\<gaia_picture_file_name>` |

---

## 3. Familiar Selection UI

**What to implement:**  
A WinForms dialog that looks like Chrome's own "Who's using Chrome?" screen.

**Layout:**

- Dark background: `#202124` (Chrome dark mode grey).
- Title label at the top: `"Who's using YouTube?"`, large font (Segoe UI 18pt).
- A `FlowLayoutPanel` (left-to-right, wrapping) holding one **card** per profile.

**Each card is a `Panel` (150 x 180 px) containing:**

1. A `PictureBox` (96 x 96, centered): shows the avatar image if `LocalAvatarPath` exists and is readable; otherwise shows a generated initial badge (first letter of the display name, on a colored circle derived from the name's hash).
2. A `Label` for the display name (bold, white, centered, ellipsis overflow).
3. A `Label` for the email (small, grey `#9AA0A6`, centered, ellipsis overflow).

**Interaction:**

- Cards must have large click targets — the entire card panel is clickable, including the image and both labels.
- Clicking a card highlights it (change card `BackColor` to a Chrome-blue tint: `#4285F4` at low opacity) and enables the **Done** button.
- The **Done** button (`"Done"`, 140 x 48 px, bottom-right) is disabled until a card is selected.
- Pressing Done sets `DialogResult = OK` and closes the form.

**Accessibility requirement:**  
Button and card sizes must be large enough for reliable eye-gaze activation. Minimum touch/gaze target: 44 x 44 px. Cards at 150 x 180 px meet this requirement. Do not make them smaller.

---

## 4. Selection Persistence

**What to save to `config.json`:**  
Only two fields. Nothing else from the selection UI should be persisted.

```json
{
  "profileDirectory": "Profile 2",
  "profileDisplayName": "Rene"
}
```

**When to save:**  
As soon as the user clicks a card — before they click Done. This avoids data loss if the form is closed by any means other than the Done button.

**Effect on subsequent launches:**  
On every launch after the first, `Program.cs` reads `config.json`, finds `profileDirectory` is non-null, and skips the profile setup form entirely. The Leader goes directly to headless operation with `--profile-directory="Profile 2"`. The user never sees the picker again unless `config.json` is deleted.

**What NOT to save:**
- The avatar image path — it can be re-derived from `Local State` if ever needed.
- The email address.
- Any Chrome internal IDs.

---

## Checklist for the agent

- [ ] `Local State` is read from `<userDataDir>\Local State`
- [ ] Parsing failure returns an empty list, never throws
- [ ] `FolderName` (object key) is stored separately from `DisplayName` (name field)
- [ ] Avatar is loaded from `<userDataDir>\<folderName>\<gaia_picture_file_name>` if the file exists
- [ ] Fallback avatar is a colored circle with the first initial (not a generic icon)
- [ ] UI background is dark (`#202124`)
- [ ] Cards are at least 44 x 44 px (150 x 180 px recommended)
- [ ] Entire card panel is clickable, not just a button inside it
- [ ] Done button is disabled until a card is selected
- [ ] Clicking a card immediately saves `profileDirectory` and `profileDisplayName` to `config.json`
- [ ] On subsequent launches, setup form is skipped when `profileDirectory` is non-null in config
- [ ] Only `profileDirectory` and `profileDisplayName` are written — no email, no avatar path
