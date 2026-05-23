<div dir="rtl" lang="he" style="direction:rtl; text-align:right;">

<img src="icon_v7.ico" align="left" width="100">

# מדריך התקנה והגדרה (V7) - להורים, מורים ומטפלים

מדריך זה מסביר כיצד להתקין ולהשתמש בתוסף ליוטיוב עבור Grid 3 למחשב (גרסה V7). פרטים טכניים על אופן פעולת המערכת מאחורי הקלעים נמצאים בתיעוד ה-README הראשי.

*משתמשים שרוצים להגדיר לוח באופן עצמאי יכולים לדלג ל-[הגדרת לוח מ-0 באופן עצמאי](#הגדרת-לוח-מ-0-באופן-עצמאי). [קישור לאתר הקהילה להורדת לוח לדוגמה (בקרוב)]*

---

## דרישות מוקדמות

לפני ההתקנה ודאו שקיימים:

- מחשב עם Windows 10 או Windows 11.
- תוכנת Grid 3 מותקנת ומורשית.
- **פרטי החשבון של המשתמש:** כתובת מייל, סיסמה וגישה לאמצעי אימות נוסף (כגון טלפון) אם מוגדר אימות דו-שלבי (2FA).
- קובץ התקנה: `Output\YouTube_V7_Full_Installer.exe` (ניתן להורדה מ-[קישור זה](https://github.com/ReneDva/Grid3-YouTube-Accessibility-Addon/releases/latest)).

**שימו לב:**
התוסף משתמש בגרסה מיוחדת של דפדפן בשם **Chrome Canary** (הלוגו שלו מופיע מטה). יוטיוב ייפתח בגרסת ה-WEB שלו בתוך דפדפן זה, ולא דרך אפליקציית יוטיוב הרגילה למחשב או דפדפן כרום הרגיל.

<img src="Chrome-canary-logo.svg" width="64">

---

## התקנה (V7)

1. הפעילו את קובץ ההתקנה Output\YouTube_V7_Full_Installer.exe כמנהל מערכת (Administrator).
2. **התראת אבטחה:** ייתכן ו-Windows תציג הודעה שהקובץ אינו מוכר. במקרה כזה, לחצו על **"מידע נוסף" (More info)** ואז על **"הפעל בכל זאת" (Run anyway)**.
3. עקבו אחר הוראות אשף ההתקנה עד לסיומו.
4. **בסיום ההתקנה:** ייפתח באופן אוטומטי חלון חדש של **Chrome Canary**.
5. **התחברות ראשונית:** עליכם לבצע התחברות לחשבון ה-Google של הילד בתוך החלון שנפתח.
6. **שלב אחרון:** לאחר התחברות מוצלחת, **חובה לבצע הפעלה מחדש (Restart) למחשב** לפני תחילת השימוש היומיומי.

---

## התחברות בהפעלה ראשונית

אם סגרתם בטעות את חלון ה-Chrome Canary לפני שביצעתם התחברות:
1. חפשו בשולחן העבודה את קיצור הדרך עם האייקון של האפליקציה:
   <img src="icon_v7.ico" width="32">
2. הפעילו אותו ידנית. חלון ה-Chrome Canary ייפתח שוב.
3. השלימו את ההתחברות, ודאו שיוטיוב עובד, ולאחר מכן **בצעו הפעלה מחדש למחשב**.

לאחר מכן, התלמיד יוכל להפעיל את התוסף ישירות מתוך ה-Grid set שלו.

---

## המלצות בטיחות
- במידה ומדובר במשתמש צעיר, מומלץ מאוד להגדיר את חשבון ה-Google שלו כחשבון ילד מפוקח (באמצעות Google Family Link).
- הורים, מורים ומטפלים צריכים לנטר באופן קבוע את התכנים שהתלמיד ניגש אליהם.

---

## יציאה בטוחה

פעולת הסגירה מובנית ישירות בתוך כפתור **"חזור למסך יישומים"** ב-Grid 3. שימוש בכפתור זה מבטיח שהדפדפן והתוסף ייסגרו בצורה נקייה ושקטה ברקע. הנחיה זו תקפה לכולם, בין אם אתם משתמשים בלוח מוכן מראש ובין אם בלוח שיצרתם בעצמכם.

---

## תרשים זרימת פעולות משתמש (Grid 3)

```mermaid
flowchart TD
   A[פתיחת לוח יוטיוב] --> B[לחיצה על כפתור התחלה]
   B --> C[האפליקציה מפעילה את Chrome ברקע]
   C --> C1[הילד צופה באנימציית פתיחה עם סאונד]
   C1 --> C2[יוטיוב מוכן לשימוש]

   C2 --> D{בחירת פעולה ב-Grid 3}
   D --> E[בית]
   E --> E1[חזרה לדף הבית של יוטיוב]

   D --> F[למעלה או למטה]
   F --> F1[מעבר לסרטון הבא או הקודם ברשימה]

   D --> G[בחירה]
   G --> G1[פתיחת הסרטון שהלוח מסמן]

   D --> H[נגן / השהה]
   H --> H1[הפסקת הסרטון או המשך ניגון]

   D --> I[מסך מלא]
   I --> I1[כניסה למצב מסך מלא או יציאה ממנו]

   D --> J[לייק]
   J --> J1[סימון 'אהבתי' לסרטון]

   D --> K[חיפוש: מילות חיפוש]
   K --> K1[פתיחת תוצאות החיפוש ביוטיוב]

   D --> L[open: קישור לסרטון ביוטיוב]
   L --> L1[מעבר ישיר לקישור שנשלח]

   D --> M[רענון]
   M --> M1[טעינה מחדש של העמוד]

   D --> N[חזרה]
   N --> N1[חזרה לעמוד הקודם]

   D --> O[יציאה]
   O --> O1[סגירת יוטיוב וחזרה ל-Grid Explorer]
```

**טיפ:** אם מסגרת הניווט האדומה לא מופיעה על המסך, לחצו פעם אחת על כפתור ניווט (למעלה או למטה). המסגרת תופיע ותאפשר להתחיל לבחור סרטונים.

---

## פתרון תקלות (V7)

אם נתקלתם בבעיות (Chrome לא נפתח, פקודות לא מגיבות וכדומה):

1.  **בדיקת תאים בודדים:** אם רק תא מסוים לא עובד, בדקו את הגדרת הפעולה בו. ודאו שאין שגיאות כתיב בפרמטרים ושהתא מפעיל את התוכנה הנכונה.
2.  **פתרון כללי:** אם הבעיה כוללת יותר מתא אחד או שהדף מתנהג בצורה מוזרה, הפתרון העיקרי הוא **לבצע הפעלה מחדש למחשב**.
    *   *הערה טכנית:* ניתן גם לנסות לסגור את `YouTubeControl.exe` דרך **מנהל המשימות (Task Manager)** ולהפעיל מחדש מהגריד, אך עבור רוב המשתמשים, הפעלה מחדש של המחשב היא הדרך הפשוטה והיעילה ביותר לאפס את מצב התוכנה שרצה ברקע (במיוחד כשכפתור ה-X של הדפדפן אינו סוגר את התוסף).

---

## הגדרת לוח מ-0 באופן עצמאי

עבור משתמשים היוצרים לוח (Grid set) משלהם, עליכם להגדיר בתאים את פעולת **Run Program** עם נתיב האפליקציה והפרמטר המתאים מהרשימה מטה.

---

### שלב 1 — הפעלת מצב Computer Control

ודאו ש-Grid 3 פועל במצב **Computer Control**.

<p align="center"><img src="setup/computer-controll.png" width="400" alt="הפעלת Computer Control"/></p>

---

### שלב 2 — כך נראה הלוח

<p align="center"><img src="setup/opening-grid.png" width="500" alt="מראה הלוח"/></p>

תצוגת פקודות מלאה ומכווצת:

<table dir="rtl" style="margin:0 auto; border-collapse:collapse; white-space:nowrap;">
  <tr>
    <th style="text-align:center; padding:6px 12px;">תצוגת פקודות מלאה:</th>
    <th style="text-align:center; padding:6px 12px;">תצוגה מכווצת:</th>
  </tr>
  <tr>
    <td style="text-align:center; vertical-align:top; padding:8px;">
      <img src="setup/full-sidebar.png" style="width:180px; max-width:100%; height:auto; display:block; margin:0 auto;" alt="סרגל פקודות מלא"/>
    </td>
    <td style="text-align:center; vertical-align:top; padding:8px;">
      <img src="setup/short-sidebar.png" style="width:80px; max-width:100%; height:auto; display:block; margin:0 auto;" alt="סרגל פקודות מקוצר"/>
    </td>
  </tr>
</table>

הצגת פקודות נוספות / פחות:

<table dir="rtl" style="margin:0 auto; border-collapse:collapse; white-space:nowrap;">
  <tr>
    <th style="text-align:center; padding:6px 12px;">הצגת פקודות נוספות</th>
    <th style="text-align:center; padding:6px 12px;">הצגת פקודות מופחתות</th>
  </tr>
  <tr>
    <td style="text-align:center; vertical-align:top; padding:8px;">
      <img src="setup/more-commands.png" style="width:200px; max-width:100%; height:auto; display:block; margin:0 auto;" alt="פקודות נוספות"/>
    </td>
    <td style="text-align:center; vertical-align:top; padding:8px;">
      <img src="setup/less-commands.png" style="width:280px; max-width:100%; height:auto; display:block; margin:0 auto;" alt="פקודות מופחתות"/>
    </td>
  </tr>
</table>

---

### שלב 3 — הגדרת פעולת פתיחת הלוח

בלוח הפתיחה, בצעו את השלבים הבאים:

<ol style="direction:rtl; padding-inline-start:1.2em;">
   <li style="margin-bottom:18px; display:block; width:100%;">
      <p><strong>3.1 — הוספת הפעולה: Start Program</strong></p>
      <p style="margin:8px 0 0 0;"><img src="setup/start-program.png" width="500" alt="הוספת Start Program" style="max-width:100%; height:auto; display:block; margin:0 auto;"/></p>
   </li>
   <li style="margin-bottom:18px; display:inline-block; width:48%; vertical-align:top; text-align:center;">
      <p><strong>3.2 — בחירת קובץ התוכנה</strong></p>
      <p style="margin:8px 0 0 0;"><img src="setup/choose-program.png" width="400" alt="בחירת קובץ התוכנה" style="max-width:100%; height:auto; display:block; margin:0 auto;"/></p>
   </li>
   <li style="margin-bottom:18px; display:inline-block; width:48%; vertical-align:top; text-align:center;">
      <p><strong>3.3 — קביעת מיקום התוכנה במחשב</strong></p>
      <p style="margin:8px 0 0 0;"><img src="setup/where-program.png" width="600" alt="נתיב התוכנה" style="max-width:100%; height:auto; display:block; margin:0 auto;"/></p>
   </li>
</ol>

<div dir="ltr" lang="en" style="direction:ltr; text-align:left; width:100%; display:block; margin:0; padding:0;">
   <pre style="text-align:left; margin:0; padding:10px; background:#f7f7f7; border:1px solid #e1e1e1; border-radius:4px; overflow:auto;">Program:    C:\YouTube_Navigator_V7\YouTubeControl.exe
Parameters: (ריק — ללא פרמטרים)</pre>
</div>
---

### שלב 4 — הגדרת תאי פקודה
לכל כפתור פעולה, הגדירו **Start Program** עם הפרמטר המתאים:
---

<table dir="rtl" style="margin:12px auto; border-collapse:collapse; width:100%;">
  <tr>
    <td style="width:40%; padding:12px; vertical-align:top; text-align:center;">
      <img src="setup/start-program-with-parameters.png" style="max-width:720px; width:100%; height:auto; display:block; margin:0 auto;" alt="הגדרת פקודה עם פרמטרים"/>
    </td>
    <td style="width:60%; padding:12px; vertical-align:top; text-align:right;">
      <h3 style="margin-top:0;">רשימת כל הפקודות</h3>
      <table style="border-collapse:collapse; width:100%;">
         <thead>
            <tr>
               <th style="border-bottom:1px solid #ccc; padding:6px 8px; text-align:right;">פקודה</th>
               <th style="border-bottom:1px solid #ccc; padding:6px 8px; text-align:right;">מטרה</th>
               <th style="border-bottom:1px solid #ccc; padding:6px 8px; text-align:right;">פרמטר</th>
            </tr>
         </thead>
         <tbody>
            <tr>
               <td style="padding:6px 8px; text-align:right;">`home`</td>
               <td style="padding:6px 8px;">מעבר לדף הבית</td>
               <td style="padding:6px 8px; text-align:left;">home</td>
            </tr>
            <tr>
               <td style="padding:6px 8px; text-align:right">`down`</td>
               <td style="padding:6px 8px;">מעבר לסרטון הבא</td>
               <td style="padding:6px 8px; text-align:left;">down</td>
            </tr>
            <tr>
               <td style="padding:6px 8px; text-align:right">`up`</td>
               <td style="padding:6px 8px;">מעבר לסרטון הקודם</td>
               <td style="padding:6px 8px; text-align:left;">up</td>
            </tr>
            <tr>
               <td style="padding:6px 8px; text-align:right;">`enter`</td>
               <td style="padding:6px 8px;">בחירה/כניסה</td>
               <td style="padding:6px 8px;text-align:left;">enter</td>
            </tr>
            <tr>
               <td style="padding:6px 8px; text-align:right;">`back`</td>
               <td style="padding:6px 8px;">חזרה</td>
               <td style="padding:6px 8px;text-align:left;">back</td>
            </tr>
            <tr>
               <td style="padding:6px 8px; text-align:right;">`play_pause`</td>
               <td style="padding:6px 8px;">נגן/השהה</td>
               <td style="padding:6px 8px; text-align:left;">play_pause</td>
            </tr>
            <tr>
               <td style="padding:6px 8px; text-align:right;">`fullscreen`</td>
               <td style="padding:6px 8px;">מסך מלא</td>
               <td style="padding:6px 8px; text-align:left;">fullscreen</td>
            </tr>
            <tr>
               <td style="padding:6px 8px; text-align:right;">`like`</td>
               <td style="padding:6px 8px;">לייק</td>
               <td style="padding:6px 8px; text-align:left;">like</td>
            </tr>
            <tr>
               <td style="padding:6px 8px; text-align:right;">`refresh`</td>
               <td style="padding:6px 8px;">רענון דף</td>
               <td style="padding:6px 8px; text-align:left;">refresh</td>
            </tr>
            <tr>
               <td style="padding:6px 8px; text-align:right">`search:`</td>
               <td style="padding:6px 8px;">חיפוש</td>
               <td style="padding:6px 8px; text-align:left;">שירי ילדים :search</td>
            </tr>
            <tr>
               <td style="padding:6px 8px; text-align:right">`open:`</td>
               <td style="padding:6px 8px;">קישור ישיר</td>
               <td style="padding:6px 8px; text-align:left;">קישור לסרטון :open</td>
            </tr>
            <tr>
               <td style="padding:6px 8px; text-align:right;">`exit`</td>
               <td style="padding:6px 8px;">סגירת התוסף</td>
               <td style="padding:6px 8px; text-align:left;">exit</td>
            </tr>
         </tbody>
      </table>
    </td>
  </tr>
</table>

---

<!-- Actions shown as table pairs for reliable web side-by-side display -->
<table dir="rtl" style="margin:12px auto; border-collapse:collapse; white-space:nowrap;">
   <tr>
      <th style="text-align:center; padding:8px 12px;">פתיחת קישור — Open URL</th>
      <th style="text-align:center; padding:8px 12px;">Search — חיפוש</th>
   </tr>
   <tr>
      <td style="text-align:center; padding:8px; vertical-align:top;">
         <img src="setup/open-url-command.png" style="max-width:1350px; width:100%; height:auto; display:block; margin:0 auto;" alt="פתיחת קישור"/>
         <div style="font-size:0.95em; color:#333; margin-top:6px;">פרמטר: <code>קישור לסרטון :open</code></div>
      </td>
      <td style="text-align:center; padding:8px; vertical-align:top;">
         <img src="setup/search-command.png" style="max-width:675px; width:100%; height:auto; display:block; margin:0 auto;" alt="חיפוש"/>
         <div style="font-size:0.95em; color:#333; margin-top:6px;">פרמטר: <code>מילות חיפוש :search</code></div>
      </td>
   </tr>
</table>

<table dir="rtl" style="margin:12px auto; border-collapse:collapse; white-space:nowrap;">
   <tr>
      <th style="text-align:center; padding:8px 12px;">Up — ניווט למעלה</th>
      <th style="text-align:center; padding:8px 12px;">Enter — בחירה / הפעלה</th>
   </tr>
   <tr>
      <td style="text-align:center; padding:8px; vertical-align:top;">
         <img src="setup/up-command.png" style="max-width:675px; width:100%; height:auto; display:block; margin:0 auto;" alt="ניווט למעלה"/>
         <div style="font-size:0.95em; color:#333; margin-top:6px;">פרמטר: <code>up</code></div>
      </td>
      <td style="text-align:center; padding:8px; vertical-align:top;">
         <img src="setup/enter-command.png" style="max-width:675px; width:100%; height:auto; display:block; margin:0 auto;" alt="בחירה"/>
         <div style="font-size:0.95em; color:#333; margin-top:6px;">פרמטר: <code>enter</code></div>
      </td>
   </tr>
</table>

<table dir="rtl" style="margin:12px auto; border-collapse:collapse; white-space:nowrap;">
   <tr>
      <th style="text-align:center; padding:8px 12px;">Fullscreen — מסך מלא</th>
      <th style="text-align:center; padding:8px 12px;">Like — לייק</th>
   </tr>
   <tr>
      <td style="text-align:center; padding:8px; vertical-align:top;">
         <img src="setup/fullscreen-command.png" style="max-width:675px; width:100%; height:auto; display:block; margin:0 auto;" alt="מסך מלא"/>
         <div style="font-size:0.95em; color:#333; margin-top:6px;">פרמטר: <code>fullscreen</code></div>
      </td>
      <td style="text-align:center; padding:8px; vertical-align:top;">
         <img src="setup/like-command.png" style="max-width:675px; width:100%; height:auto; display:block; margin:0 auto;" alt="לייק"/>
         <div style="font-size:0.95em; color:#333; margin-top:6px;">פרמטר: <code>like</code></div>
      </td>
   </tr>
</table>

<table dir="rtl" style="margin:12px auto; border-collapse:collapse; white-space:nowrap;">
   <tr>
      <th style="text-align:center; padding:8px 12px;">Exit — יציאה וסגירה</th>
      <th style="text-align:center; padding:8px 12px;"></th>
   </tr>
   <tr>
      <td style="text-align:center; padding:8px; vertical-align:top;">
         <img src="setup/exit-command.png" style="max-width:675px; width:100%; height:auto; display:block; margin:0 auto;" alt="יציאה"/>
         <div style="font-size:0.95em; color:#333; margin-top:6px;">פרמטר: <code>exit</code></div>
      </td>
      <td style="text-align:center; padding:8px; vertical-align:top;">&nbsp;</td>
   </tr>
</table>

---
## הערת מעבר מגרסה קודמת

שימו לב: לוחות תקשורת (Grid sets) שעבדו בגרסאות קודמות **לא יעבדו כלל** בגרסה הנוכחית, ולכן נדרש להשתמש בפורמט של הלוח החדש בלבד.



</div>

